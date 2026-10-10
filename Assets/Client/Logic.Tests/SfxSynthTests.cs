using System;
using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Client.Logic.Tests
{
    public class SfxSynthTests
    {
        private static readonly string[] Factions =
            { "goobers", "evergrowing_wild", "glitterworld", "sensationalists", "shadow_money_wizards", "neutral" };

        [Test]
        public void EverySound_IsAudible_InRange_AndShort()
        {
            foreach (SfxKind kind in Enum.GetValues(typeof(SfxKind)))
                foreach (var f in Factions)
                {
                    var s = SfxSynth.Render(kind, f);
                    Assert.Greater(s.Length, 1000, kind + " " + f);
                    Assert.LessOrEqual(s.Length / (double)SfxSynth.SampleRate, 1.51, kind + " " + f);
                    float peak = s.Max(x => Math.Abs(x));
                    Assert.LessOrEqual(peak, 0.81f, kind + " " + f);
                    // Every sound about as loud as the others (RMS in dB): smooth pads have low peaks, so loudness is RMS.
                    double rmsDb = 20 * Math.Log10(Math.Sqrt(s.Average(x => (double)x * x)));
                    Assert.That(rmsDb, Is.InRange(-24.0, -15.0), kind + " " + f);
                    Assert.IsFalse(s.Any(float.IsNaN), kind + " " + f);
                    // Ends quietly (no click when it stops).
                    Assert.Less(s.Skip(s.Length - 50).Max(x => Math.Abs(x)), 0.1f, kind + " " + f + " ends abruptly");
                }
        }

        /// <summary>The same kind of card sounds different per faction (user: "a bit of variation per faction").</summary>
        [Test]
        public void Factions_SoundDifferent()
        {
            foreach (var kind in new[] { SfxKind.Creature, SfxKind.Instant, SfxKind.Sorcery, SfxKind.Relic, SfxKind.Curse, SfxKind.Ability, SfxKind.Power, SfxKind.Death })
                for (int i = 0; i < Factions.Length; i++)
                    for (int j = i + 1; j < Factions.Length; j++)
                    {
                        var a = SfxSynth.Render(kind, Factions[i]);
                        var b = SfxSynth.Render(kind, Factions[j]);
                        int n = Math.Min(a.Length, b.Length);
                        double diff = 0;
                        for (int k = 0; k < n; k++) diff += Math.Abs(a[k] - b[k]);
                        Assert.Greater(diff / n, 0.01, kind + ": " + Factions[i] + " vs " + Factions[j]);
                    }
        }

        [Test]
        public void Render_IsDeterministic()
        {
            CollectionAssert.AreEqual(SfxSynth.Render(SfxKind.Creature, "goobers"), SfxSynth.Render(SfxKind.Creature, "goobers"));
        }
    }
}
