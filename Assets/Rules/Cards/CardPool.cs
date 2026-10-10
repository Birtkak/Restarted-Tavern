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
        }

        public static IReadOnlyList<DeckList> PrototypeDecks()
        {
            lock (Lock)
            {
                if (_decks != null) return _decks;
                string file = Path.Combine(DataRoot, "Decks", "prototype_decks.json");
                if (!(Json.Parse(File.ReadAllText(file)) is List<object> list)) throw new FormatException(file + ": expected a list of decks.");
                var decks = new List<DeckList>();
                foreach (var item in list)
                {
                    var o = (Json.Obj)item;
                    var deck = new DeckList
                    {
                        Id = Text(o, "id"), Name = Text(o, "name"), TavernDweller = Text(o, "tavernDweller"), Description = Text(o, "description"),
                    };
                    o.TryGet("cards", out var cards);
                    foreach (var kv in (Json.Obj)cards)
                        for (long n = 0; n < (long)kv.Value; n++) deck.Cards.Add(kv.Key);
                    decks.Add(deck);
                }
                return _decks = decks;
            }

            string Text(Json.Obj o, string key) => o.TryGet(key, out var v) ? v as string : null;
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
