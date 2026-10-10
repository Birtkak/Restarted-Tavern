using System;

namespace RestartedTavern.Client.Logic
{
    /// <summary>
    /// What a sound is for. The names follow Legends of Runeterra's sound events (its audio files are named
    /// summon, arrival, attack declare, block declare, death, spell resolve, levelup...; LoR wiki, LoR audio category).
    /// </summary>
    public enum SfxKind
    {
        /// <summary>LoR "summon": a creature is played.</summary>
        Creature,
        /// <summary>A fast spell is cast.</summary>
        Instant,
        /// <summary>A slow spell is cast.</summary>
        Sorcery,
        Equipment,
        /// <summary>LoR's landmarks: a lasting non-creature permanent.</summary>
        Relic,
        Curse,
        Ability,
        /// <summary>A Tavern Dweller's Power (LoR's champion moments, "levelup").</summary>
        Power,
        Trigger,
        Equip,
        Countered,
        Pass,
        RoundStart,
        /// <summary>LoR "attack declare".</summary>
        Attack,
        /// <summary>LoR "block declare".</summary>
        Block,
        /// <summary>LoR "death".</summary>
        Death,
        /// <summary>The mouse moves onto a button: a faint high tick (playtest 2026-10-10_173612).</summary>
        UiHover,
        /// <summary>A button is pressed: a short wooden tap.</summary>
        UiClick,
    }

    /// <summary>
    /// The game's sound effects, synthesized in code (no audio files). Version 2 (user, 2026-10-10: "it's very arcady
    /// sounding, not smooth", with LoR as the reference): instead of raw square and saw waves, every voice is built
    /// from soft sine partials (brass, marimba, celesta, choir, harp, mallet), notes start softly, transitions are
    /// filtered-noise whooshes and low booms, and everything goes through a small hall reverb and a gentle low-pass. The
    /// kind sets the gesture, the faction sets the instrument and its chord. No Unity references (Client.Logic.Tests).
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 44100;

        /// <summary>A faction's instrument: partials (frequency ratio, level, how much faster it fades), chord, colour.</summary>
        private sealed class Voice
        {
            public (double Ratio, double Amp, double Decay)[] Partials;
            public double Root;
            public double Third, Fifth, Seventh;
            /// <summary>Attack time of a note (s): plucked instruments start fast, pads slowly.</summary>
            public double Attack;
            /// <summary>How long a note rings, as a decay rate (higher = shorter).</summary>
            public double Decay;
            public double Vibrato, VibratoRate;
            /// <summary>Extra detuned copies (chorus/choir width), as a ratio offset; 0 = none.</summary>
            public double Detune;
            /// <summary>A breath of filtered noise under each note (fire for Goobers, air for the choir).</summary>
            public double Breath;
            /// <summary>Wet level of the hall reverb.</summary>
            public double Reverb;
        }

        private static Voice VoiceOf(string faction)
        {
            switch (faction)
            {
                case "goobers": // warm, rough brass and a crackle of fire
                    return new Voice
                    {
                        Partials = new[] { (1.0, 1.0, 1.0), (2.0, 0.55, 1.3), (3.0, 0.35, 1.6), (4.0, 0.2, 2.0), (5.0, 0.12, 2.4), (6.0, 0.07, 2.8) },
                        Root = 196, Third = 1.26, Fifth = 1.5, Seventh = 2.0, Attack = 0.025, Decay = 5, Vibrato = 0.004, VibratoRate = 5.5,
                        Breath = 0.05, Reverb = 0.22,
                    };
                case "evergrowing_wild": // a deep wooden marimba, earthy
                    return new Voice
                    {
                        Partials = new[] { (1.0, 1.0, 1.0), (3.93, 0.3, 3.0), (9.2, 0.08, 6.0) },
                        Root = 147, Third = 1.25, Fifth = 1.5, Seventh = 2.0, Attack = 0.006, Decay = 4, Breath = 0.02, Reverb = 0.25,
                    };
                case "glitterworld": // celesta and glass: bright but soft, with a shimmer
                    return new Voice
                    {
                        Partials = new[] { (1.0, 1.0, 1.0), (2.0, 0.45, 1.5), (3.0, 0.2, 2.2), (4.16, 0.12, 3.0) },
                        Root = 523, Third = 1.26, Fifth = 1.5, Seventh = 1.888, Attack = 0.004, Decay = 4.5, Detune = 0.003, Reverb = 0.32,
                    };
                case "sensationalists": // a dark choir: detuned voices, slow vibrato, minor
                    return new Voice
                    {
                        Partials = new[] { (1.0, 1.0, 1.0), (2.0, 0.3, 1.2), (3.0, 0.14, 1.5), (5.0, 0.05, 2.0) },
                        Root = 196, Third = 1.189, Fifth = 1.498, Seventh = 1.682, Attack = 0.06, Decay = 3, Vibrato = 0.006, VibratoRate = 4.5,
                        Detune = 0.007, Breath = 0.04, Reverb = 0.35,
                    };
                case "shadow_money_wizards": // a harp with a major-seventh sparkle
                    return new Voice
                    {
                        Partials = new[] { (1.0, 1.0, 1.0), (2.0, 0.5, 1.6), (3.0, 0.25, 2.2), (4.0, 0.12, 3.0), (5.0, 0.06, 3.5) },
                        Root = 330, Third = 1.26, Fifth = 1.5, Seventh = 1.888, Attack = 0.004, Decay = 3.5, Detune = 0.002, Reverb = 0.3,
                    };
                default: // neutral: a soft mallet
                    return new Voice
                    {
                        Partials = new[] { (1.0, 1.0, 1.0), (2.0, 0.22, 2.0), (3.0, 0.06, 3.0) },
                        Root = 262, Third = 1.25, Fifth = 1.5, Seventh = 2.0, Attack = 0.008, Decay = 4.5, Reverb = 0.25,
                    };
            }
        }

        /// <summary>The samples of one sound: mono, <see cref="SampleRate"/>, levelled to about -18 dB RMS, peak at most 0.8.</summary>
        public static float[] Render(SfxKind kind, string faction)
        {
            var v = VoiceOf(faction ?? "neutral");
            var neutral = VoiceOf("neutral");
            double r = v.Root;
            var buf = new float[(int)(Length(kind) * SampleRate)];
            int seed = (int)kind * 31;
            foreach (char c in faction ?? "") seed = seed * 7 + c;
            var rng = new Random(seed);
            double reverb = v.Reverb;

            switch (kind)
            {
                case SfxKind.Creature: // summon: a low boom as it lands, then the root and fifth bloom
                    Boom(buf, 0, 0.5, 0.7, 70);
                    Note(buf, v, 0.02, 0.9, r, 0.5, rng);
                    Note(buf, v, 0.09, 0.9, r * v.Fifth, 0.4, rng);
                    Whoosh(buf, 0, 0.35, 0.12, 2500, 900, rng); // a settling air
                    break;
                case SfxKind.Instant: // fast spell: a quick rising whoosh that lands on a bright note
                    Whoosh(buf, 0, 0.28, 0.35, 500, 4000, rng);
                    Note(buf, v, 0.18, 0.7, r * 2, 0.4, rng);
                    Note(buf, v, 0.22, 0.6, r * 2 * v.Fifth, 0.28, rng);
                    break;
                case SfxKind.Sorcery: // slow spell: a long swell into a full chord
                    Whoosh(buf, 0, 0.7, 0.3, 300, 2200, rng);
                    Pad(buf, v, 0.1, 1.1, r, 0.32, rng);
                    Pad(buf, v, 0.14, 1.05, r * v.Third, 0.26, rng);
                    Pad(buf, v, 0.18, 1.0, r * v.Fifth, 0.24, rng);
                    break;
                case SfxKind.Equipment: // a weapon: a muffled metal ring over a thud
                    Boom(buf, 0, 0.3, 0.5, 90);
                    Metal(buf, 0.01, 0.9, r * 1.5, 0.45);
                    Note(buf, v, 0.04, 0.7, r, 0.25, rng);
                    break;
                case SfxKind.Equip: // strapping it on: a short metal ring and a soft tick
                    Metal(buf, 0, 0.6, r * 2, 0.4);
                    Whoosh(buf, 0, 0.12, 0.15, 3000, 1500, rng);
                    break;
                case SfxKind.Relic: // a landmark: a deep chord and a bell
                    Boom(buf, 0, 0.6, 0.5, 60);
                    Pad(buf, v, 0.03, 1.1, r * 0.5, 0.3, rng);
                    Bell(buf, 0.08, 1.1, r * 2, 0.35);
                    Bell(buf, 0.22, 1.0, r * 2 * v.Fifth, 0.25);
                    break;
                case SfxKind.Curse: // a dark pad sinking, over a low rumble
                {
                    var dark = Darken(v);
                    Rumble(buf, 0, 1.0, 0.3, rng);
                    Pad(buf, dark, 0, 1.1, r, 0.35, rng, glideTo: 0.75);
                    Pad(buf, dark, 0.05, 1.05, r * dark.Fifth, 0.25, rng, glideTo: 0.75);
                    reverb = Math.Max(reverb, 0.35);
                    break;
                }
                case SfxKind.Ability: // a soft two-note pluck
                    Note(buf, v, 0, 0.5, r * 2, 0.4, rng);
                    Note(buf, v, 0.08, 0.5, r * 2 * v.Fifth, 0.35, rng);
                    break;
                case SfxKind.Power: // a champion moment: a rising arpeggio over a swell, crowned with a bell
                    Whoosh(buf, 0, 0.5, 0.25, 300, 3000, rng);
                    Pad(buf, v, 0, 1.2, r * 0.5, 0.28, rng);
                    Note(buf, v, 0.05, 0.9, r, 0.32, rng);
                    Note(buf, v, 0.13, 0.9, r * v.Third, 0.3, rng);
                    Note(buf, v, 0.21, 0.9, r * v.Fifth, 0.3, rng);
                    Bell(buf, 0.3, 0.9, r * 2 * v.Seventh, 0.25);
                    break;
                case SfxKind.Trigger: // a small glint
                    Note(buf, v, 0, 0.35, r * 3, 0.3, rng);
                    break;
                case SfxKind.Countered: // a swell that collapses: down, muffled
                {
                    var dark = Darken(neutral);
                    Pad(buf, dark, 0, 0.7, 330, 0.35, rng, glideTo: 0.5);
                    Boom(buf, 0.25, 0.4, 0.5, 55);
                    Whoosh(buf, 0.05, 0.4, 0.2, 2500, 300, rng);
                    reverb = 0.3;
                    break;
                }
                case SfxKind.Pass: // passing: two soft wooden taps, down a fourth
                {
                    var wood = VoiceOf("evergrowing_wild");
                    Note(buf, wood, 0, 0.35, 392, 0.4, rng);
                    Note(buf, wood, 0.11, 0.4, 294, 0.36, rng);
                    reverb = 0.18;
                    break;
                }
                case SfxKind.UiHover: // a faint high tick
                    Note(buf, neutral, 0, 0.12, 1568, 0.3, rng);
                    reverb = 0.05;
                    break;
                case SfxKind.UiClick: // a short wooden tap
                    Note(buf, VoiceOf("evergrowing_wild"), 0, 0.2, 523, 0.4, rng);
                    reverb = 0.08;
                    break;
                case SfxKind.RoundStart: // a new round: a deep boom, a swell and a bright open chord
                    Boom(buf, 0, 0.9, 0.7, 50);
                    Whoosh(buf, 0, 0.6, 0.25, 200, 2500, rng);
                    Pad(buf, neutral, 0.15, 1.2, 196, 0.25, rng);
                    Pad(buf, neutral, 0.18, 1.15, 294, 0.2, rng);
                    Bell(buf, 0.35, 1.0, 784, 0.18);
                    reverb = 0.35;
                    break;
                case SfxKind.Attack: // attack declare: a charge (whoosh) and a war drum
                    Whoosh(buf, 0, 0.35, 0.35, 400, 2800, rng);
                    Boom(buf, 0.22, 0.6, 0.8, 65);
                    Boom(buf, 0.36, 0.5, 0.55, 75);
                    reverb = 0.25;
                    break;
                case SfxKind.Block: // block declare: shields up, a dull thud with a muted ring
                    Boom(buf, 0, 0.45, 0.7, 85);
                    Metal(buf, 0.005, 0.5, 260, 0.25);
                    reverb = 0.22;
                    break;
                case SfxKind.Death: // death: the unit dissolves, a falling shimmer and a soft low note
                    Whoosh(buf, 0, 0.6, 0.25, 4000, 600, rng);
                    Pad(buf, Darken(v), 0.02, 0.8, r, 0.25, rng, glideTo: 0.6);
                    reverb = Math.Max(reverb, 0.3);
                    break;
            }

            Reverb(buf, reverb);
            LowPass(buf, 0.45); // round off the top (about 6 kHz)
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(buf[i] * 1.2) / 1.2f; // soft, never hard clipping
            FadeOut(buf, 0.06);
            Level(buf);
            return buf;
        }

        public static double Length(SfxKind kind)
        {
            switch (kind)
            {
                case SfxKind.UiHover: return 0.15;
                case SfxKind.UiClick: return 0.25;
                case SfxKind.Trigger: return 0.5;
                case SfxKind.Pass: return 0.7;
                case SfxKind.Ability: return 0.75;
                case SfxKind.Equip: return 0.8;
                case SfxKind.Block: return 0.8;
                case SfxKind.Instant: return 1.0;
                case SfxKind.Countered: return 1.0;
                case SfxKind.Attack: return 1.0;
                case SfxKind.Death: return 1.0;
                case SfxKind.Creature: return 1.1;
                case SfxKind.Equipment: return 1.1;
                case SfxKind.Curse: return 1.3;
                case SfxKind.Sorcery: return 1.3;
                case SfxKind.Relic: return 1.3;
                case SfxKind.Power: return 1.4;
                default: return 1.5; // RoundStart
            }
        }

        // ------------------------------------------------------------------ instruments

        private static Voice Darken(Voice v) => new Voice
        {
            Partials = v.Partials, Root = v.Root, Third = 1.189, Fifth = 1.414, Seventh = 1.682, Attack = Math.Max(v.Attack, 0.05),
            Decay = Math.Min(v.Decay, 3), Vibrato = 0.008, VibratoRate = 5, Detune = Math.Max(v.Detune, 0.006), Breath = v.Breath, Reverb = v.Reverb,
        };

        /// <summary>One note of the voice: its partials, each fading at its own rate (higher ones faster), with chorus and breath.</summary>
        private static void Note(float[] buf, Voice v, double start, double dur, double f, double amp, Random rng, double glideTo = 1.0, double attack = -1, double decay = -1)
        {
            int s0 = (int)(start * SampleRate), n = (int)(dur * SampleRate);
            double att = attack >= 0 ? attack : v.Attack, dec = decay >= 0 ? decay : v.Decay;
            int copies = v.Detune > 0 ? 3 : 1;
            var phases = new double[v.Partials.Length * copies];
            double breathLp = 0;
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                double t = (double)i / SampleRate, p = (double)i / n;
                double freq = f * Math.Pow(glideTo, p);
                if (v.Vibrato > 0) freq *= 1 + v.Vibrato * Math.Sin(2 * Math.PI * v.VibratoRate * t) * Math.Min(1, t * 4);
                double env = (t < att ? Smooth(t / att) : 1.0) * Math.Exp(-dec * Math.Max(0, t - att)) * (1 - p * p * p);
                double x = 0;
                for (int k = 0; k < v.Partials.Length; k++)
                {
                    var (ratio, a, d) = v.Partials[k];
                    double pe = Math.Exp(-dec * (d - 1) * t); // higher partials fade faster: the tone mellows
                    for (int c = 0; c < copies; c++)
                    {
                        double detune = copies == 1 ? 1 : 1 + (c - 1) * v.Detune;
                        int idx = k * copies + c;
                        phases[idx] += freq * ratio * detune / SampleRate;
                        x += a * pe * Math.Sin(2 * Math.PI * phases[idx]) / copies;
                    }
                }
                if (v.Breath > 0)
                {
                    breathLp += 0.08 * ((rng.NextDouble() * 2 - 1) - breathLp);
                    x += v.Breath * 6 * breathLp * Math.Exp(-8 * t);
                }
                buf[s0 + i] += (float)(x * amp * env);
            }
        }

        /// <summary>A sustained, slow-attack version of a note (chords, swells).</summary>
        private static void Pad(float[] buf, Voice v, double start, double dur, double f, double amp, Random rng, double glideTo = 1.0) =>
            Note(buf, v, start, dur, f, amp, rng, glideTo, attack: Math.Max(v.Attack, 0.12), decay: Math.Min(v.Decay, 1.8));

        /// <summary>A bell: harmonic partials with long, separate decays.</summary>
        private static void Bell(float[] buf, double start, double dur, double f, double amp)
        {
            Partial(buf, start, dur, f, amp, 3.5, 0.004);
            Partial(buf, start, dur, f * 2.0, amp * 0.4, 5, 0.004);
            Partial(buf, start, dur * 0.8, f * 3.0, amp * 0.18, 7, 0.004);
            Partial(buf, start, dur * 0.6, f * 4.2, amp * 0.08, 10, 0.004);
        }

        /// <summary>Metal, muffled: inharmonic partials with a soft (not clicky) attack.</summary>
        private static void Metal(float[] buf, double start, double dur, double f, double amp)
        {
            Partial(buf, start, dur, f, amp, 4, 0.003);
            Partial(buf, start, dur * 0.8, f * 2.76, amp * 0.35, 6, 0.003);
            Partial(buf, start, dur * 0.6, f * 5.4, amp * 0.12, 9, 0.003);
        }

        private static void Partial(float[] buf, double start, double dur, double f, double amp, double decay, double attack)
        {
            int s0 = (int)(start * SampleRate), n = (int)(dur * SampleRate);
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                double t = (double)i / SampleRate, p = (double)i / n;
                double env = (t < attack ? Smooth(t / attack) : 1.0) * Math.Exp(-decay * t) * (1 - p * p);
                buf[s0 + i] += (float)(amp * Math.Sin(2 * Math.PI * f * t) * env);
            }
        }

        /// <summary>A low boom: a sine that sinks a little in pitch, with a soft attack (no click).</summary>
        private static void Boom(float[] buf, double start, double dur, double amp, double f)
        {
            int s0 = (int)(start * SampleRate), n = (int)(dur * SampleRate);
            double phase = 0;
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                double t = (double)i / SampleRate, p = (double)i / n;
                phase += f * (0.7 + 0.3 * Math.Exp(-t * 12)) / SampleRate;
                double env = Smooth(Math.Min(1, t / 0.008)) * Math.Exp(-t * 6) * (1 - p * p);
                buf[s0 + i] += (float)(amp * Math.Sin(2 * Math.PI * phase) * env);
            }
        }

        /// <summary>Air: noise through a band-pass whose centre sweeps from f0 to f1 (Hz), swelling in and out.</summary>
        private static void Whoosh(float[] buf, double start, double dur, double amp, double f0, double f1, Random rng)
        {
            int s0 = (int)(start * SampleRate), n = (int)(dur * SampleRate);
            double low = 0, band = 0;
            const double q = 0.5;
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                double p = (double)i / n;
                double fc = f0 * Math.Pow(f1 / f0, p);
                double g = 2 * Math.Sin(Math.PI * Math.Min(fc, 8000) / SampleRate);
                double noise = rng.NextDouble() * 2 - 1;
                double high = noise - low - q * band; // state-variable filter
                band += g * high;
                low += g * band;
                double env = Math.Pow(Math.Sin(Math.PI * Math.Min(1, p * 1.15)), 1.5);
                buf[s0 + i] += (float)(amp * 1.5 * band * env);
            }
        }

        /// <summary>A low rumble: dark noise.</summary>
        private static void Rumble(float[] buf, double start, double dur, double amp, Random rng)
        {
            int s0 = (int)(start * SampleRate), n = (int)(dur * SampleRate);
            double lp1 = 0, lp2 = 0;
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                double p = (double)i / n;
                lp1 += 0.01 * ((rng.NextDouble() * 2 - 1) - lp1);
                lp2 += 0.01 * (lp1 - lp2);
                buf[s0 + i] += (float)(amp * 30 * lp2 * Math.Sin(Math.PI * p));
            }
        }

        // ------------------------------------------------------------------ the room

        /// <summary>A small hall (Schroeder: four damped combs into two all-passes), mixed in at <paramref name="wet"/>.</summary>
        private static void Reverb(float[] buf, double wet)
        {
            if (wet <= 0) return;
            int[] combs = { 1557, 1617, 1491, 1422 };
            int[] alls = { 225, 556 };
            var outp = new double[buf.Length];
            foreach (int d in combs)
            {
                var line = new double[d];
                double damp = 0;
                int idx = 0;
                for (int i = 0; i < buf.Length; i++)
                {
                    double y = line[idx];
                    damp = y * 0.6 + damp * 0.4;
                    line[idx] = buf[i] + damp * 0.78;
                    idx = (idx + 1) % d;
                    outp[i] += y / combs.Length;
                }
            }
            foreach (int d in alls)
            {
                var line = new double[d];
                int idx = 0;
                for (int i = 0; i < buf.Length; i++)
                {
                    double delayed = line[idx];
                    double x = outp[i];
                    line[idx] = x + delayed * 0.5;
                    outp[i] = delayed - x * 0.5;
                    idx = (idx + 1) % d;
                }
            }
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)(buf[i] * (1 - wet * 0.5) + outp[i] * wet * 2.2);
        }

        private static void LowPass(float[] buf, double a)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                double lp = 0;
                for (int i = 0; i < buf.Length; i++) { lp += a * (buf[i] - lp); buf[i] = (float)lp; }
            }
        }

        private static void FadeOut(float[] buf, double seconds)
        {
            int n = Math.Min(buf.Length, (int)(seconds * SampleRate));
            for (int i = 0; i < n; i++) buf[buf.Length - 1 - i] *= (float)Smooth((double)i / n);
        }

        private static double Smooth(double x) => x <= 0 ? 0 : x >= 1 ? 1 : x * x * (3 - 2 * x);

        /// <summary>Even loudness: about -18 dB RMS, never past a peak of 0.8.</summary>
        private static void Level(float[] buf)
        {
            double sum = 0;
            float max = 0f;
            foreach (var x in buf) { sum += x * x; max = Math.Max(max, Math.Abs(x)); }
            if (max < 1e-6f) return;
            double rms = Math.Sqrt(sum / buf.Length);
            float g = (float)Math.Min(0.125 / rms, 0.8 / max);
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
        }
    }
}
