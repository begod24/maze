using NoWayOut.Gameplay.World.Generation;
using NUnit.Framework;

namespace NoWayOut.Tests.EditMode.World
{
    public sealed class DeterministicRandomTests
    {
        private const int SEED_COUNT = 200;
        private const int MIN_EXPECTED_BELOW_HALF = 60;
        private const int MAX_EXPECTED_BELOW_HALF = 140;


        [Test]
        public void NextFloat_SmallSeeds_FirstValuesSpreadAcrossRange()
        {
            int belowHalf = 0;
            for (int seed = 1; seed <= SEED_COUNT; seed++)
            {
                if (new DeterministicRandom(seed).NextFloat() < 0.5f)
                {
                    belowHalf++;
                }
            }

            Assert.That(belowHalf, Is.InRange(MIN_EXPECTED_BELOW_HALF, MAX_EXPECTED_BELOW_HALF));
        }


        [Test]
        public void NextUInt_SameSeed_ReturnsSameSequence()
        {
            DeterministicRandom first = new DeterministicRandom(42);
            DeterministicRandom second = new DeterministicRandom(42);

            for (int i = 0; i < SEED_COUNT; i++)
            {
                Assert.AreEqual(first.NextUInt(), second.NextUInt());
            }
        }


        [Test]
        public void Range_AnySeed_StaysWithinBounds()
        {
            DeterministicRandom random = new DeterministicRandom(7);

            for (int i = 0; i < SEED_COUNT; i++)
            {
                Assert.That(random.Range(3, 9), Is.InRange(3, 8));
            }
        }
    }
}
