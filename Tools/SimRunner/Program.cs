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

            if (args.Contains("-trace")) return Trace(Arg("-decks", "0,1"), ulong.Parse(Arg("-seed", "1")), runeterra, Arg("-out"));
            if (args.Contains("-h2h")) return HeadToHead(games, runeterra, Arg("-off"));
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

        /// <summary>One game between two GreedyBots, written out turn by turn with the board at each turn start.</summary>
        private static int Trace(string deckArg, ulong seed, bool runeterra, string output)
        {
            var decks = Experiments.PrototypeDecks();
            var pick = deckArg.Split(',').Select(int.Parse).ToArray();
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
