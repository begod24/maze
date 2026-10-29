using System;
using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class LinearFloorTemplate
    {
        private const int EAST_SIDE = 1;
        private const int CENTER = 0;
        private const int WEST_SIDE = -1;
        private const int SYMMETRIC_ENTRY = 0;

        private readonly FloorConfig config;
        private readonly ModulePicker picker;
        private readonly ModulePlacer placer;
        private readonly LaneBuilder lanes;


        public LinearFloorTemplate(FloorConfig config, ModulePicker picker, ModulePlacer placer, LaneBuilder lanes)
        {
            this.config = config;
            this.picker = picker;
            this.placer = placer;
            this.lanes = lanes;
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
            LaneBounds eastHalf = new LaneBounds(EAST_SIDE, config.laneHalfWidth);
            LaneBounds westHalf = new LaneBounds(-config.laneHalfWidth, WEST_SIDE);

            if (!lanes.TryClimb(layout, upper, LaneBuilder.UP) || !lanes.TryClimb(layout, lower, LaneBuilder.DOWN))
            {
                return null;
            }

            int upperSteps = random.Range(config.upperLaneStepsMin, config.upperLaneStepsMax + 1);
            if (!lanes.TryWander(layout, upper, eastHalf, upperSteps, random))
            {
                return null;
            }

            if (!TryBuildLowerPath(layout, upper, lower, westHalf, variant, random))
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


        private bool TryBuildLowerPath(FloorLayout layout, FloorWalker upper, FloorWalker lower, LaneBounds westHalf, FloorStartVariant variant, DeterministicRandom random)
        {
            if (variant == FloorStartVariant.Loop)
            {
                return TryBuildLoop(layout, lower, random) && lanes.TryReachColumn(layout, upper, EAST_SIDE, random);
            }

            int lowerSteps = random.Range(config.lowerLaneStepsMin, config.lowerLaneStepsMax + 1);
            if (!lanes.TryWander(layout, lower, westHalf, lowerSteps, random))
            {
                return false;
            }

            return TryMergeLanes(layout, upper, lower, random);
        }


        private bool TryMergeLanes(FloorLayout layout, FloorWalker upper, FloorWalker lower, DeterministicRandom random)
        {
            int lowest = Math.Min(upper.Level, lower.Level);
            int highest = Math.Max(upper.Level, lower.Level);
            int target = random.Range(lowest, highest + 1);

            if (!lanes.TryReachLevel(layout, upper, target) || !lanes.TryReachLevel(layout, lower, target))
            {
                return false;
            }

            if (!lanes.TryReachColumn(layout, upper, EAST_SIDE, random) || !lanes.TryReachColumn(layout, lower, WEST_SIDE, random))
            {
                return false;
            }

            int row = Math.Max(upper.Cell.z, lower.Cell.z);
            return lanes.TryPadToRow(layout, upper, row, random) && lanes.TryPadToRow(layout, lower, row, random);
        }


        private bool TryBuildLoop(FloorLayout layout, FloorWalker walker, DeterministicRandom random)
        {
            int rows = random.Range(config.loopRowsMin, config.loopRowsMax + 1);

            if (!lanes.TryPlaceWalkways(layout, walker, rows, random))
            {
                return false;
            }

            WorldModuleDefinition uTurn = picker.GetSingle(WorldModuleRole.UTurn);
            if (!placer.TryAdvance(layout, walker, uTurn, ModulePlacer.FindPassage(uTurn, GridDirection.South, CENTER)))
            {
                return false;
            }

            if (!lanes.TryPlaceWalkways(layout, walker, rows, random) || !lanes.TryClimb(layout, walker, LaneBuilder.UP))
            {
                return false;
            }

            return lanes.TryTurn(layout, walker, GridDirection.East);
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
