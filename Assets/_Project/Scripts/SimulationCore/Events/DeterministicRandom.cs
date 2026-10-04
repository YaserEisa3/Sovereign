namespace Sovereign.Core
{
    /// <summary>
    /// Xorshift, with its whole state in one field so it saves with the run. Events
    /// are the first randomness in the simulation, and a seeded generator keeps runs
    /// replayable - the same seed and the same decisions give the same history, which
    /// is what makes an event bug reproducible instead of a ghost story.
    /// </summary>
    public class DeterministicRandom
    {
        public uint state;

        public DeterministicRandom(uint seed) { state = seed == 0u ? 2463534242u : seed; }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>0 inclusive to 1 exclusive.</summary>
        public float Value() { return (NextUInt() >> 8) * (1f / 16777216f); }

        public float Range(float min, float max) { return min + (max - min) * Value(); }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }
    }
}
