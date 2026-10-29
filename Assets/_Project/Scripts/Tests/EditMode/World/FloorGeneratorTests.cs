using NoWayOut.Gameplay.World;
using NoWayOut.Gameplay.World.Generation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NoWayOut.Tests.EditMode.World
{
    public sealed class FloorGeneratorTests
    {
        private const string CATALOG_PATH = "Assets/_Project/Data/World/WorldModuleCatalog.asset";
        private const string CONFIG_PATH = "Assets/_Project/Data/Configs/FloorConfig.asset";
        private const int FIRST_SEED = 1;
        private const int SEED_COUNT = 200;
        private const int FIXED_SEED = 424242;

        private FloorConfig config;
        private FloorGenerator generator;


        [SetUp]
        public void SetUp()
        {
            WorldModuleCatalog catalog = AssetDatabase.LoadAssetAtPath<WorldModuleCatalog>(CATALOG_PATH);
            config = AssetDatabase.LoadAssetAtPath<FloorConfig>(CONFIG_PATH);
            generator = new FloorGenerator(config, catalog);
        }


        [Test]
        public void Generate_SameSeed_ReturnsSameLayout()
        {
            FloorLayout first = generator.Generate(FIXED_SEED);
            FloorLayout second = generator.Generate(FIXED_SEED);

            Assert.AreEqual(first.StartVariant, second.StartVariant);
            Assert.AreEqual(first.Modules.Count, second.Modules.Count);
            for (int i = 0; i < first.Modules.Count; i++)
            {
                Assert.AreSame(first.Modules[i].Definition, second.Modules[i].Definition);
                Assert.AreEqual(first.Modules[i].Anchor, second.Modules[i].Anchor);
                Assert.AreEqual(first.Modules[i].Rotation, second.Modules[i].Rotation);
            }
        }


        [Test]
        public void Generate_ManySeeds_ConnectsStartToExit()
        {
            for (int seed = FIRST_SEED; seed < FIRST_SEED + SEED_COUNT; seed++)
            {
                FloorLayout layout = generator.Generate(seed);
                Assert.IsTrue(FloorLayoutValidator.AreConnected(layout, layout.StartIndex, layout.ExitIndex), "seed " + seed);
            }
        }


        [Test]
        public void Generate_ManySeeds_HasMatchingJoints()
        {
            for (int seed = FIRST_SEED; seed < FIRST_SEED + SEED_COUNT; seed++)
            {
                Assert.IsTrue(FloorLayoutValidator.HasMatchingJoints(generator.Generate(seed)), "seed " + seed);
            }
        }


        [Test]
        public void Generate_ManySeeds_HasNoOverlappingModules()
        {
            for (int seed = FIRST_SEED; seed < FIRST_SEED + SEED_COUNT; seed++)
            {
                FloorLayout layout = generator.Generate(seed);
                int cells = 0;
                foreach (PlacedModule module in layout.Modules)
                {
                    cells += module.CellCount;
                }

                Assert.AreEqual(cells, layout.CellCount, "seed " + seed);
            }
        }


        [Test]
        public void Generate_ManySeeds_StaysWithinLevels()
        {
            for (int seed = FIRST_SEED; seed < FIRST_SEED + SEED_COUNT; seed++)
            {
                foreach (PlacedModule module in generator.Generate(seed).Modules)
                {
                    for (int i = 0; i < module.CellCount; i++)
                    {
                        int level = module.GetCell(i).y;
                        Assert.That(level, Is.InRange(config.minLevel, config.maxLevel), "seed " + seed);
                    }
                }
            }
        }


        [Test]
        public void Generate_ManySeeds_ProducesBothStartVariants()
        {
            bool hasTwoPaths = false;
            bool hasLoop = false;

            for (int seed = FIRST_SEED; seed < FIRST_SEED + SEED_COUNT; seed++)
            {
                FloorStartVariant variant = generator.Generate(seed).StartVariant;
                hasTwoPaths |= variant == FloorStartVariant.TwoPaths;
                hasLoop |= variant == FloorStartVariant.Loop;
            }

            Assert.IsTrue(hasTwoPaths, "no TwoPaths floor");
            Assert.IsTrue(hasLoop, "no Loop floor");
        }


        [Test]
        public void Generate_LoopVariant_ReturnsToStart()
        {
            for (int seed = FIRST_SEED; seed < FIRST_SEED + SEED_COUNT; seed++)
            {
                FloorLayout layout = generator.Generate(seed);
                if (layout.StartVariant == FloorStartVariant.Loop)
                {
                    Assert.IsTrue(FloorLayoutValidator.IsLoopClosed(layout), "seed " + seed);
                }
            }
        }


        [Test]
        public void Generate_FixedSeed_PlacesExitBeyondMinimumRows()
        {
            FloorLayout layout = generator.Generate(FIXED_SEED);
            Vector3Int exitAnchor = layout.Modules[layout.ExitIndex].Anchor;
            int minimumRows = config.bridgeRowsMin + config.separationRowsMin;

            Assert.GreaterOrEqual(exitAnchor.z, minimumRows);
        }
    }
}
