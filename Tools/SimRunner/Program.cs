using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RestartedTavern.Rules;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.SimRunner
{
    /// <summary>
    /// The simulation report outside Unity (same code as SimulationMenu.RunReport, faster runtime), plus bot tools.
    ///   (default)  the report. [-simGames N] [-simSections "round,cap"] [-out path]
    ///   -trace     one bot game, readable, turn by turn. [-decks "0,1"] [-seed N] [-rules runeterra|classic] [-out path]
    ///   -h2h       the current GreedyBot against BotStyle.Baseline() in every mirror (A = current). [-simGames N] [-rules ...]
    /// Decks are indexes into Experiments.PrototypeDecks(): 0 Goober, 1 Jungle, 2 Zoo, 3 Vesper, 4 Sparkwrench, 5 Auditor.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string Arg(string name, string fallback = null)
            {
                int i = Array.IndexOf(args, name);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
            }
            bool runeterra = Arg("-rules", "runeterra") != "classic";
            int games = int.Parse(Arg("-simGames", "500"));

            if (args.Contains("-trace")) return Trace(Arg("-decks", "0,1"), ulong.Parse(Arg("-seed", "1")), runeterra, Arg("-out"), Arg("-pass", "0,0"), Arg("-swap"));
            if (args.Contains("-h2h")) return HeadToHead(games, runeterra, Arg("-off"));
            if (args.Contains("-tweaks")) return TweakTest(games, runeterra);
            if (args.Contains("-balance")) return Balance(games, runeterra, Arg("-out"));
            if (args.Contains("-decktest")) return args.Contains("-singles") ? SwapTest(games, runeterra) : DeckTest(games, runeterra);
            return Report(games, Arg("-simSections")?.ToLowerInvariant().Split(','), Arg("-out"));
        }

        private static FormatConfig Rules(bool runeterra) => runeterra ? FormatConfig.Runeterra(3) : FormatConfig.Standard();

        private static int Report(int games, string[] only, string output)
        {
            output ??= Path.Combine(RepoRoot(), "docs", "playtest", "SIMULATION_REPORT.md");
            var watch = Stopwatch.StartNew();
            var sections = Experiments.Build(games);
            if (only != null)
                sections = sections.FindAll(s => Array.Exists(only, w => s.Title.ToLowerInvariant().Contains(w.Trim())));
            var gate = new object();
            Experiments.Run(sections, CardPool.CreateDatabase(), r =>
            {
                lock (gate)
                    Console.WriteLine($"[sim] {r.Config.Name}: A {r.WinRateA:P1}, first {r.FirstPlayerWinRate:P1}, {r.AvgTurns:0.0} turns ({watch.Elapsed.TotalSeconds:0}s)");
            });
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, Experiments.ToMarkdown(sections, games, watch.Elapsed).Replace("\r\n", "\n"));
            Console.WriteLine("[sim] Report written to " + output + " in " + watch.Elapsed.TotalSeconds.ToString("0") + "s");
            return 0;
        }

        /// <summary>The current bot (A) against the baseline bot (B) in every mirror, seats swapped every game.</summary>
        private static int HeadToHead(int games, bool runeterra, string off)
        {
            BotStyle B()
            {
                if (off == null) return BotStyle.Baseline();
                // The current bot with one switch turned back to its baseline value.
                var style = BotStyle.Greedy();
                var prop = typeof(BotStyle).GetProperty(off) ?? throw new ArgumentException("No BotStyle switch " + off);
                prop.SetValue(style, prop.GetValue(BotStyle.Baseline()));
                style.Name = "without " + off;
                return style;
            }
            var decks = Experiments.PrototypeDecks();
            var configs = decks.Select(d => new MatchConfig
            {
                Name = d.Name, Format = Rules(runeterra), Games = games,
                DeckAName = d.Name, DeckA = d.Cards, TavernDwellerA = d.TavernDweller, StyleA = BotStyle.Greedy(),
                DeckBName = d.Name, DeckB = d.Cards, TavernDwellerB = d.TavernDweller, StyleB = B(),
            }).ToList();
            var watch = Stopwatch.StartNew();
            var results = MatchRunner.RunAll(configs, CardPool.CreateDatabase());
            Console.WriteLine($"Head-to-head, {(runeterra ? "Runeterra" : "classic")} rules, {games} games per mirror ({watch.Elapsed.TotalSeconds:0}s). A = current bot, B = {B().Name}.");
            foreach (var r in results)
                Console.WriteLine($"  {r.Config.Name,-22} current wins {r.WinRateA,6:P1}   1st {r.FirstPlayerWinRate,6:P1}   {r.AvgTurns,5:0.0} turns   off-turn {r.PerGame(r.InstantsOnOpponentsTurn + r.AbilitiesOnOpponentsTurn),4:0.0}   wasted {r.GoldWastedShare,6:P1}   discards {r.PerGame(r.Discards),4:0.0}   draws {r.Draws}");
            Console.WriteLine($"  {"ALL",-22} current wins {results.Sum(r => r.WinsA) / (double)results.Sum(r => r.Games),6:P1}");
            return 0;
        }

        /// <summary>Candidate deck lists: each deck swaps whole cards (all four copies) for others. Used by -decktest.</summary>
        /// <summary>
        /// Candidate deck lists for -decktest / -singles / trace -pass: each deck swaps whole cards (all four copies).
        /// The v0.3 deck pass was approved and applied on 2026-10-09, so this is empty until the next one.
        /// </summary>
        private static readonly Dictionary<string, (string[] Out, string[] In)> DeckPass = new Dictionary<string, (string[], string[])>();

        private static List<string> Swap(List<string> deck, (string[] Out, string[] In) pass)
        {
            var result = deck.Where(id => !pass.Out.Contains(id)).ToList();
            foreach (var id in pass.In) result.AddRange(Enumerable.Repeat(id, 4));
            if (result.Count != 60) throw new InvalidOperationException("Deck pass leaves " + result.Count + " cards");
            return result;
        }

        /// <summary>
        /// The candidate deck pass: each new list's mirror next to the old one (mana left unspent per turn, game length,
        /// plays on the opponent's turn, first-player win%), and the new list against the old one.
        /// </summary>
        private static int DeckTest(int games, bool runeterra)
        {
            var db = CardPool.CreateDatabase();
            var configs = new List<MatchConfig>();
            foreach (var d in Experiments.PrototypeDecks())
            {
                if (!DeckPass.ContainsKey(d.Name)) continue;
                var nu = Swap(d.Cards, DeckPass[d.Name]);
                DeckValidator.Validate(db, FormatConfig.Standard(), nu, d.TavernDweller);
                MatchConfig M(string name, List<string> a, List<string> b) => new MatchConfig
                {
                    Name = name, Format = Rules(runeterra), Games = games,
                    DeckAName = d.Name, DeckA = a, TavernDwellerA = d.TavernDweller, DeckBName = d.Name, DeckB = b, TavernDwellerB = d.TavernDweller,
                };
                configs.Add(M(d.Name + " | old mirror", d.Cards, d.Cards));
                configs.Add(M(d.Name + " | new mirror", nu, nu));
                configs.Add(M(d.Name + " | new vs old", nu, d.Cards));
            }
            var watch = Stopwatch.StartNew();
            var results = MatchRunner.RunAll(configs, db);
            Console.WriteLine($"Deck pass test, {(runeterra ? "Runeterra" : "classic")} rules, {games} games per row ({watch.Elapsed.TotalSeconds:0}s).");
            Console.WriteLine("  unspent = mana left at the end of a turn or round, per turn; wasted = share of it lost to the Gold cap");
            foreach (var r in results)
            {
                double unspent = r.GameLengths.Sum() == 0 ? 0 : (double)r.UnspentMana / r.GameLengths.Sum();
                Console.WriteLine($"  {r.Config.Name,-36} A {r.WinRateA,6:P1}  1st {r.FirstPlayerWinRate,6:P1}  {r.AvgTurns,5:0.0} turns  unspent {unspent,4:0.00}/turn  wasted {r.GoldWastedShare,6:P1}  off-turn {r.PerGame(r.InstantsOnOpponentsTurn + r.AbilitiesOnOpponentsTurn),4:0.0}  Gold spent {r.PerGame(r.GoldSpent),4:0.0}");
            }
            return 0;
        }

        /// <summary>Each swap of the deck pass on its own (one card out, one in) against the old list: which new card pulls its weight?</summary>
        private static int SwapTest(int games, bool runeterra)
        {
            var db = CardPool.CreateDatabase();
            var configs = new List<MatchConfig>();
            foreach (var d in Experiments.PrototypeDecks())
            {
                if (!DeckPass.TryGetValue(d.Name, out var pass)) continue;
                for (int i = 0; i < pass.Out.Length; i++)
                {
                    var nu = Swap(d.Cards, (new[] { pass.Out[i] }, new[] { pass.In[i] }));
                    configs.Add(new MatchConfig
                    {
                        Name = d.Name + ": " + pass.Out[i] + " -> " + pass.In[i], Format = Rules(runeterra), Games = games,
                        DeckAName = d.Name, DeckA = nu, TavernDwellerA = d.TavernDweller, DeckBName = d.Name, DeckB = d.Cards, TavernDwellerB = d.TavernDweller,
                    });
                }
            }
            var results = MatchRunner.RunAll(configs, db);
            Console.WriteLine($"Single swaps against the old list, {games} games each (A = with the swap; 50% = as good as the card it replaces).");
            foreach (var r in results) Console.WriteLine($"  {r.Config.Name,-70} {r.WinRateA,6:P1}");
            return 0;
        }

        /// <summary>
        /// Card tuning: each weak card, swapped into a deck as printed and with a proposed change, against the current list
        /// (50% = as good as the card it replaces).
        /// </summary>
        private static int TweakTest(int games, bool runeterra)
        {
            var tests = new (string Deck, string Out, string In, string Change, Action<CardDefinition> Tweak)[]
            {
                ("Vesper's Ledger", "hired_enforcer", "insider_trading", "X+1, no Gold for opponents", d =>
                {
                    d.Cost = 1;
                    d.SpellEffects.RemoveAll(e => e is GainGoldEffect);
                }),
                ("Sparkwrench Scrappers", "brawling_runt", "turret_rig", "+1/+1, Equip 1, ping (1)", d =>
                {
                    d.Abilities[0].Cost = 1;
                    var bonus = (AttachedCreatureModifier)d.Statics[0];
                    bonus.Power = 1;
                    bonus.Health = 1;
                    bonus.Abilities[0].Cost = 1;
                }),
                ("Auditor's Arsenal", "retainer_mage", "loan_shark", "cost 6 -> 5", d => d.Cost = 5),
                ("Goober Mob", "pickpocket_boss", "big_boom", "Sorcery -> Instant", d => d.Type = CardType.Instant),
                ("Jungle Stampede", "reckless_charge", "call_of_the_deep", "cost X+2 -> X+1", d => d.Cost = 1),
                ("Goober Mob", "brawling_runt", "fireworks_stand", "ability (3) -> (2)", d => d.Abilities[0].Cost = 2),
            };
            var plain = CardPool.CreateDatabase();
            var tweakedCards = CardPool.All().ToList();
            foreach (var t in tests) t.Tweak(tweakedCards.First(c => c.Id == t.In));
            var tweaked = new CardDatabase(tweakedCards);

            var decks = Experiments.PrototypeDecks().ToDictionary(d => d.Name);
            List<MatchConfig> Configs() => tests.Select(t =>
            {
                var d = decks[t.Deck];
                return new MatchConfig
                {
                    Name = t.Deck + ": " + t.Out + " -> " + t.In, Format = Rules(runeterra), Games = games,
                    DeckAName = d.Name, DeckA = Swap(d.Cards, (new[] { t.Out }, new[] { t.In })), TavernDwellerA = d.TavernDweller,
                    DeckBName = d.Name, DeckB = d.Cards, TavernDwellerB = d.TavernDweller,
                };
            }).ToList();
            var before = MatchRunner.RunAll(Configs(), plain);
            var after = MatchRunner.RunAll(Configs(), tweaked);
            Console.WriteLine($"Card tweaks, {games} games each (A = with the swap; 50% = as good as the card it replaces).");
            for (int i = 0; i < tests.Length; i++)
                Console.WriteLine($"  {before[i].Config.Name,-58} as printed {before[i].WinRateA,6:P1}   {tests[i].Change,-30} {after[i].WinRateA,6:P1}");
            return 0;
        }

        /// <summary>
        /// The balance dashboard: every deck against every deck (and each mirror), Greedy bots. Prints a win-rate matrix,
        /// each deck's average against the other decks, the spread (mean distance from 50%), mirror first-player win%,
        /// game length and mana left unspent per turn. With -out, appends the summary to that file.
        /// </summary>
        private static int Balance(int games, bool runeterra, string output)
        {
            var decks = Experiments.PrototypeDecks();
            var configs = new List<MatchConfig>();
            for (int i = 0; i < decks.Length; i++)
                for (int j = i; j < decks.Length; j++)
                    configs.Add(new MatchConfig
                    {
                        Name = decks[i].Name + " vs " + decks[j].Name, Format = Rules(runeterra), Games = games,
                        DeckAName = decks[i].Name, DeckA = decks[i].Cards, TavernDwellerA = decks[i].TavernDweller,
                        DeckBName = decks[j].Name, DeckB = decks[j].Cards, TavernDwellerB = decks[j].TavernDweller,
                    });
            var watch = Stopwatch.StartNew();
            var results = MatchRunner.RunAll(configs, CardPool.CreateDatabase());
            int n = decks.Length;
            var win = new double[n, n];
            var mirrors = new MatchResult[n];
            int k = 0;
            for (int i = 0; i < n; i++)
                for (int j = i; j < n; j++)
                {
                    var r = results[k++];
                    win[i, j] = r.WinRateA;
                    win[j, i] = 1 - r.WinRateA;
                    if (i == j) mirrors[i] = r;
                }
            string Short(string name) => name.Split(' ')[0].Replace("'s", "");
            var lines = new List<string>
            {
                $"Balance, {(runeterra ? "Runeterra" : "classic")} rules, {games} games per pairing ({watch.Elapsed.TotalSeconds:0}s). Row deck's win% against the column deck.",
                "            " + string.Join(" ", decks.Select(d => Short(d.Name).PadLeft(7))) + "   avg vs field",
            };
            double spread = 0, worst = 0;
            for (int i = 0; i < n; i++)
            {
                double avg = Enumerable.Range(0, n).Where(j => j != i).Average(j => win[i, j]);
                spread += Math.Abs(avg - 0.5) / n;
                for (int j = 0; j < n; j++) if (j != i) worst = Math.Max(worst, Math.Abs(win[i, j] - 0.5));
                lines.Add(Short(decks[i].Name).PadRight(11) + " " + string.Join(" ", Enumerable.Range(0, n).Select(j => i == j ? "     --" : (win[i, j] * 100).ToString("0.0").PadLeft(6) + "%"))
                          + $"   {avg * 100,5:0.0}%");
            }
            var all = results;
            double unspent = all.Sum(r => (double)r.UnspentMana) / all.Sum(r => r.GameLengths.Sum());
            lines.Add($"Spread (mean |avg - 50%|): {spread * 100:0.0} points · worst matchup: {(0.5 + worst) * 100:0.0}%");
            lines.Add("Mirrors, 1st player win%: " + string.Join("  ", Enumerable.Range(0, n).Select(i => Short(decks[i].Name) + " " + (mirrors[i].FirstPlayerWinRate * 100).ToString("0"))));
            lines.Add("Mirrors, turns: " + string.Join("  ", Enumerable.Range(0, n).Select(i => Short(decks[i].Name) + " " + mirrors[i].AvgTurns.ToString("0.0"))));
            lines.Add($"All games: {all.Average(r => r.AvgTurns):0.0} turns · unspent mana {unspent:0.00}/turn · lost to Gold cap {all.Sum(r => r.UnspentMana - r.GoldBanked) / (double)Math.Max(1, all.Sum(r => r.UnspentMana)) * 100:0}% · off-turn plays {all.Average(r => r.PerGame(r.InstantsOnOpponentsTurn + r.AbilitiesOnOpponentsTurn)):0.0}/game · draws {all.Sum(r => r.Draws)}");
            lines.ForEach(Console.WriteLine);
            if (output != null) File.AppendAllLines(output, lines.Prepend("").Prepend("### " + DateTime.Now.ToString("HH:mm")));
            return 0;
        }

        /// <summary>One game between two GreedyBots, written out turn by turn with the board at each turn start.</summary>
        private static int Trace(string deckArg, ulong seed, bool runeterra, string output, string passArg, string swapArg)
        {
            var decks = Experiments.PrototypeDecks();
            var pick = deckArg.Split(',').Select(int.Parse).ToArray();
            // -pass 1,0: P1 plays the deck-pass version of its list.
            var pass = passArg.Split(',').Select(v => v == "1").ToArray();
            for (int i = 0; i < 2; i++)
                if (pass[i]) decks[pick[i]] = new Experiments.Deck
                {
                    Name = decks[pick[i]].Name + " (new)", Cards = Swap(decks[pick[i]].Cards, DeckPass[decks[pick[i]].Name]),
                    TavernDweller = decks[pick[i]].TavernDweller,
                };
            // -swap out:in replaces a card (all copies) in P1's deck.
            if (swapArg != null)
            {
                var parts = swapArg.Split(':');
                decks[pick[0]] = new Experiments.Deck
                {
                    Name = decks[pick[0]].Name + " (" + parts[1] + ")", Cards = Swap(decks[pick[0]].Cards, (new[] { parts[0] }, new[] { parts[1] })),
                    TavernDweller = decks[pick[0]].TavernDweller,
                };
            }
            var db = CardPool.CreateDatabase();
            var engine = new GameEngine(db);
            var text = new GameText(db);
            var events = new List<GameEvent>();
            var state = engine.CreateGame(Rules(runeterra), new[]
            {
                new PlayerSetup { Deck = decks[pick[0]].Cards, TavernDwellerId = decks[pick[0]].TavernDweller },
                new PlayerSetup { Deck = decks[pick[1]].Cards, TavernDwellerId = decks[pick[1]].TavernDweller },
            }, seed, events);
            var bot = new GreedyBot(engine);
            var lines = new List<string>
            {
                $"Trace: {decks[pick[0]].Name} (P1) vs {decks[pick[1]].Name} (P2), seed {seed}, {(runeterra ? "Runeterra" : "classic")} rules. First player: {state.ActivePlayer}",
            };
            text.Remember(state, events);
            int turn = -1;
            while (!state.IsGameOver && state.TurnNumber <= 120)
            {
                if (state.TurnNumber != turn && state.Step == Step.Main1)
                {
                    turn = state.TurnNumber;
                    lines.Add("");
                    lines.Add($"=== Turn {turn}, round {state.RoundNumber}, {state.ActivePlayer} active" + (runeterra ? $", attack token {state.Players[state.RoundLeaderSeat].Id}" : ""));
                    foreach (var p in state.Players)
                    {
                        lines.Add($"  {p.Id}: life {p.Life}, mana {p.Mana}/{p.MaxMana}, Gold {p.Gold}, hand {p.Hand.Count}: "
                                  + string.Join(", ", p.Hand.Select(c => text.Name(c.DefinitionId) + "(" + db.Get(c.DefinitionId).Cost + ")")));
                        lines.Add("     board: " + string.Join(" | ", p.Battlefield.Select(c => text.Describe(state, c, multiline: false))));
                    }
                }
                var who = engine.WaitingOn(state).Value;
                var action = bot.Choose(state, who);
                string desc = text.Describe(state, action);
                text.Remember(state);
                var ev = engine.Apply(state, action);
                text.Remember(state, ev);
                if (action.Kind != ActionKind.PassPriority) lines.Add($"> {who} [{GameText.StepName(state.Step)}]: {desc}");
                foreach (var e in ev)
                {
                    var line = text.Describe(state, e);
                    if (line != null && !line.StartsWith("===")) lines.Add("    " + line);
                }
            }
            lines.Add("");
            lines.Add($"END turn {state.TurnNumber}: winners {string.Join(",", state.Winners)}, life {string.Join(" vs ", state.Players.Select(p => p.Life))}");
            if (output != null) File.WriteAllLines(output, lines);
            else lines.ForEach(Console.WriteLine);
            return 0;
        }

        /// <summary>The folder that holds Assets/ (searched upward from the working directory).</summary>
        private static string RepoRoot()
        {
            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
                if (Directory.Exists(Path.Combine(dir.FullName, "Assets", "Rules"))) return dir.FullName;
            throw new InvalidOperationException("Run this from inside the Restarted-Tavern repository.");
        }
    }
}
