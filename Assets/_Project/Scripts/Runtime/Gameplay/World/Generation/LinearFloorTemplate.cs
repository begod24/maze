using System;
using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class LinearFloorTemplate
    {
        private const int UP = 1;
        private const int DOWN = -1;
        private const int EAST_SIDE = 1;
        private const int CENTER = 0;
        private const int WEST_SIDE = -1;
        private const int STAIR_ROWS = 2;
        private const int STAIR_LOWER_LEVEL = 0;
        private const int STAIR_UPPER_LEVEL = 1;
        private const int SYMMETRIC_ENTRY = 0;
        private const float EVEN_CHANCE = 0.5f;

        private readonly FloorConfig config;
        private readonly ModulePicker picker;
        private readonly ModulePlacer placer;


        public LinearFloorTemplate(FloorConfig config, ModulePicker picker, ModulePlacer placer)
        {
            this.config = config;
            this.picker = picker;
            this.placer = placer;
        }


        public FloorLayout TryBuild(int seed, DeterministicRandom random)
        {
            FloorStartVariant variant = PickVariant(random);
            FloorLayout layout = new FloorLayout(seed, variant);

            WorldModuleDefinition startDefinition = picker.GetSingle(WorldModuleRole.Start);
            PlacedModule start = new PlacedModule(startDefinition, Vector3Int.zero, 0);
            layout.MarkStart(layout.Add(start));

            FloorWalker upper = ModulePlacer.WalkerFrom(start, ModulePlacer.FindPassage(startDefinition, GridDirection.North, EAST_SIDE));
            FloorWalker lower = ModulePlacer.WalkerFrom(start, ModulePlacer.FindPassage(startDefinition, GridDirection.North, WEST_SIDE));

            if (!TryClimb(layout, upper, UP) || !TryClimb(layout, lower, DOWN))
            {
                return null;
            }

            int bridgeRows = random.Range(config.bridgeRowsMin, config.bridgeRowsMax + 1);
            if (!TryFillBridges(layout, upper, bridgeRows, random))
            {
                return null;
            }

            if (!TryBuildLowerPath(layout, upper, lower, variant, bridgeRows, random))
            {
                return null;
            }

            if (!TryBuildSeparationAndExit(layout, upper, lower, variant, random))
            {
                return null;
            }

            return layout;
        }


        private FloorStartVariant PickVariant(DeterministicRandom random)
        {
            if (random.Chance(config.loopStartChance))
            {
                return FloorStartVariant.Loop;
            }

            return FloorStartVariant.TwoPaths;
        }


        private bool TryBuildLowerPath(FloorLayout layout, FloorWalker upper, FloorWalker lower, FloorStartVariant variant, int bridgeRows, DeterministicRandom random)
        {
            if (variant == FloorStartVariant.Loop)
            {
                return TryBuildLoop(layout, lower, random);
            }

            if (!TryFillBridges(layout, lower, bridgeRows, random))
            {
                return false;
            }

            return TryAlignLanes(layout, upper, lower, random);
        }


        private bool TryClimb(FloorLayout layout, FloorWalker walker, int direction)
        {
            WorldModuleDefinition stair = picker.GetSingle(WorldModuleRole.Stair);
            int entryLevel = direction == UP ? STAIR_LOWER_LEVEL : STAIR_UPPER_LEVEL;
            int entry = ModulePlacer.FindPassageAtLevel(stair, entryLevel);
            return placer.TryAdvance(layout, walker, stair, entry);
        }


        private bool TryFillBridges(FloorLayout layout, FloorWalker walker, int rows, DeterministicRandom random)
        {
            Vector3Int origin = walker.Cell;

            while (walker.DistanceAlongHeadingFrom(origin) < rows)
            {
                int remaining = rows - walker.DistanceAlongHeadingFrom(origin);
                if (!TryPlaceBridgeStep(layout, walker, remaining, random))
                {
                    return false;
                }
            }

            return true;
        }


        private bool TryPlaceBridgeStep(FloorLayout layout, FloorWalker walker, int remainingRows, DeterministicRandom random)
        {
            if (remainingRows >= STAIR_ROWS && random.Chance(config.stairChance))
            {
                return TryClimb(layout, walker, PickStairDirection(walker.Level, random));
            }

            if (walker.DoorwayCount < config.maxDoorwaysPerLane && random.Chance(config.doorwayChance))
            {
                walker.CountDoorway();
                return placer.TryAdvance(layout, walker, picker.GetSingle(WorldModuleRole.Doorway), SYMMETRIC_ENTRY);
            }

            return TryPlaceWalkway(layout, walker, random);
        }


        private bool TryPlaceWalkway(FloorLayout layout, FloorWalker walker, DeterministicRandom random)
        {
            WorldModuleDefinition definition = picker.PickWeighted(random, WorldModuleRole.Bridge, WorldModuleRole.Platform);
            return placer.TryAdvance(layout, walker, definition, SYMMETRIC_ENTRY);
        }


        private int PickStairDirection(int level, DeterministicRandom random)
        {
            bool canClimb = level + 1 <= config.maxLevel;
            bool canDescend = level - 1 >= config.minLevel;

            if (canClimb && canDescend && random.Chance(EVEN_CHANCE))
            {
                return DOWN;
            }

            return canClimb ? UP : DOWN;
        }


        private bool TryAlignLanes(FloorLayout layout, FloorWalker upper, FloorWalker lower, DeterministicRandom random)
        {
            int lowest = Math.Min(upper.Level, lower.Level);
            int highest = Math.Max(upper.Level, lower.Level);
            int target = random.Range(lowest, highest + 1);

            if (!TryReachLevel(layout, upper, target) || !TryReachLevel(layout, lower, target))
            {
                return false;
            }

            int row = Math.Max(upper.Cell.z, lower.Cell.z);
            return TryPadToRow(layout, upper, row, random) && TryPadToRow(layout, lower, row, random);
        }


        private bool TryReachLevel(FloorLayout layout, FloorWalker walker, int target)
        {
            while (walker.Level != target)
            {
                int direction = walker.Level < target ? UP : DOWN;
                if (!TryClimb(layout, walker, direction))
                {
                    return false;
                }
            }

            return true;
        }


        private bool TryPadToRow(FloorLayout layout, FloorWalker walker, int row, DeterministicRandom random)
        {
            while (walker.Cell.z < row)
            {
                if (!TryPlaceWalkway(layout, walker, random))
                {
                    return false;
                }
            }

            return true;
        }


        private bool TryBuildLoop(FloorLayout layout, FloorWalker walker, DeterministicRandom random)
        {
            int rows = random.Range(config.loopRowsMin, config.loopRowsMax + 1);

            if (!TryPlaceWalkways(layout, walker, rows, random))
            {
                return false;
            }

            WorldModuleDefinition uTurn = picker.GetSingle(WorldModuleRole.UTurn);
            if (!placer.TryAdvance(layout, walker, uTurn, ModulePlacer.FindPassage(uTurn, GridDirection.South, CENTER)))
            {
                return false;
            }

            if (!TryPlaceWalkways(layout, walker, rows, random) || !TryClimb(layout, walker, UP))
            {
                return false;
            }

            return placer.TryAdvanceToward(layout, walker, picker.GetSingle(WorldModuleRole.Turn), GridDirection.East);
        }


        private bool TryPlaceWalkways(FloorLayout layout, FloorWalker walker, int count, DeterministicRandom random)
        {
            for (int i = 0; i < count; i++)
            {
                if (!TryPlaceWalkway(layout, walker, random))
                {
                    return false;
                }
            }

            return true;
        }


        private bool TryBuildSeparationAndExit(FloorLayout layout, FloorWalker upper, FloorWalker lower, FloorStartVariant variant, DeterministicRandom random)
        {
            WorldModuleDefinition junctionDefinition = picker.GetSingle(WorldModuleRole.Junction);
            int junctionEast = ModulePlacer.FindPassage(junctionDefinition, GridDirection.South, EAST_SIDE);
            int junctionWest = ModulePlacer.FindPassage(junctionDefinition, GridDirection.South, WEST_SIDE);

            if (!placer.TryPlace(layout, upper, junctionDefinition, junctionEast, out PlacedModule junction, out _))
            {
                return false;
            }

            if (variant == FloorStartVariant.TwoPaths && !ModulePlacer.Reaches(lower, junction, junctionWest))
            {
                return false;
            }

            FloorWalker left = ModulePlacer.WalkerFrom(junction, ModulePlacer.FindPassage(junctionDefinition, GridDirection.North, WEST_SIDE));
            FloorWalker right = ModulePlacer.WalkerFrom(junction, ModulePlacer.FindPassage(junctionDefinition, GridDirection.North, EAST_SIDE));
            WorldModuleDefinition corridor = picker.GetSingle(WorldModuleRole.Corridor);
            int rows = random.Range(config.separationRowsMin, config.separationRowsMax + 1);

            for (int i = 0; i < rows; i++)
            {
                if (!placer.TryAdvance(layout, left, corridor, SYMMETRIC_ENTRY) || !placer.TryAdvance(layout, right, corridor, SYMMETRIC_ENTRY))
                {
                    return false;
                }
            }

            return TryPlaceExit(layout, left, right);
        }


        private bool TryPlaceExit(FloorLayout layout, FloorWalker left, FloorWalker right)
        {
            WorldModuleDefinition exitDefinition = picker.GetSingle(WorldModuleRole.Exit);
            int exitEast = ModulePlacer.FindPassage(exitDefinition, GridDirection.South, EAST_SIDE);
            int exitWest = ModulePlacer.FindPassage(exitDefinition, GridDirection.South, WEST_SIDE);

            if (!placer.TryPlace(layout, right, exitDefinition, exitEast, out PlacedModule exit, out int exitIndex))
            {
                return false;
            }

            if (!ModulePlacer.Reaches(left, exit, exitWest))
            {
                return false;
            }

            layout.MarkExit(exitIndex);
            return true;
        }
    }
}
