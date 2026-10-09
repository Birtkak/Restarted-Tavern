using System;
using System.Diagnostics;
using System.IO;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RestartedTavern.Client.Editor
{
    /// <summary>
    /// Runs the balance experiments and writes docs/playtest/SIMULATION_REPORT.md. Headless:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod RestartedTavern.Client.Editor.SimulationMenu.RunReport [-simGames N]
    /// </summary>
    public static class SimulationMenu
    {
        private const int DefaultGames = 1000;

        [MenuItem("Restarted Tavern/Run Simulation Report")]
        public static void RunReport()
        {
            int games = DefaultGames;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-simGames") int.TryParse(args[i + 1], out games);

            var watch = Stopwatch.StartNew();
            var sections = Experiments.Build(games);
            Experiments.Run(sections, PrototypeCards.CreateDatabase(),
                r => Debug.Log($"[sim] {r.Config.Name}: A {r.WinRateA:P1}, first {r.FirstPlayerWinRate:P1}, {r.AvgTurns:0.0} turns ({watch.Elapsed.TotalSeconds:0}s)"));

            var path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "docs", "playtest", "SIMULATION_REPORT.md");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, Experiments.ToMarkdown(sections, games, watch.Elapsed).Replace("\r\n", "\n"));
            Debug.Log("[sim] Report written to " + path);
        }
    }
}
