using System;
using System.Collections.Generic;
using System.IO;
using RestartedTavern.Client.Logic;
using UnityEditor;
using UnityEngine;

namespace RestartedTavern.Client.Editor
{
    /// <summary>
    /// Writes every game sound (<see cref="SfxSynth"/>) into one WAV to listen to: per faction the card kinds, ability
    /// and Power, then the shared sounds (pass, round start, countered, trigger). Restarted Tavern → Export Sound Preview,
    /// or headless with -executeMethod RestartedTavern.Client.Editor.SfxPreview.Export (writes Builds/sfx_preview.wav,
    /// and Builds/sfx/&lt;kind&gt;_&lt;faction&gt;.wav for Tools/SoundCheck).
    /// </summary>
    public static class SfxPreview
    {
        private static readonly string[] Factions =
            { "goobers", "evergrowing_wild", "glitterworld", "sensationalists", "shadow_money_wizards", "neutral" };

        [MenuItem("Restarted Tavern/Export Sound Preview")]
        public static void Export()
        {
            var all = new List<float>();
            void Add(float[] s, double gap) { all.AddRange(s); all.AddRange(new float[(int)(gap * SfxSynth.SampleRate)]); }
            var kinds = new[] { SfxKind.Creature, SfxKind.Instant, SfxKind.Sorcery, SfxKind.Equipment, SfxKind.Relic, SfxKind.Curse, SfxKind.Ability, SfxKind.Power, SfxKind.Death };
            foreach (var f in Factions)
            {
                foreach (var k in kinds) Add(SfxSynth.Render(k, f), 0.25);
                Add(Array.Empty<float>(), 0.8);
            }
            foreach (var k in new[] { SfxKind.Pass, SfxKind.Pass, SfxKind.RoundStart, SfxKind.Attack, SfxKind.Block, SfxKind.Equip, SfxKind.Countered, SfxKind.Trigger })
                Add(SfxSynth.Render(k, "neutral"), 0.4);

            string root = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Builds");
            WriteWav(Path.Combine(root, "sfx_preview.wav"), all);
            // One file per sound too, for Tools/SoundCheck (spectrograms and measurements).
            string dir = Path.Combine(root, "sfx");
            Directory.CreateDirectory(dir);
            foreach (SfxKind k in Enum.GetValues(typeof(SfxKind)))
                foreach (var f in Factions)
                    WriteWav(Path.Combine(dir, k + "_" + f + ".wav"), new List<float>(SfxSynth.Render(k, f)));
            Debug.Log("Sound preview: " + root);
        }

        private static void WriteWav(string path, List<float> all)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int n = all.Count;
                w.Write("RIFF".ToCharArray()); w.Write(36 + n * 2); w.Write("WAVE".ToCharArray());
                w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(SfxSynth.SampleRate); w.Write(SfxSynth.SampleRate * 2); w.Write((short)2); w.Write((short)16);
                w.Write("data".ToCharArray()); w.Write(n * 2);
                foreach (var x in all) w.Write((short)Mathf.Clamp(x * 32767f, -32768f, 32767f));
            }
        }
    }
}
