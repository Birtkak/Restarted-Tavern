using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RestartedTavern.Rules
{
    /// <summary>A public property that isn't card data (a shorthand for another property). Card files skip it.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotCardDataAttribute : Attribute
    {
    }
}

namespace RestartedTavern.Rules.Data
{
    /// <summary>
    /// Card definitions to and from JSON (DEVELOPMENT §3). The mapping is plain reflection over the rules classes:
    /// every public read-write property is a JSON property (camelCase), values equal to the class's defaults are left
    /// out, enums are written by name ("Haste, Trample" for flags), and an object whose class isn't the declared one
    /// (the effect and static ability building blocks) gets a "$type" with its class name. Reading fails loudly on an
    /// unknown class, property or enum name, so a typo in a card file never goes unnoticed.
    /// </summary>
    public static class CardJson
    {
        private const string TypeKey = "$type";

        private static readonly Dictionary<string, Type> BuildingBlocks = FindBuildingBlocks();
        private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new Dictionary<Type, PropertyInfo[]>();
        private static readonly Dictionary<Type, object> DefaultCache = new Dictionary<Type, object>();
        private static readonly object Lock = new object();

        public static string WriteCards(IEnumerable<CardDefinition> cards) =>
            Json.Write(cards.Select(c => ToJson(c, typeof(CardDefinition))).ToList());

        public static List<CardDefinition> ReadCards(string json, string fileName = "cards")
        {
            if (!(Json.Parse(json) is List<object> list)) throw new FormatException(fileName + ": expected a JSON array of cards.");
            var cards = new List<CardDefinition>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    cards.Add((CardDefinition)FromJson(list[i], typeof(CardDefinition)));
                }
                catch (Exception e) when (!(e is FormatException && e.Message.StartsWith(fileName, StringComparison.Ordinal)))
                {
                    string id = list[i] is Json.Obj o && o.TryGet("id", out var v) ? v as string : "#" + i;
                    throw new FormatException(fileName + ", card " + id + ": " + e.Message, e);
                }
            }
            return cards;
        }

        // ---------------------------------------------------------------- writing

        public static object ToJson(object value, Type declared)
        {
            if (value == null) return null;
            var type = value.GetType();
            if (value is string || value is bool) return value;
            if (value is int i) return (long)i;
            if (value is long) return value;
            if (type.IsEnum) return value.ToString();
            if (value is IList list)
            {
                var elementType = ElementType(declared) ?? ElementType(type) ?? typeof(object);
                var result = new List<object>(list.Count);
                foreach (var item in list) result.Add(ToJson(item, elementType));
                return result;
            }
            if (!type.IsClass) throw new NotSupportedException("Card data can't hold a " + type.Name + ".");

            var obj = new Json.Obj();
            var underlying = Nullable.GetUnderlyingType(declared) ?? declared;
            if (underlying.IsAbstract || underlying != type) obj.Add(TypeKey, type.Name);
            var defaults = DefaultOf(type);
            foreach (var p in DataProperties(type))
            {
                var v = p.GetValue(value);
                var json = ToJson(v, p.PropertyType);
                if (!AlwaysWritten(value, p) && Same(json, ToJson(p.GetValue(defaults), p.PropertyType))) continue;
                obj.Add(Camel(p.Name), json);
            }
            return obj;
        }

        /// <summary>The fields a card file always shows, even at their default, so each card reads on its own.</summary>
        private static bool AlwaysWritten(object owner, PropertyInfo p)
        {
            if (!(owner is CardDefinition card)) return false;
            switch (p.Name)
            {
                case nameof(CardDefinition.Type):
                case nameof(CardDefinition.Faction):
                    return true;
                case nameof(CardDefinition.Cost):
                case nameof(CardDefinition.Rarity):
                case nameof(CardDefinition.Text):
                    return !card.IsToken;
                case nameof(CardDefinition.Power):
                case nameof(CardDefinition.Health):
                    return card.IsCreature;
                default:
                    return false;
            }
        }

        private static bool IsEmpty(object json) => json == null || json is List<object> l && l.Count == 0;

        private static bool Same(object a, object b)
        {
            if (IsEmpty(a) && IsEmpty(b)) return true;
            if (a == null || b == null) return false;
            return Json.Write(a) == Json.Write(b);
        }

        // ---------------------------------------------------------------- reading

        public static object FromJson(object json, Type declared)
        {
            var nullable = Nullable.GetUnderlyingType(declared);
            var type = nullable ?? declared;
            if (json == null)
            {
                if (type.IsValueType && nullable == null) throw new FormatException("null for a " + type.Name);
                return null;
            }
            if (type == typeof(string)) return json as string ?? throw new FormatException("expected text, got " + json);
            if (type == typeof(bool)) return json is bool b ? b : throw new FormatException("expected true/false, got " + json);
            if (type == typeof(int)) return json is long l ? checked((int)l) : throw new FormatException("expected a whole number, got " + json);
            if (type == typeof(long)) return json is long l2 ? l2 : throw new FormatException("expected a whole number, got " + json);
            if (type.IsEnum) return ParseEnum(type, json as string ?? throw new FormatException("expected a " + type.Name + " name"));

            var elementType = ElementType(type);
            if (elementType != null)
            {
                if (!(json is List<object> items)) throw new FormatException("expected a list for " + type.Name);
                if (type.IsArray)
                {
                    var array = Array.CreateInstance(elementType, items.Count);
                    for (int i = 0; i < items.Count; i++) array.SetValue(FromJson(items[i], elementType), i);
                    return array;
                }
                var list = (IList)Activator.CreateInstance(type);
                foreach (var item in items) list.Add(FromJson(item, elementType));
                return list;
            }

            if (!(json is Json.Obj obj)) throw new FormatException("expected an object for " + type.Name);
            var actual = type;
            if (obj.TryGet(TypeKey, out var typeName))
            {
                if (!BuildingBlocks.TryGetValue(typeName as string ?? "", out actual))
                    throw new FormatException("unknown building block \"" + typeName + "\"");
                if (!type.IsAssignableFrom(actual)) throw new FormatException(typeName + " is not a " + type.Name);
            }
            else if (type.IsAbstract)
            {
                throw new FormatException("a " + type.Name + " needs \"" + TypeKey + "\"");
            }

            var instance = Activator.CreateInstance(actual);
            var properties = DataProperties(actual);
            foreach (var kv in obj)
            {
                if (kv.Key == TypeKey) continue;
                var p = properties.FirstOrDefault(x => string.Equals(Camel(x.Name), kv.Key, StringComparison.Ordinal));
                if (p == null) throw new FormatException(actual.Name + " has no property \"" + kv.Key + "\"");
                try
                {
                    p.SetValue(instance, FromJson(kv.Value, p.PropertyType));
                }
                catch (FormatException e)
                {
                    throw new FormatException(actual.Name + "." + kv.Key + ": " + e.Message, e);
                }
            }
            return instance;
        }

        private static object ParseEnum(Type type, string text)
        {
            foreach (var part in text.Split(','))
            {
                string name = part.Trim();
                if (!Enum.IsDefined(type, name)) throw new FormatException("\"" + name + "\" is not a " + type.Name);
            }
            return Enum.Parse(type, text);
        }

        // ---------------------------------------------------------------- reflection helpers

        private static Type ElementType(Type type)
        {
            if (type == null || type == typeof(string)) return null;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return type.GetGenericArguments()[0];
            return null;
        }

        /// <summary>Public read-write properties, base class first, in declaration order.</summary>
        private static PropertyInfo[] DataProperties(Type type)
        {
            lock (Lock)
            {
                if (PropertyCache.TryGetValue(type, out var cached)) return cached;
                var chain = new List<Type>();
                for (var t = type; t != null && t != typeof(object); t = t.BaseType) chain.Insert(0, t);
                var result = new List<PropertyInfo>();
                foreach (var t in chain)
                    result.AddRange(t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(p => p.CanRead && p.CanWrite && p.GetSetMethod() != null && p.GetIndexParameters().Length == 0
                                    && p.GetCustomAttribute<NotCardDataAttribute>() == null)
                        .OrderBy(p => p.MetadataToken));
                var array = result.ToArray();
                PropertyCache[type] = array;
                return array;
            }
        }

        private static object DefaultOf(Type type)
        {
            lock (Lock)
            {
                if (!DefaultCache.TryGetValue(type, out var d)) DefaultCache[type] = d = Activator.CreateInstance(type);
                return d;
            }
        }

        private static string Camel(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);

        /// <summary>Every concrete effect and static ability class, by its class name.</summary>
        private static Dictionary<string, Type> FindBuildingBlocks()
        {
            var result = new Dictionary<string, Type>();
            foreach (var t in typeof(Effect).Assembly.GetTypes())
            {
                if (t.IsAbstract || !t.IsClass || !t.IsPublic) continue;
                if (!typeof(Effect).IsAssignableFrom(t) && !typeof(StaticAbility).IsAssignableFrom(t)) continue;
                if (result.ContainsKey(t.Name)) throw new InvalidOperationException("Two building blocks are called " + t.Name);
                result.Add(t.Name, t);
            }
            return result;
        }
    }
}
