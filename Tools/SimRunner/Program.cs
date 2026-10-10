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
    ///   -trace     one bot game, readable, turn by turn. [-decks "0,1"] [-seed N] [-rules standard|mtg|rotation|classic] [-out path]
    ///   -h2h       the current GreedyBot against BotStyle.Baseline() in every mirror (A = current). [-simGames N] [-rules ...] [-off Switch]
    ///   -balance   every deck against every deck: win-rate matrix, spread, mirror first-player win%. [-simGames N] [-rules ...] [-out file]
    ///   -goingfirst  first-player win% per turn-structure variant (-rules standard: Runeterra rounds variants; -rules mtg: MTG-turn compensations)
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
            RulesName = Arg("-rules", "standard");
            // -data <folder>: a copy of StreamingAssets with changed card files (balance experiments).
            if (Arg("-data") != null) CardPool.DataRoot = Arg("-data");
            bool runeterra = RulesName != "classic";
            int games = int.Parse(Arg("-simGames", "500"));

            if (args.Contains("-trace")) return Trace(Arg("-decks", "0,1"), ulong.Parse(Arg("-seed", "1")), runeterra, Arg("-out"), Arg("-pass", "0,0"), Arg("-swap"));
            if (args.Contains("-h2h")) return HeadToHead(games, runeterra, Arg("-off"));
            if (args.Contains("-tweaks")) return TweakTest(games, runeterra);
            if (args.Contains("-goingfirst")) return GoingFirst(games);
            if (args.Contains("-scan")) return Scan(int.Parse(Arg("-games", "200")), Arg("-out"), Arg("-only")?.Split(','));
            if (args.Contains("-impact")) return Impact(int.Parse(Arg("-games", "300")), Arg("-decks", "0,1,2,3,4,5"));
            if (args.Contains("-optimize")) return Optimize(int.Parse(Arg("-games", "400")), Arg("-decks", "5"), int.Parse(Arg("-swaps", "4")), Arg("-out"), double.Parse(Arg("-target", "1"), System.Globalization.CultureInfo.InvariantCulture));
            if (args.Contains("-balance")) return Balance(games, runeterra, Arg("-out"), args.Contains("-cards"));
            if (args.Contains("-decktest")) return args.Contains("-singles") ? SwapTest(games, runeterra) : DeckTest(games, runeterra);
            return Report(games, Arg("-simSections")?.ToLowerInvariant().Split(','), Arg("-out"));
        }

        /// <summary>-rules: standard (default, Runeterra rounds), mtg (MTG turns), rotation (A B | B A + attack token), classic.</summary>
        private static string RulesName = "standard";

        private static FormatConfig Rules(bool runeterra) => RulesName switch
        {
            "classic" => FormatConfig.Classic(),
            "rotation" => FormatConfig.RuneterraRotation(),
            "mtg" => FormatConfig.MtgTurns(),
            _ => FormatConfig.Standard(),
        };

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
                var baseline = prop.GetValue(BotStyle.Baseline());
                // A switch that is off in both: B gets it turned on instead.
                prop.SetValue(style, Equals(baseline, prop.GetValue(style)) && baseline is bool on ? !on : baseline);
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
            Console.WriteLine($"Head-to-head, {RulesName} rules, {games} games per mirror ({watch.Elapsed.TotalSeconds:0}s). A = current bot, B = {B().Name}.");
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
            Console.WriteLine($"Deck pass test, {RulesName} rules, {games} games per row ({watch.Elapsed.TotalSeconds:0}s).");
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
        private static int Balance(int games, bool runeterra, string output, bool cards = false)
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
                $"Balance, {RulesName} rules, {games} games per pairing ({watch.Elapsed.TotalSeconds:0}s). Row deck's win% against the column deck.",
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
            if (cards) lines.AddRange(CardLines(decks, configs, results));
            lines.ForEach(Console.WriteLine);
            if (output != null) File.AppendAllLines(output, lines.Prepend("").Prepend("### " + DateTime.Now.ToString("HH:mm")));
            return 0;
        }

        /// <summary>
        /// Per deck, against the other decks (mirrors left out): each card's cast rate (share of games it was cast at least
        /// once), the deck's win% in games it was cast and in games it wasn't. "Power" is the Tavern Dweller's Power.
        /// Cards cast in long games look better (more cards get cast), so compare cards within a deck, not across decks.
        /// </summary>
        private static List<string> CardLines(Experiments.Deck[] decks, List<MatchConfig> configs, List<MatchResult> results)
        {
            var db = CardPool.CreateDatabase();
            var text = new GameText(db);
            var lines = new List<string>();
            foreach (var deck in decks)
            {
                var games = new Dictionary<string, int>();
                var wins = new Dictionary<string, int>();
                int decided = 0, deckWins = 0;
                for (int i = 0; i < configs.Count; i++)
                {
                    var c = configs[i];
                    var r = results[i];
                    if (c.DeckAName == c.DeckBName) continue;
                    string side = c.DeckAName == deck.Name ? "A:" : c.DeckBName == deck.Name ? "B:" : null;
                    if (side == null) continue;
                    decided += r.Decided;
                    deckWins += side == "A:" ? r.WinsA : r.WinsB;
                    foreach (var kv in r.CardGames)
                        if (kv.Key.StartsWith(side))
                        {
                            string id = kv.Key.Substring(2);
                            games[id] = games.TryGetValue(id, out int n) ? n + kv.Value : kv.Value;
                            r.CardWins.TryGetValue(kv.Key, out int w);
                            wins[id] = wins.TryGetValue(id, out int m) ? m + w : w;
                        }
                }
                lines.Add("");
                lines.Add($"{deck.Name}: {100.0 * deckWins / Math.Max(1, decided):0.0}% against the field ({decided} games). Card: copies, cast in % of games, win% when cast / not cast (diff)");
                foreach (var id in deck.Cards.Distinct().Append("power").OrderByDescending(id => games.TryGetValue(id, out int g) ? g : 0))
                {
                    games.TryGetValue(id, out int g);
                    wins.TryGetValue(id, out int w);
                    double castWr = g == 0 ? 0 : 100.0 * w / g;
                    double notWr = decided - g == 0 ? 0 : 100.0 * (deckWins - w) / (decided - g);
                    string name = id == "power" ? "(Power)" : text.Name(id);
                    int copies = id == "power" ? 0 : deck.Cards.Count(c => c == id);
                    lines.Add($"  {name,-28} {copies}x  cast {100.0 * g / Math.Max(1, decided),5:0}%   {castWr,5:0.0}% / {notWr,5:0.0}%  ({castWr - notWr,6:+0.0;-0.0})");
                }
            }
            return lines;
        }

        /// <summary>
        /// Going-first fixes under MTG turn order (A B A B, everyone attacks every turn) with the round pool, Gold first and
        /// Gold cap 3: first-player win% in every mirror for each variant.
        /// </summary>
        private static int GoingFirst(int games)
        {
            FormatConfig Base()
            {
                return FormatConfig.MtgTurns();
            }
            if (RulesName == "standard") return GoingFirstRounds(games);
            var variants = new (string Name, Action<FormatConfig> Tweak)[]
            {
                ("draw skip (MTG)", f => { }),
                ("Runeterra rotation (old Standard)", f => { f.RotateRoundLeader = true; f.AttackToken = true; }),
                ("rotation, no token", f => { f.RotateRoundLeader = true; }),
                ("draw skip + 2nd +1 mana t2 + 1 Gold", f => { f.SecondPlayerFirstTurnBonusMana = 1; f.SecondPlayerStartingGold = 1; }),
                ("draw skip + 2nd +1 mana x2", f => { f.SecondPlayerFirstTurnBonusMana = 1; f.SecondPlayerBonusTurns = 2; }),
                ("draw skip + 2nd +1 mana x3", f => { f.SecondPlayerFirstTurnBonusMana = 1; f.SecondPlayerBonusTurns = 3; }),
                ("draw skip + 2nd +1 mana x4", f => { f.SecondPlayerFirstTurnBonusMana = 1; f.SecondPlayerBonusTurns = 4; }),
                ("draw skip + 2nd +1 mana x3 + 1 card", f => { f.SecondPlayerFirstTurnBonusMana = 1; f.SecondPlayerBonusTurns = 3; f.SecondPlayerExtraCards = 1; }),
                ("until-next + 2nd +1 mana x3", f => { f.ManaPerRound = false; f.ManaUntilYourNextTurn = true; f.SecondPlayerFirstTurnBonusMana = 1; f.SecondPlayerBonusTurns = 3; }),
                ("draw skip + 2nd +1 mana x3, Control", f => { f.SecondPlayerFirstTurnBonusMana = 1; f.SecondPlayerBonusTurns = 3; }),
            };
            var decks = Experiments.PrototypeDecks();
            var configs = new List<MatchConfig>();
            foreach (var v in variants)
                foreach (var d in decks)
                {
                    var f = Base();
                    v.Tweak(f);
                    configs.Add(new MatchConfig
                    {
                        Name = v.Name + " | " + d.Name, Format = f, Games = games,
                        StyleA = v.Name.Contains("Control") ? BotStyle.Control() : BotStyle.Greedy(),
                        StyleB = v.Name.Contains("Control") ? BotStyle.Control() : BotStyle.Greedy(),
                        DeckAName = d.Name, DeckA = d.Cards, TavernDwellerA = d.TavernDweller,
                        DeckBName = d.Name, DeckB = d.Cards, TavernDwellerB = d.TavernDweller,
                    });
                }
            var watch = Stopwatch.StartNew();
            var results = MatchRunner.RunAll(configs, CardPool.CreateDatabase());
            string Short(string name) => name.Split(' ')[0].Replace("'s", "");
            Console.WriteLine($"Going first, MTG turn order + round pool + Gold first, {games} games per mirror ({watch.Elapsed.TotalSeconds:0}s). 1st player win%, then avg distance from 50% and avg turns.");
            Console.WriteLine("".PadRight(38) + string.Join(" ", decks.Select(d => Short(d.Name).PadLeft(7))) + "    off50  turns");
            for (int v = 0; v < variants.Length; v++)
            {
                var row = results.Skip(v * decks.Length).Take(decks.Length).ToList();
                Console.WriteLine(variants[v].Name.PadRight(38) + string.Join(" ", row.Select(r => (r.FirstPlayerWinRate * 100).ToString("0.0").PadLeft(7)))
                                  + $"   {row.Average(r => Math.Abs(r.FirstPlayerWinRate - 0.5)) * 100,6:0.0}  {row.Average(r => r.AvgTurns),5:0.0}");
            }
            return 0;
        }

        /// <summary>Legends of Runeterra rounds (alternating actions): first-player win% and game length per variant.</summary>
        private static int GoingFirstRounds(int games)
        {
            var variants = new (string Name, Action<FormatConfig> Tweak)[]
            {
                ("MTG turns, draw skip", f => Copy(FormatConfig.MtgTurns(), f)),
                ("LoR rounds (Standard)", f => { }),
                ("LoR, everyone attacks once a round", f => f.AttackToken = false),
                ("LoR, leader skips round-1 draw", f => f.FirstPlayerSkipsDraw = true),
                ("LoR, Gold cap 5", f => f.GoldCap = 5),
            };
            var decks = Experiments.PrototypeDecks();
            var configs = new List<MatchConfig>();
            foreach (var v in variants)
                foreach (var d in decks)
                {
                    var f = FormatConfig.Standard();
                    v.Tweak(f);
                    configs.Add(new MatchConfig
                    {
                        Name = v.Name + " | " + d.Name, Format = f, Games = games,
                        DeckAName = d.Name, DeckA = d.Cards, TavernDwellerA = d.TavernDweller,
                        DeckBName = d.Name, DeckB = d.Cards, TavernDwellerB = d.TavernDweller,
                    });
                }
            var watch = Stopwatch.StartNew();
            var results = MatchRunner.RunAll(configs, CardPool.CreateDatabase());
            string Short(string name) => name.Split(' ')[0].Replace("'s", "");
            Console.WriteLine($"Going first, mirrors, {games} games each ({watch.Elapsed.TotalSeconds:0}s). 1st player win%; off50 = avg distance from 50%; length = turns (MTG) or rounds (LoR, a round = everyone's turn); wasted = mana lost to the Gold cap.");
            Console.WriteLine("".PadRight(38) + string.Join(" ", decks.Select(d => Short(d.Name).PadLeft(7))) + "    off50  length  long%  wasted");
            for (int v = 0; v < variants.Length; v++)
            {
                var row = results.Skip(v * decks.Length).Take(decks.Length).ToList();
                Console.WriteLine(variants[v].Name.PadRight(38) + string.Join(" ", row.Select(r => (r.FirstPlayerWinRate * 100).ToString("0.0").PadLeft(7)))
                                  + $"   {row.Average(r => Math.Abs(r.FirstPlayerWinRate - 0.5)) * 100,6:0.0}  {row.Average(r => r.AvgTurns),6:0.0}  {row.Average(r => r.LongGameShare) * 100,5:0}  {row.Average(r => r.GoldWastedShare) * 100,5:0}%");
            }
            return 0;
        }

        private static void Copy(FormatConfig from, FormatConfig to)
        {
            foreach (var prop in typeof(FormatConfig).GetProperties())
                if (prop.CanWrite) prop.SetValue(to, prop.GetValue(from));
        }

        /// <summary>
        /// Pool-wide card power scan (balance, 2026-10-10). Each prototype deck gets one open slot: its weakest card (the one
        /// whose replacement by a plain filler, Hired Sellsword 2 mana 2/3, helps most). Every card in the pool goes into that
        /// slot (4 copies) of every deck that may play it, against the other five decks. A card's power is the deck's win% with
        /// it minus the win% with the filler, averaged over the decks that can play it. Writes a Markdown table with -out.
        /// </summary>
        private static int Scan(int games, string output, string[] only = null)
        {
            var db = CardPool.CreateDatabase();
            var decks = Experiments.PrototypeDecks();
            var watch = Stopwatch.StartNew();
            bool Legal(List<string> list, string dweller)
            {
                try { DeckValidator.Validate(db, FormatConfig.Standard(), list, dweller); return true; }
                catch (ArgumentException) { return false; }
            }
            MatchConfig Config(int host, List<string> list, MatchConfigOpponent o, ulong seed) => new MatchConfig
            {
                Name = decks[host].Name, Format = FormatConfig.Standard(), Games = games, FirstSeed = seed,
                DeckAName = decks[host].Name, DeckA = list, TavernDwellerA = decks[host].TavernDweller,
                DeckBName = o.Name, DeckB = o.Cards, TavernDwellerB = o.Dweller,
            };
            List<MatchConfigOpponent> Opponents(int host) => decks.Where((d, i) => i != host)
                .Select(d => new MatchConfigOpponent { Name = d.Name, Cards = d.Cards, Dweller = d.TavernDweller }).ToList();

            // 1. Each host's open slot.
            var openSlot = new string[decks.Length];
            {
                var configs = new List<MatchConfig>();
                var keys = new List<(int Host, string Card)>();
                for (int h = 0; h < decks.Length; h++)
                    foreach (var id in decks[h].Cards.Distinct())
                    {
                        keys.Add((h, id));
                        foreach (var o in Opponents(h)) configs.Add(Config(h, Swap(decks[h].Cards, (new[] { id }, new[] { "hired_sellsword" })), o, 7));
                    }
                var results = MatchRunner.RunAll(configs, db);
                for (int h = 0; h < decks.Length; h++)
                {
                    var best = keys.Select((k, i) => (k, rate: results.Skip(i * 5).Take(5).Average(r => r.WinRateA)))
                        .Where(x => x.k.Host == h).OrderByDescending(x => x.rate).First();
                    openSlot[h] = best.k.Card;
                    Console.WriteLine($"[{watch.Elapsed.TotalSeconds,4:0}s] {decks[h].Name}: open slot = {db.Get(openSlot[h]).Name}");
                }
            }

            // 2. Every card in every host that may play it, plus the filler baseline per host.
            var cardIds = CardPool.All().Where(c => !c.IsToken && !c.IsTavernDweller).Select(c => c.Id)
                .Where(id => only == null || id == "hired_sellsword" || only.Contains(id)).ToList();
            var runs = new List<(int Host, string Card)>();
            for (int h = 0; h < decks.Length; h++)
            {
                runs.Add((h, "hired_sellsword"));
                foreach (var id in cardIds)
                {
                    if (id == "hired_sellsword" || decks[h].Cards.Contains(id) && id != openSlot[h]) continue;
                    var list = Swap(decks[h].Cards, (new[] { openSlot[h] }, new[] { id }));
                    if (Legal(list, decks[h].TavernDweller)) runs.Add((h, id));
                }
            }
            var all = new List<MatchConfig>();
            foreach (var run in runs)
                foreach (var o in Opponents(run.Host))
                    all.Add(Config(run.Host, Swap(decks[run.Host].Cards, (new[] { openSlot[run.Host] }, new[] { run.Card })), o, 11));
            Console.WriteLine($"[{watch.Elapsed.TotalSeconds,4:0}s] {runs.Count} card/deck pairs, {all.Count * games} games");
            var res = MatchRunner.RunAll(all, db);
            var rate = runs.Select((r, i) => res.Skip(i * 5).Take(5).Average(x => x.WinRateA)).ToList();
            var filler = Enumerable.Range(0, decks.Length).Select(h => rate[runs.FindIndex(r => r.Host == h && r.Card == "hired_sellsword")]).ToArray();

            string Short(int h) => decks[h].Name.Split(' ')[0].Replace("'s", "");
            var rows = cardIds.Where(id => id != "hired_sellsword")
                .Select(id => (id, per: runs.Select((r, i) => (r, i)).Where(x => x.r.Card == id).Select(x => (x.r.Host, delta: rate[x.i] - filler[x.r.Host])).ToList()))
                .Where(x => x.per.Count > 0)
                .OrderByDescending(x => x.per.Average(p => p.delta)).ToList();
            var lines = new List<string>
            {
                "# Card power scan",
                "",
                $"Each card (4 copies) in the open slot of every prototype deck that may play it, against the other five decks, {games} games per opponent. "
                + "Power = the deck's win% with the card minus with a plain filler (Hired Sellsword, 2 mana 2/3) in the same slot. "
                + "Open slots: " + string.Join(", ", Enumerable.Range(0, decks.Length).Select(h => Short(h) + " " + db.Get(openSlot[h]).Name + $" (filler {filler[h] * 100:0.0}%)")) + ". "
                + "Noise is about ±3 points per deck. Standard rules (Runeterra rounds), Greedy bots.",
                "",
                "| Card | Cost | Type | Rarity | Faction | Power | Per deck |",
                "|---|---|---|---|---|---|---|",
            };
            foreach (var (id, per) in rows)
            {
                var d = db.Get(id);
                lines.Add($"| {d.Name} | {d.Cost} | {d.Type} | {d.Rarity} | {d.Faction} | {per.Average(p => p.delta) * 100:+0.0;-0.0} | "
                          + string.Join(", ", per.Select(p => Short(p.Host) + $" {p.delta * 100:+0;-0}")) + " |");
            }
            Console.WriteLine($"[{watch.Elapsed.TotalSeconds,4:0}s] done");
            if (output != null) File.WriteAllText(output, string.Join("\n", lines) + "\n");
            else lines.ForEach(Console.WriteLine);
            return 0;
        }

        private sealed class MatchConfigOpponent
        {
            public string Name;
            public List<string> Cards;
            public string Dweller;
        }

        /// <summary>
        /// Card impact (balance): for each deck, each card's contribution = the deck's win% against the field minus its win%
        /// with all copies of that card replaced by a plain filler (Hired Sellsword, 2 mana 2/3). Big positive numbers are the
        /// cards carrying the deck (nerf candidates in strong decks); negative numbers are cards worse than the filler.
        /// </summary>
        private static int Impact(int games, string deckArg)
        {
            var db = CardPool.CreateDatabase();
            var decks = Experiments.PrototypeDecks();
            foreach (int index in deckArg.Split(',').Select(int.Parse))
            {
                var deck = decks[index];
                var opponents = decks.Where((d, i) => i != index).ToList();
                var distinct = deck.Cards.Distinct().ToList();
                var lists = new List<List<string>> { deck.Cards };
                lists.AddRange(distinct.Select(id => Swap(deck.Cards, (new[] { id }, new[] { "hired_sellsword" }))));
                var configs = new List<MatchConfig>();
                foreach (var list in lists)
                    foreach (var o in opponents)
                        configs.Add(new MatchConfig
                        {
                            Name = deck.Name, Format = FormatConfig.Standard(), Games = games,
                            DeckAName = deck.Name, DeckA = list, TavernDwellerA = deck.TavernDweller,
                            DeckBName = o.Name, DeckB = o.Cards, TavernDwellerB = o.TavernDweller,
                        });
                var results = MatchRunner.RunAll(configs, db);
                var rates = Enumerable.Range(0, lists.Count)
                    .Select(i => results.Skip(i * opponents.Count).Take(opponents.Count).Average(r => r.WinRateA)).ToList();
                Console.WriteLine($"{deck.Name}: {rates[0] * 100:0.0}% against the field ({games} games per opponent). Contribution of each card vs a 2/3 filler:");
                foreach (var (id, rate) in distinct.Select((id, i) => (id, rates[i + 1])).OrderByDescending(x => rates[0] - x.Item2))
                {
                    var def = db.Get(id);
                    Console.WriteLine($"  {def.Name,-32} {def.Cost} {(rates[0] - rate) * 100,6:+0.0;-0.0}");
                }
            }
            return 0;
        }

        /// <summary>
        /// Deck tuning by measurement (balance, 2026-10-10). Each step: rank the deck's cards by how much its win% against the
        /// field rises when all copies are replaced by a plain filler (Hired Sellsword, 100 games per opponent); for the two
        /// weakest, screen every legal card of the Tavern Dweller's factions and Neutral in that slot (40 games per opponent);
        /// confirm the 8 best screened swaps and the current list on fresh seeds (-games per opponent, default 400) and keep the
        /// best if it beats the current list by more than 1.5 points. Repeats up to -swaps times. Opponents keep their lists.
        /// Only deck lists change, never cards.
        /// </summary>
        private static int Optimize(int games, string deckArg, int swaps, string output, double target = 1)
        {
            const int RankGames = 100, ScreenGames = 40, Finalists = 8;
            var db = CardPool.CreateDatabase();
            var decks = Experiments.PrototypeDecks();
            var log = new List<string>();
            var watch = Stopwatch.StartNew();
            void Log(string line)
            {
                line = $"[{watch.Elapsed.TotalSeconds,4:0}s] " + line;
                Console.WriteLine(line);
                Console.Out.Flush();
                log.Add(line);
            }
            foreach (int index in deckArg.Split(',').Select(int.Parse))
            {
                var deck = decks[index];
                var current = new List<string>(deck.Cards);
                var opponents = decks.Where((d, i) => i != index).ToList();
                var legal = CardPool.All().Where(c => !c.IsToken && !c.IsTavernDweller).Select(c => c.Id)
                    .Where(id => Legal(Swap(current, (new[] { current[0] }, new[] { id }))) || current.Contains(id)).ToList();

                bool Legal(List<string> list)
                {
                    try { DeckValidator.Validate(db, FormatConfig.Standard(), list, deck.TavernDweller); return true; }
                    catch (ArgumentException) { return false; }
                }

                // A batch of lists, all against the field at once (one parallel pool): win% against the field for each.
                List<double> Evaluate(List<List<string>> lists, int perOpponent, ulong seed)
                {
                    var configs = new List<MatchConfig>();
                    foreach (var list in lists)
                        foreach (var o in opponents)
                            configs.Add(new MatchConfig
                            {
                                Name = deck.Name, Format = FormatConfig.Standard(), Games = perOpponent,
                                DeckAName = deck.Name, DeckA = list, TavernDwellerA = deck.TavernDweller,
                                DeckBName = o.Name, DeckB = o.Cards, TavernDwellerB = o.TavernDweller, FirstSeed = seed,
                            });
                    var results = MatchRunner.RunAll(configs, db);
                    return Enumerable.Range(0, lists.Count)
                        .Select(i => results.Skip(i * opponents.Count).Take(opponents.Count).Average(r => r.WinRateA)).ToList();
                }

                double baseline = Evaluate(new List<List<string>> { current }, games, 1)[0];
                Log($"{deck.Name}: {baseline * 100:0.0}% against the field ({games} games per opponent), {legal.Count} legal cards");
                for (int step = 0; step < swaps && baseline < target; step++)
                {
                    ulong seed = 1_000_000UL * (ulong)(step + 2);
                    var distinct = current.Distinct().ToList();
                    var withoutEach = Evaluate(distinct.Select(id => Swap(current, (new[] { id }, new[] { "hired_sellsword" }))).ToList(), RankGames, seed);
                    var weakest = distinct.Select((id, i) => (id, rate: withoutEach[i])).OrderByDescending(x => x.rate).Take(2).ToList();
                    Log("  weakest: " + string.Join(", ", weakest.Select(w => $"{db.Get(w.id).Name} (filler {w.rate * 100:0.0}%)")));

                    var swapsToTry = weakest.SelectMany(w => legal.Where(id => !current.Contains(id)).Select(id => (Out: w.id, In: id))).ToList();
                    var screened = Evaluate(swapsToTry.Select(x => Swap(current, (new[] { x.Out }, new[] { x.In }))).ToList(), ScreenGames, seed + 1);
                    var finalists = swapsToTry.Select((x, i) => (x.Out, x.In, rate: screened[i])).OrderByDescending(x => x.rate).Take(Finalists).ToList();

                    var confirmLists = new List<List<string>> { current };
                    confirmLists.AddRange(finalists.Select(f => Swap(current, (new[] { f.Out }, new[] { f.In }))));
                    var confirmed = Evaluate(confirmLists, games, seed + 2);
                    double now = confirmed[0];
                    int bestIndex = Enumerable.Range(0, finalists.Count).OrderByDescending(i => confirmed[i + 1]).First();
                    var best = finalists[bestIndex];
                    double bestRate = confirmed[bestIndex + 1];
                    Log("  finalists: " + string.Join(", ", finalists.Select((f, i) => $"{db.Get(f.In).Name} for {db.Get(f.Out).Name} {confirmed[i + 1] * 100:0.0}%"))
                        + $" (current {now * 100:0.0}%)");
                    if (bestRate < now + 0.015)
                    {
                        Log("  no swap beats the list by 1.5 points; stopping");
                        baseline = now;
                        break;
                    }
                    current = Swap(current, (new[] { best.Out }, new[] { best.In }));
                    baseline = bestRate;
                    Log($"  swap {db.Get(best.Out).Name} -> {db.Get(best.In).Name}: {now * 100:0.0}% -> {bestRate * 100:0.0}%");
                }
                Log($"  {deck.Name} final: {baseline * 100:0.0}%  list: " + string.Join(", ", current.Distinct().Select(id => db.Get(id).Name)));
            }
            if (output != null) File.AppendAllLines(output, log);
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
                $"Trace: {decks[pick[0]].Name} (P1) vs {decks[pick[1]].Name} (P2), seed {seed}, {RulesName} rules. First player: {state.ActivePlayer}",
            };
            text.Remember(state, events);
            int turn = -1;
            while (!state.IsGameOver && state.TurnNumber <= 120)
            {
                if (state.TurnNumber != turn && state.Step == Step.Main1)
                {
                    turn = state.TurnNumber;
                    lines.Add("");
                    lines.Add($"=== Turn {turn}, round {state.RoundNumber}, {state.ActivePlayer} active" + (state.Format.AttackToken ? $", attack token {state.Players[state.RoundLeaderSeat].Id}" : ""));
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
                // A pass with an empty Chain in the action phase hands the action on (checked before the pass is applied).
                bool passesAction = state.Format.AlternatingActions && state.Step == Step.Main1 && state.Chain.Count == 0 && state.Pending == null;
                text.Remember(state);
                var ev = engine.Apply(state, action);
                text.Remember(state, ev);
                if (action.Kind != ActionKind.PassPriority || passesAction) lines.Add($"> {who} [{GameText.StepName(state.Step)}]: {(action.Kind == ActionKind.PassPriority ? "Pass the action" : desc)}");
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
