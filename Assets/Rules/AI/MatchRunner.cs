using System;
using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Rules.AI
{
    /// <summary>One simulated matchup: two decks, a format, a number of games.</summary>
    public sealed class MatchConfig
    {
        public string Name { get; set; }
        public FormatConfig Format { get; set; } = FormatConfig.Standard();
        public string DeckAName { get; set; }
        public List<string> DeckA { get; set; }
        public string DeckBName { get; set; }
        public List<string> DeckB { get; set; }
        public int Games { get; set; } = 1000;
        public ulong FirstSeed { get; set; } = 1;
        /// <summary>Games running longer than this many turns count as draws.</summary>
        public int MaxTurns { get; set; } = 120;
    }

    /// <summary>Aggregated statistics over all games of a <see cref="MatchConfig"/>.</summary>
    public sealed class MatchResult
    {
        public MatchConfig Config;
        public int Games, WinsA, WinsB, Draws;
        public int FirstPlayerWins;
        public long TotalTurns;
        /// <summary>Unspent mana at end of turn, how much of it became Gold, and Gold spent.</summary>
        public long UnspentMana, GoldBanked, GoldSpent;
        public long InstantsOnOpponentsTurn;
        public long CreatureDeaths, HealingDone, DamageToCreatures, DamageToPlayers;
        /// <summary>At each turn start: creatures on the battlefield, and how many of them carried damage.</summary>
        public long CreatureTurnSamples, WoundedTurnSamples;
        public long WinnerLifeTotal;
        public int[] WinsBySeatDeckA = new int[2];
        public int[] GamesBySeatDeckA = new int[2];

        public double WinRateA => Games == 0 ? 0 : (double)WinsA / Games;
        public double FirstPlayerWinRate => Decided == 0 ? 0 : (double)FirstPlayerWins / Decided;
        public int Decided => WinsA + WinsB;
        public double AvgTurns => Games == 0 ? 0 : (double)TotalTurns / Games;
        public double GoldWastedShare => UnspentMana == 0 ? 0 : 1.0 - (double)GoldBanked / UnspentMana;
        public double WoundedShare => CreatureTurnSamples == 0 ? 0 : (double)WoundedTurnSamples / CreatureTurnSamples;
        public double PerGame(long total) => Games == 0 ? 0 : (double)total / Games;
    }

    /// <summary>
    /// Plays bot-vs-bot games for balance experiments (DEVELOPMENT roadmap step 3). Decks swap
    /// seats every game; who goes first is still random (GAME_DESIGN §3).
    /// </summary>
    public static class MatchRunner
    {
        public static MatchResult Run(MatchConfig cfg, CardDatabase db)
        {
            var engine = new GameEngine(db);
            var bot = new GreedyBot(engine);
            var r = new MatchResult { Config = cfg };

            for (int g = 0; g < cfg.Games; g++)
            {
                bool aFirstSeat = g % 2 == 0;
                var setups = aFirstSeat
                    ? new[] { new PlayerSetup { Deck = cfg.DeckA }, new PlayerSetup { Deck = cfg.DeckB } }
                    : new[] { new PlayerSetup { Deck = cfg.DeckB }, new PlayerSetup { Deck = cfg.DeckA } };
                var deckAPlayer = new PlayerId(aFirstSeat ? 1 : 2);

                var events = new List<GameEvent>();
                var state = engine.CreateGame(cfg.Format, setups, cfg.FirstSeed + (ulong)g, events);
                var firstPlayer = state.ActivePlayer;
                int seatOfA = state.GetPlayer(deckAPlayer).Seat == state.StartingPlayerIndex ? 0 : 1; // 0 = A went first
                r.GamesBySeatDeckA[seatOfA]++;

                Tally(r, state, events, db);
                while (!state.IsGameOver && state.TurnNumber <= cfg.MaxTurns)
                {
                    var who = engine.WaitingOn(state).Value;
                    Tally(r, state, engine.Apply(state, bot.Choose(state, who)), db);
                }

                r.Games++;
                r.TotalTurns += state.TurnNumber;
                if (!state.IsGameOver || state.Winners.Count != 1)
                {
                    r.Draws++;
                    continue;
                }
                var winner = state.Winners[0];
                r.WinnerLifeTotal += state.GetPlayer(winner).Life;
                if (winner == firstPlayer) r.FirstPlayerWins++;
                if (winner == deckAPlayer)
                {
                    r.WinsA++;
                    r.WinsBySeatDeckA[seatOfA]++;
                }
                else r.WinsB++;
            }
            return r;
        }

        private static void Tally(MatchResult r, GameState s, List<GameEvent> events, CardDatabase db)
        {
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
                    case SpellCastEvent c when c.Player != s.ActivePlayer:
                        r.InstantsOnOpponentsTurn++;
                        break;
                    case CreatureDiedEvent _:
                        r.CreatureDeaths++;
                        break;
                    case HealedEvent h:
                        r.HealingDone += h.Amount;
                        break;
                    case DamageDealtEvent d:
                        if (d.Target.IsPlayer) r.DamageToPlayers += d.Amount;
                        else r.DamageToCreatures += d.Amount;
                        break;
                    case TurnStartedEvent _:
                        foreach (var c in s.AllPermanents())
                        {
                            if (!db.Get(c.DefinitionId).IsCreature) continue;
                            r.CreatureTurnSamples++;
                            if (c.Damage > 0) r.WoundedTurnSamples++;
                        }
                        break;
                }
            }
        }

        /// <summary>Runs several matchups in order, reporting each as it finishes.</summary>
        public static List<MatchResult> RunAll(IEnumerable<MatchConfig> configs, CardDatabase db, Action<MatchResult> progress = null)
        {
            var results = new List<MatchResult>();
            foreach (var cfg in configs)
            {
                var r = Run(cfg, db);
                results.Add(r);
                progress?.Invoke(r);
            }
            return results;
        }
    }
}
