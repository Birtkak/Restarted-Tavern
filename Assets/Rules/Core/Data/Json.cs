using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RestartedTavern.Rules.Data
{
    /// <summary>
    /// A small JSON reader and writer with no dependencies (the rules assembly can't use UnityEngine, and the SimRunner
    /// runs on plain .NET). Objects are read as ordered lists of (key, value) pairs, arrays as List&lt;object&gt;, numbers as
    /// long or double, plus string, bool and null.
    /// </summary>
    public static class Json
    {
        public sealed class Obj : List<KeyValuePair<string, object>>
        {
            public bool TryGet(string key, out object value)
            {
                foreach (var kv in this)
                    if (kv.Key == key)
                    {
                        value = kv.Value;
                        return true;
                    }
                value = null;
                return false;
            }

            public void Add(string key, object value) => Add(new KeyValuePair<string, object>(key, value));
        }

        // ---------------------------------------------------------------- reading

        public static object Parse(string text)
        {
            int i = 0;
            var value = ReadValue(text, ref i);
            SkipSpace(text, ref i);
            if (i != text.Length) throw Error(text, i, "unexpected text after the value");
            return value;
        }

        private static object ReadValue(string s, ref int i)
        {
            SkipSpace(s, ref i);
            if (i >= s.Length) throw Error(s, i, "unexpected end");
            char c = s[i];
            switch (c)
            {
                case '{': return ReadObject(s, ref i);
                case '[': return ReadArray(s, ref i);
                case '"': return ReadString(s, ref i);
                case 't': Expect(s, ref i, "true"); return true;
                case 'f': Expect(s, ref i, "false"); return false;
                case 'n': Expect(s, ref i, "null"); return null;
                default:
                    if (c == '-' || char.IsDigit(c)) return ReadNumber(s, ref i);
                    throw Error(s, i, "unexpected '" + c + "'");
            }
        }

        private static Obj ReadObject(string s, ref int i)
        {
            var obj = new Obj();
            i++; // {
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Error(s, i, "expected a property name");
                string key = ReadString(s, ref i);
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Error(s, i, "expected ':'");
                i++;
                obj.Add(key, ReadValue(s, ref i));
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return obj; }
                throw Error(s, i, "expected ',' or '}'");
            }
        }

        private static List<object> ReadArray(string s, ref int i)
        {
            var list = new List<object>();
            i++; // [
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (true)
            {
                list.Add(ReadValue(s, ref i));
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return list; }
                throw Error(s, i, "expected ',' or ']'");
            }
        }

        private static string ReadString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // opening quote
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Error(s, i, "bad \\u escape");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw Error(s, i, "bad escape \\" + e);
                }
            }
            throw Error(s, i, "unterminated string");
        }

        private static object ReadNumber(string s, ref int i)
        {
            int start = i;
            if (s[i] == '-') i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+' || s[i] == '-')) i++;
            string n = s.Substring(start, i - start);
            if (long.TryParse(n, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
            return double.Parse(n, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static void Expect(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw Error(s, i, "expected " + word);
            i += word.Length;
        }

        private static void SkipSpace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static FormatException Error(string s, int i, string message)
        {
            int line = 1, col = 1;
            for (int k = 0; k < i && k < s.Length; k++)
            {
                if (s[k] == '\n') { line++; col = 1; }
                else col++;
            }
            return new FormatException("JSON line " + line + ", column " + col + ": " + message);
        }

        // ---------------------------------------------------------------- writing

        /// <summary>
        /// Writes with two-space indentation. Arrays of plain values (strings, numbers) and small objects stay on one
        /// line when they fit in <paramref name="width"/> characters, so card files stay short and readable.
        /// </summary>
        public static string Write(object value, int width = 120)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value, 0, width);
            sb.Append('\n');
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object value, int indent, int width)
        {
            string inline = Inline(value);
            if (inline != null && indent * 2 + inline.Length <= width)
            {
                sb.Append(inline);
                return;
            }
            switch (value)
            {
                case Obj obj:
                    sb.Append("{\n");
                    for (int k = 0; k < obj.Count; k++)
                    {
                        Indent(sb, indent + 1);
                        WriteString(sb, obj[k].Key);
                        sb.Append(": ");
                        WriteValue(sb, obj[k].Value, indent + 1, width);
                        sb.Append(k + 1 < obj.Count ? ",\n" : "\n");
                    }
                    Indent(sb, indent);
                    sb.Append('}');
                    break;
                case List<object> list:
                    sb.Append("[\n");
                    for (int k = 0; k < list.Count; k++)
                    {
                        Indent(sb, indent + 1);
                        WriteValue(sb, list[k], indent + 1, width);
                        sb.Append(k + 1 < list.Count ? ",\n" : "\n");
                    }
                    Indent(sb, indent);
                    sb.Append(']');
                    break;
                default:
                    sb.Append(inline);
                    break;
            }
        }

        /// <summary>The value on one line, or null if it has nested objects/arrays with more than one entry.</summary>
        private static string Inline(object value)
        {
            var sb = new StringBuilder();
            switch (value)
            {
                case Obj obj:
                    sb.Append('{');
                    for (int k = 0; k < obj.Count; k++)
                    {
                        if (k > 0) sb.Append(", ");
                        WriteString(sb, obj[k].Key);
                        sb.Append(": ");
                        string inner = Inline(obj[k].Value);
                        if (inner == null || (obj[k].Value is Obj o && o.Count > 1) || (obj[k].Value is List<object> l && HasContainers(l)))
                            return null;
                        sb.Append(inner);
                    }
                    sb.Append('}');
                    return sb.ToString();
                case List<object> list:
                    if (HasContainers(list)) return null;
                    sb.Append('[');
                    for (int k = 0; k < list.Count; k++)
                    {
                        if (k > 0) sb.Append(", ");
                        sb.Append(Inline(list[k]));
                    }
                    sb.Append(']');
                    return sb.ToString();
                case null: return "null";
                case bool b: return b ? "true" : "false";
                case string str:
                    WriteString(sb, str);
                    return sb.ToString();
                case long l: return l.ToString(CultureInfo.InvariantCulture);
                case int n: return n.ToString(CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                default: throw new ArgumentException("Can't write " + value.GetType().Name + " as JSON.");
            }
        }

        private static bool HasContainers(List<object> list)
        {
            foreach (var v in list)
                if (v is Obj || v is List<object>) return true;
            return false;
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private static void Indent(StringBuilder sb, int indent) => sb.Append(' ', indent * 2);
    }
}
