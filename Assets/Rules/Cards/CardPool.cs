using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RestartedTavern.Rules.Data;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// Every card and the prototype decks, loaded from the data files (DEVELOPMENT §3):
    /// <c>Assets/StreamingAssets/Cards/*.json</c> (one file per faction plus the Tavern Dwellers) and
    /// <c>Assets/StreamingAssets/Decks/prototype_decks.json</c>. The C# side only has the building blocks
    /// (effects, statics, triggers) the files are made of. Loaded once and shared: definitions never change.
    /// </summary>
    public static class CardPool
    {
        // Ids the code refers to (tests, bots, effects that make tokens).
        public const string GooberToken = "goober_token";
        public const string SpiritToken = "spirit_token";
        public const string MercenaryToken = "mercenary_token";
        public const string DroneToken = "drone_token";
        public const string SpawnToken = "spawn_token";
        public const string ScrapPlatingToken = "scrap_plating_token";
        public const string CritterToken = "critter_token";

        private static readonly object Lock = new object();
        private static string _dataRoot;
        private static List<CardDefinition> _cards;
        private static List<DeckList> _decks;
        private static string _customDecksFile;

        /// <summary>
        /// The folder with <c>Cards/</c> and <c>Decks/</c> (Unity's StreamingAssets). If it isn't set, it's found by
        /// looking for <c>Assets/StreamingAssets</c> from the working directory and the program's folder upward, which
        /// works in the Unity editor, in tests and in Tools/SimRunner. Built players set it to Application.streamingAssetsPath.
        /// </summary>
        public static string DataRoot
        {
            get
            {
                lock (Lock) return _dataRoot ?? (_dataRoot = FindDataRoot());
            }
            set
            {
                lock (Lock)
                {
                    _dataRoot = value;
                    _cards = null;
                    _decks = null;
                }
            }
        }

        /// <summary>
        /// The player's own decks (the deck editor's file, a list in the prototype deck format). When set, they come
        /// after the prototype decks in <see cref="PrototypeDecks"/>, so the prototype indexes stay the same. Only the
        /// client sets it; sims and tests see the prototype decks alone.
        /// </summary>
        public static string CustomDecksFile
        {
            get { lock (Lock) return _customDecksFile; }
            set
            {
                lock (Lock)
                {
                    _customDecksFile = value;
                    _decks = null;
                }
            }
        }

        public static IEnumerable<CardDefinition> All() => Cards();

        public static CardDatabase CreateDatabase() => new CardDatabase(All());

        private static List<CardDefinition> Cards()
        {
            lock (Lock)
            {
                if (_cards != null) return _cards;
                var cards = new List<CardDefinition>();
                foreach (var file in Directory.GetFiles(Path.Combine(DataRoot, "Cards"), "*.json").OrderBy(f => f, StringComparer.Ordinal))
                    cards.AddRange(CardJson.ReadCards(File.ReadAllText(file), Path.GetFileName(file)));
                return _cards = cards;
            }
        }

        // ---------------------------------------------------------------- decks

        /// <summary>A deck list from a data file.</summary>
        public sealed class DeckList
        {
            public string Id;
            public string Name;
            public string TavernDweller;
            public string Description;
            public List<string> Cards = new List<string>();
            /// <summary>Made in the deck editor (lives in <see cref="CustomDecksFile"/>).</summary>
            public bool Custom;
        }

        public static IReadOnlyList<DeckList> PrototypeDecks()
        {
            lock (Lock)
            {
                if (_decks != null) return _decks;
                var decks = ReadDecks(Path.Combine(DataRoot, "Decks", "prototype_decks.json"), false);
                if (_customDecksFile != null && File.Exists(_customDecksFile))
                {
                    try { decks.AddRange(ReadDecks(_customDecksFile, true)); }
                    catch (Exception) { /* a broken file of the player's own: show the prototype decks only */ }
                }
                return _decks = decks;
            }
        }

        private static List<DeckList> ReadDecks(string file, bool custom)
        {
            if (!(Json.Parse(File.ReadAllText(file)) is List<object> list)) throw new FormatException(file + ": expected a list of decks.");
            var decks = new List<DeckList>();
            foreach (var item in list)
            {
                var o = (Json.Obj)item;
                var deck = new DeckList
                {
                    Id = Text(o, "id"), Name = Text(o, "name"), TavernDweller = Text(o, "tavernDweller"), Description = Text(o, "description"),
                    Custom = custom,
                };
                o.TryGet("cards", out var cards);
                foreach (var kv in (Json.Obj)cards)
                    for (long n = 0; n < (long)kv.Value; n++) deck.Cards.Add(kv.Key);
                decks.Add(deck);
            }
            return decks;

            string Text(Json.Obj o, string key) => o.TryGet(key, out var v) ? v as string : null;
        }

        /// <summary>Writes the player's own decks to <see cref="CustomDecksFile"/> (same format as the prototype decks).</summary>
        public static void SaveCustomDecks(IEnumerable<DeckList> decks)
        {
            string file = CustomDecksFile ?? throw new InvalidOperationException("CustomDecksFile isn't set.");
            var sb = new System.Text.StringBuilder("[\n");
            bool first = true;
            foreach (var d in decks)
            {
                if (!first) sb.Append(",\n");
                first = false;
                sb.Append("  {\n    \"id\": ").Append(Quote(d.Id)).Append(",\n    \"name\": ").Append(Quote(d.Name))
                  .Append(",\n    \"tavernDweller\": ").Append(Quote(d.TavernDweller))
                  .Append(",\n    \"description\": ").Append(Quote(d.Description ?? "")).Append(",\n    \"cards\": {");
                bool firstCard = true;
                foreach (var g in d.Cards.GroupBy(c => c))
                {
                    sb.Append(firstCard ? "\n" : ",\n").Append("      ").Append(Quote(g.Key)).Append(": ").Append(g.Count());
                    firstCard = false;
                }
                sb.Append(firstCard ? "}\n  }" : "\n    }\n  }");
            }
            sb.Append("\n]\n");
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, sb.ToString());
            lock (Lock) _decks = null;

            string Quote(string s)
            {
                var q = new System.Text.StringBuilder("\"");
                foreach (char c in s ?? "")
                {
                    if (c == '"' || c == '\\') q.Append('\\').Append(c);
                    else if (c < ' ') q.Append("\\u").Append(((int)c).ToString("x4"));
                    else q.Append(c);
                }
                return q.Append('"').ToString();
            }
        }

        public static DeckList PrototypeDeck(string id) =>
            PrototypeDecks().FirstOrDefault(d => d.Id == id) ?? throw new KeyNotFoundException("Unknown deck: " + id);

        private static List<string> Copy(string id) => new List<string>(PrototypeDeck(id).Cards);

        public static List<string> GooberMobDeck() => Copy("goober_mob");
        public static List<string> JungleStampedeDeck() => Copy("jungle_stampede");
        public static List<string> ZooPatrolDeck() => Copy("zoo_patrol");
        public static List<string> VespersLedgerDeck() => Copy("vespers_ledger");
        public static List<string> SparkwrenchScrappersDeck() => Copy("sparkwrench_scrappers");
        public static List<string> AuditorsArsenalDeck() => Copy("auditors_arsenal");

        /// <summary>The Tavern Dweller each prototype deck is built around (GAME_DESIGN §9).</summary>
        public static string GooberMobTavernDweller => PrototypeDeck("goober_mob").TavernDweller;
        public static string JungleStampedeTavernDweller => PrototypeDeck("jungle_stampede").TavernDweller;
        public static string ZooPatrolTavernDweller => PrototypeDeck("zoo_patrol").TavernDweller;
        public static string VespersLedgerTavernDweller => PrototypeDeck("vespers_ledger").TavernDweller;
        public static string SparkwrenchScrappersTavernDweller => PrototypeDeck("sparkwrench_scrappers").TavernDweller;
        public static string AuditorsArsenalTavernDweller => PrototypeDeck("auditors_arsenal").TavernDweller;

        // ---------------------------------------------------------------- finding the files

        private static string FindDataRoot()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
            {
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    string candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets");
                    if (Directory.Exists(Path.Combine(candidate, "Cards"))) return candidate;
                }
            }
            throw new DirectoryNotFoundException(
                "Card data not found: no Assets/StreamingAssets/Cards above " + Directory.GetCurrentDirectory() + ". Set CardPool.DataRoot.");
        }
    }
}
