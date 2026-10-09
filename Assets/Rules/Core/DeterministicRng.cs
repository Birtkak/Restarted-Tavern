using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// The only source of randomness in the engine (DEVELOPMENT §1.2). SplitMix64: one
    /// 64-bit state value, so it is trivial to save, clone and replay.
    /// </summary>
    public sealed class DeterministicRng
    {
        public ulong State { get; private set; }

        public DeterministicRng(ulong seed) { State = seed; }

        public ulong NextULong()
        {
            ulong z = State += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Uniform int in [0, maxExclusive).</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            // Rejection sampling keeps the result unbiased.
            ulong bound = (ulong)maxExclusive;
            ulong limit = ulong.MaxValue - ulong.MaxValue % bound;
            ulong r;
            do { r = NextULong(); } while (r >= limit);
            return (int)(r % bound);
        }

        /// <summary>Fisher–Yates shuffle.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        public DeterministicRng Clone() => new DeterministicRng(State);
    }
}
