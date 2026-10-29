namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class DeterministicRandom
    {
        private const uint ZERO_SEED_REPLACEMENT = 0x9E3779B9;
        private const uint MIX_MULTIPLIER_A = 0x7FEB352D;
        private const uint MIX_MULTIPLIER_B = 0x846CA68B;
        private const int MIX_SHIFT_A = 16;
        private const int MIX_SHIFT_B = 15;
        private const int SHIFT_A = 13;
        private const int SHIFT_B = 17;
        private const int SHIFT_C = 5;
        private const int FLOAT_DROP_BITS = 8;
        private const float FLOAT_SCALE = 1f / 16777216f;

        private uint state;


        public DeterministicRandom(int seed)
        {
            state = Mix(unchecked((uint)seed));
            if (state == 0)
            {
                state = ZERO_SEED_REPLACEMENT;
            }
        }


        public uint NextUInt()
        {
            state ^= state << SHIFT_A;
            state ^= state >> SHIFT_B;
            state ^= state << SHIFT_C;
            return state;
        }


        public int Range(int minInclusive, int maxExclusive)
        {
            uint span = (uint)(maxExclusive - minInclusive);
            if (span == 0)
            {
                return minInclusive;
            }

            return minInclusive + (int)(NextUInt() % span);
        }


        public float NextFloat()
        {
            return (NextUInt() >> FLOAT_DROP_BITS) * FLOAT_SCALE;
        }


        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }


        private static uint Mix(uint value)
        {
            unchecked
            {
                value ^= value >> MIX_SHIFT_A;
                value *= MIX_MULTIPLIER_A;
                value ^= value >> MIX_SHIFT_B;
                value *= MIX_MULTIPLIER_B;
                value ^= value >> MIX_SHIFT_A;
                return value;
            }
        }
    }
}
