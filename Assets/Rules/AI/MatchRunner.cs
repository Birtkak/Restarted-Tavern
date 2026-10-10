using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestartedTavern.Rules.AI
{
    /// <summary>One simulated matchup: two decks (each with a bot style), a format, a number of games.</summary>
    public sealed class MatchConfig
    {
        public string Name { get; set; }
        public FormatConfig Format { get; set; } = FormatConfig.Standard();
        public string DeckAName { get; set; }
        public List<string> DeckA { get; set; }
        public string TavernDwellerA { get; set; }
        public BotStyle StyleA { get; set; } = BotStyle.Greedy();
        public string DeckBName { get; set; }
        public List<string> DeckB { get; set; }
        public string TavernDwellerB { get; set; }
        public BotStyle StyleB { get; set; } = BotStyle.Greedy();
        public int Games { get; set; } = 1000;
        public ulong FirstSeed { get; set; } = 1;
        /// <summary>Games running longer than this many rounds count as draws.</summary>
        public int MaxRounds { get; set; } = 120;
    }

    /// <summary>Aggregated statistics over all games of a <see cref="MatchConfig"/>.</summary>
    public sealed class MatchResult
    {
        public const int LongGameRounds = 25;
        public const int ShortGameRounds = 10;

        public MatchConfig Config;
        public int Games, WinsA, WinsB, Draws;
        public int FirstPlayerWins;
        public List<int> GameLengths = new List<int>();
        /// <summary>Unspent mana at the end of the round, how much of it became Gold, and Gold spent.</summary>
        public long UnspentMana, GoldBanked, GoldSpent;
        public long InstantsOnOpponentsTurn, AbilitiesOnOpponentsTurn;
        /// <summary>Cards that went from hand to the graveyard (mostly discards to hand size).</summary>
        public long Discards;
        /// <summary>Tavern Dweller Powers used, how many of them while an opponent had the action, other activated abilities, and Gold paid for both.</summary>
        public long PowersUsed, PowersOnOpponentsTurn, AbilitiesActivated, GoldOnAbilities;
        public long CreatureDeaths, HealingDone, DamageToCreatures, DamageToPlayers;
        /// <summary>Damage that was still on a creature when it died (the rest was healed or never mattered).</summary>
        public long DamageOnDeath;
        /// <summary>
        /// Chip damage: damage a creature carried into a later round (its peak carried amount).
        /// ChipThatKilled = the part of it still on the creature when it died.
        /// </summary>
        public long ChipCarried, ChipThatKilled;
        /// <summary>At each round start: creatures on the battlefield, and how many of them carried damage.</summary>
        public long CreatureRoundSamples, WoundedRoundSamples;
        /// <summary>
        /// Per card (balance): in how many decided games each side cast it at least once, and how many of those that side
        /// won. Keys are "A:card_id" / "B:card_id"; Tavern Dweller Powers are "A:power".
        /// </summary>
        public Dictionary<string, int> CardGames = new Dictionary<string, int>(), CardWins = new Dictionary<string, int>();

        public int Decided => WinsA + WinsB;
        public double WinRateA => Games == 0 ? 0 : (double)WinsA / Games;
        public double FirstPlayerWinRate => Decided == 0 ? 0 : (double)FirstPlayerWins / Decided;
        public double AvgRounds => GameLengths.Count == 0 ? 0 : GameLengths.Average();
        public double RoundsStdDev
        {
            get
            {
                if (GameLengths.Count < 2) return 0;
                double avg = AvgRounds;
                return Math.Sqrt(GameLengths.Sum(t => (t - avg) * (t - avg)) / (GameLengths.Count - 1));
            }
        }
        public double LongGameShare => GameLengths.Count == 0 ? 0 : (double)GameLengths.Count(t => t > LongGameRounds) / GameLengths.Count;
        public double ShortGameShare => GameLengths.Count == 0 ? 0 : (double)GameLengths.Count(t => t < ShortGameRounds) / GameLengths.Count;
        public double GoldWastedShare => UnspentMana == 0 ? 0 : 1.0 - (double)GoldBanked / UnspentMana;
        public double WoundedShare => CreatureRoundSamples == 0 ? 0 : (double)WoundedRoundSamples / CreatureRoundSamples;
        /// <summary>Share of damage dealt to creatures that was on them when they died.</summary>
        public double DamageThatKilledShare => DamageToCreatures == 0 ? 0 : (double)DamageOnDeath / DamageToCreatures;
        /// <summary>Share of carried-over chip damage that was still on a creature when it died. The rest never decided anything.</summary>
        public double ChipThatKilledShare => ChipCarried == 0 ? 0 : (double)ChipThatKilled / ChipCarried;
        public double PerGame(long total) => Games == 0 ? 0 : (double)total / Games;

        /// <summary>Adds another (partial) result. Merging in game order keeps results deterministic.</summary>
        public void Add(MatchResult o)
        {
            Games += o.Games; WinsA += o.WinsA; WinsB += o.WinsB; Draws += o.Draws;
            FirstPlayerWins += o.FirstPlayerWins;
            GameLengths.AddRange(o.GameLengths);
            UnspentMana += o.UnspentMana; GoldBanked += o.GoldBanked; GoldSpent += o.GoldSpent;
            InstantsOnOpponentsTurn += o.InstantsOnOpponentsTurn;
            AbilitiesOnOpponentsTurn += o.AbilitiesOnOpponentsTurn;
            Discards += o.Discards;
            PowersUsed += o.PowersUsed; PowersOnOpponentsTurn += o.PowersOnOpponentsTurn;
            AbilitiesActivated += o.AbilitiesActivated; GoldOnAbilities += o.GoldOnAbilities;
            CreatureDeaths += o.CreatureDeaths; HealingDone += o.HealingDone;
            DamageToCreatures += o.DamageToCreatures; DamageToPlayers += o.DamageToPlayers;
            DamageOnDeath += o.DamageOnDeath; ChipCarried += o.ChipCarried; ChipThatKilled += o.ChipThatKilled;
            CreatureRoundSamples += o.CreatureRoundSamples; WoundedRoundSamples += o.WoundedRoundSamples;
            foreach (var kv in o.CardGames) CardGames[kv.Key] = CardGames.TryGetValue(kv.Key, out int n) ? n + kv.Value : kv.Value;
            foreach (var kv in o.CardWins) CardWins[kv.Key] = CardWins.TryGetValue(kv.Key, out int n) ? n + kv.Value : kv.Value;
        }
    }

    /// <summary>
    /// Plays bot-vs-bot games for balance measurements. Decks swap
    /// seats every game; who goes first is still random (GAME_DESIGN §3).
    /// Games run in parallel on all CPU cores. Each game has its own seed and results are merged
    /// in game order, so the outcome is the same as running them one by one.
    /// </summary>
    public static class MatchRunner
    {
        /// <summary>Set to false to play games one at a time (debugging).</summary>
        public static bool Parallel { get; set; } = true;

        public static MatchResult Run(MatchConfig cfg, CardDatabase db)
        {
            var perGame = new MatchResult[cfg.Games];
            if (Parallel)
            {
                System.Threading.Tasks.Parallel.For(0, cfg.Games,
                    () => new GameEngine(db) { CacheLegalActions = true },
                    (g, _, engine) => { perGame[g] = PlayGame(cfg, db, engine, g); return engine; },
                    _ => { });
            }
            else
            {
                var engine = new GameEngine(db) { CacheLegalActions = true };
                for (int g = 0; g < cfg.Games; g++) perGame[g] = PlayGame(cfg, db, engine, g);
            }

            var r = new MatchResult { Config = cfg };
            foreach (var game in perGame) r.Add(game);
            return r;
        }

        private static MatchResult PlayGame(MatchConfig cfg, CardDatabase db, GameEngine engine, int g)
        {
            var botA = new GreedyBot(engine, cfg.StyleA);
            var botB = new GreedyBot(engine, cfg.StyleB);
            var r = new MatchResult { Config = cfg };
            {
                bool aFirstSeat = g % 2 == 0;
                var setups = aFirstSeat
                    ? new[] { new PlayerSetup { Deck = cfg.DeckA, TavernDwellerId = cfg.TavernDwellerA }, new PlayerSetup { Deck = cfg.DeckB, TavernDwellerId = cfg.TavernDwellerB } }
                    : new[] { new PlayerSetup { Deck = cfg.DeckB, TavernDwellerId = cfg.TavernDwellerB }, new PlayerSetup { Deck = cfg.DeckA, TavernDwellerId = cfg.TavernDwellerA } };
                var deckAPlayer = new PlayerId(aFirstSeat ? 1 : 2);

                var events = new List<GameEvent>();
                var state = engine.CreateGame(cfg.Format, setups, cfg.FirstSeed + (ulong)g, events);
                var firstPlayer = state.ActivePlayer;
                var t = new Tracker();

                Tally(r, state, events, db, t);
                var cast = new HashSet<string>();
                while (!state.IsGameOver && state.RoundNumber <= cfg.MaxRounds)
                {
                    var who = engine.WaitingOn(state).Value;
                    var bot = who == deckAPlayer ? botA : botB;
                    var applied = engine.Apply(state, bot.Choose(state, who));
                    foreach (var e in applied)
                    {
                        if (e is SpellCastEvent c) cast.Add((c.Player == deckAPlayer ? "A:" : "B:") + c.DefinitionId);
                        else if (e is AbilityActivatedEvent a && a.IsTavernDwellerPower) cast.Add((a.Player == deckAPlayer ? "A:" : "B:") + "power");
                    }
                    Tally(r, state, applied, db, t);
                }
                foreach (var carried in t.Carried.Values) r.ChipCarried += carried; // survivors: their chip never killed

                r.Games++;
                r.GameLengths.Add(state.RoundNumber);
                if (!state.IsGameOver || state.Winners.Count != 1)
                {
                    r.Draws++;
                    return r;
                }
                var winner = state.Winners[0];
                foreach (var key in cast)
                {
                    r.CardGames[key] = r.CardGames.TryGetValue(key, out int n) ? n + 1 : 1;
                    bool sideWon = (key[0] == 'A') == (winner == deckAPlayer);
                    if (sideWon) r.CardWins[key] = r.CardWins.TryGetValue(key, out int w) ? w + 1 : 1;
                }
                if (winner == firstPlayer) r.FirstPlayerWins++;
                if (winner == deckAPlayer) r.WinsA++;
                else r.WinsB++;
            }
            return r;
        }

        /// <summary>Per-game bookkeeping for the damage metrics.</summary>
        private sealed class Tracker
        {
            /// <summary>Damage currently on each creature.</summary>
            public readonly Dictionary<ObjectId, int> DamageOn = new Dictionary<ObjectId, int>();
            /// <summary>Most damage each creature has carried into a turn start.</summary>
            public readonly Dictionary<ObjectId, int> Carried = new Dictionary<ObjectId, int>();
        }

        private static void Tally(MatchResult r, GameState s, List<GameEvent> events, CardDatabase db, Tracker t)
        {
            var damageOn = t.DamageOn;
            foreach (var e in events)
            {
                switch (e)
                {
                    case GoldBankedEvent b:
                        r.UnspentMana += b.UnspentMana;
                        r.GoldBanked += b.Banked;
                        break;
                    case GoldChangedEvent g when g.NewGold < g.OldGold:
                        r.GoldSpent += g.OldGold - g.NewGold;
                        break;
                    case ZoneChangedEvent z when z.From == Zone.Hand && z.To == Zone.Graveyard:
                        r.Discards++;
                        break;
                    case SpellCastEvent c when c.Player != s.ActivePlayer:
                        r.InstantsOnOpponentsTurn++;
                        break;
                    case AbilityActivatedEvent a:
                        if (a.Player != s.ActivePlayer) r.AbilitiesOnOpponentsTurn++;
                        if (a.IsTavernDwellerPower)
                        {
                            r.PowersUsed++;
                            if (a.Player != s.ActivePlayer) r.PowersOnOpponentsTurn++;
                        }
                        else
                        {
                            r.AbilitiesActivated++;
                        }
                        r.GoldOnAbilities += a.GoldPaid;
                        break;
                    case CreatureDiedEvent d:
                        r.CreatureDeaths++;
                        damageOn.TryGetValue(d.Card, out int onIt);
                        r.DamageOnDeath += onIt;
                        damageOn.Remove(d.Card);
                        if (t.Carried.TryGetValue(d.Card, out int carried))
                        {
                            r.ChipCarried += carried;
                            r.ChipThatKilled += Math.Min(carried, onIt);
                            t.Carried.Remove(d.Card);
                        }
                        break;
                    case HealedEvent h:
                        r.HealingDone += h.Amount;
                        if (!h.Target.IsPlayer && damageOn.TryGetValue(h.Target.Object, out int had))
                            damageOn[h.Target.Object] = Math.Max(0, had - h.Amount);
                        break;
                    case DamageDealtEvent d:
                        if (d.Target.IsPlayer)
                        {
                            r.DamageToPlayers += d.Amount;
                        }
                        else
                        {
                            r.DamageToCreatures += d.Amount;
                            damageOn.TryGetValue(d.Target.Object, out int before);
                            damageOn[d.Target.Object] = before + d.Amount;
                        }
                        break;
                    case RoundStartedEvent _:
                        foreach (var c in s.AllPermanents())
                        {
                            if (!db.Get(c.DefinitionId).IsCreature) continue;
                            r.CreatureRoundSamples++;
                            if (c.Damage <= 0) continue;
                            r.WoundedRoundSamples++;
                            t.Carried.TryGetValue(c.Id, out int peak);
                            t.Carried[c.Id] = Math.Max(peak, c.Damage);
                        }
                        break;
                }
            }
        }

        /// <summary>Runs several matchups in order, reporting each as it finishes.</summary>
        /// <summary>
        /// Every game of every matchup goes into one parallel pool, so cores don't sit idle while a matchup's
        /// last long games finish. Results are still merged per matchup in game order (deterministic).
        /// <paramref name="progress"/> is called as each matchup finishes, possibly from a worker thread.
        /// </summary>
        public static List<MatchResult> RunAll(IEnumerable<MatchConfig> configs, CardDatabase db, Action<MatchResult> progress = null)
        {
            var cfgs = configs.ToList();
            var perGame = cfgs.Select(c => new MatchResult[c.Games]).ToArray();
            var left = cfgs.Select(c => c.Games).ToArray();
            var jobs = new List<(int cfg, int game)>();
            for (int c = 0; c < cfgs.Count; c++)
                for (int g = 0; g < cfgs[c].Games; g++) jobs.Add((c, g));

            MatchResult Merge(int c)
            {
                var r = new MatchResult { Config = cfgs[c] };
                foreach (var game in perGame[c]) r.Add(game);
                return r;
            }

            void Play(GameEngine engine, int j)
            {
                var (c, g) = jobs[j];
                perGame[c][g] = PlayGame(cfgs[c], db, engine, g);
                if (System.Threading.Interlocked.Decrement(ref left[c]) == 0) progress?.Invoke(Merge(c));
            }

            if (Parallel)
            {
                System.Threading.Tasks.Parallel.For(0, jobs.Count,
                    new System.Threading.Tasks.ParallelOptions(),
                    () => new GameEngine(db) { CacheLegalActions = true },
                    (j, _, engine) => { Play(engine, j); return engine; },
                    _ => { });
            }
            else
            {
                var engine = new GameEngine(db) { CacheLegalActions = true };
                for (int j = 0; j < jobs.Count; j++) Play(engine, j);
            }
            return Enumerable.Range(0, cfgs.Count).Select(Merge).ToList();
        }
    }
}
