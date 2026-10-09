using System;
using System.Diagnostics;
using System.IO;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.SimRunner
{
    /// <summary>
    /// The simulation report outside Unity (same code as SimulationMenu.RunReport, faster runtime).
    /// Arguments: [-simGames N] [-simSections "round,cap"] [-out path]. Default output: docs/playtest/SIMULATION_REPORT.md.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            int games = 500;
            string[] only = null;
            string output = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-simGames") int.TryParse(args[i + 1], out games);
                if (args[i] == "-simSections") only = args[i + 1].ToLowerInvariant().Split(',');
                if (args[i] == "-out") output = args[i + 1];
            }
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

        /// <summary>The folder that holds Assets/ (searched upward from the working directory).</summary>
        private static string RepoRoot()
        {
            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
                if (Directory.Exists(Path.Combine(dir.FullName, "Assets", "Rules"))) return dir.FullName;
            throw new InvalidOperationException("Run this from inside the Restarted-Tavern repository.");
        }
    }
}
