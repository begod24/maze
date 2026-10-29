namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class LaneBuilder
    {
        public const int UP = 1;
        public const int DOWN = -1;

        private const int STAIR_LOWER_LEVEL = 0;
        private const int STAIR_UPPER_LEVEL = 1;
        private const int STAIR_LENGTH = 2;
        private const int SYMMETRIC_ENTRY = 0;
        private const int FORK_EAST_ENTRY = 0;
        private const int FORK_WEST_ENTRY = 1;
        private const int FORK_BRANCH_PASSAGE = 2;
        private const int DEAD_END_ENTRY = 0;
        private const float EVEN_CHANCE = 0.5f;

        private readonly FloorConfig config;
        private readonly ModulePicker picker;
        private readonly ModulePlacer placer;


        public LaneBuilder(FloorConfig config, ModulePicker picker, ModulePlacer placer)
        {
            this.config = config;
            this.picker = picker;
            this.placer = placer;
        }


        public bool TryWander(FloorLayout layout, FloorWalker walker, LaneBounds bounds, int steps, DeterministicRandom random)
        {
            for (int step = 0; step < steps; step++)
            {
                if (!TryWanderStep(layout, walker, bounds, random))
                {
                    return false;
                }

                walker.CountStep();
            }

            return TryFaceNorth(layout, walker);
        }


        public bool TryClimb(FloorLayout layout, FloorWalker walker, int direction)
        {
            WorldModuleDefinition stair = picker.GetSingle(WorldModuleRole.Stair);
            int entryLevel = direction == UP ? STAIR_LOWER_LEVEL : STAIR_UPPER_LEVEL;
            int entry = ModulePlacer.FindPassageAtLevel(stair, entryLevel);
            return placer.TryAdvance(layout, walker, stair, entry);
        }


        public bool TryPlaceWalkway(FloorLayout layout, FloorWalker walker, DeterministicRandom random)
        {
            WorldModuleDefinition definition = picker.PickWeighted(random, WorldModuleRole.Bridge, WorldModuleRole.Platform);
            return placer.TryAdvance(layout, walker, definition, SYMMETRIC_ENTRY);
        }


        public bool TryPlaceWalkways(FloorLayout layout, FloorWalker walker, int count, DeterministicRandom random)
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


        public bool TryTurn(FloorLayout layout, FloorWalker walker, GridDirection exitDirection)
        {
            return placer.TryAdvanceToward(layout, walker, picker.GetSingle(WorldModuleRole.Turn), exitDirection);
        }


        public bool TryFaceNorth(FloorLayout layout, FloorWalker walker)
        {
            if (walker.Heading == GridDirection.North)
            {
                return true;
            }

            return TryTurn(layout, walker, GridDirection.North);
        }


        public bool TryReachLevel(FloorLayout layout, FloorWalker walker, int target)
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


        public bool TryReachColumn(FloorLayout layout, FloorWalker walker, int targetX, DeterministicRandom random)
        {
            if (walker.Cell.x == targetX)
            {
                return true;
            }

            GridDirection side = walker.Cell.x < targetX ? GridDirection.East : GridDirection.West;
            if (!TryTurn(layout, walker, side))
            {
                return false;
            }

            while (walker.Cell.x != targetX)
            {
                if (!TryPlaceWalkway(layout, walker, random))
                {
                    return false;
                }
            }

            return TryTurn(layout, walker, GridDirection.North);
        }


        public bool TryPadToRow(FloorLayout layout, FloorWalker walker, int row, DeterministicRandom random)
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


        private bool TryWanderStep(FloorLayout layout, FloorWalker walker, LaneBounds bounds, DeterministicRandom random)
        {
            if (walker.Heading != GridDirection.North)
            {
                if (walker.SideStepsLeft <= 0 || !bounds.Contains(walker.CellAhead(1)))
                {
                    return TryTurnNorth(layout, walker);
                }

                return TryForwardStep(layout, walker, bounds, random);
            }

            bool canTurn = walker.StepsSinceTurn >= config.minStepsBetweenTurns;
            if (canTurn && random.Chance(config.turnChance) && TryPickSide(walker, bounds, config.sideStepsMin + 1, random, out GridDirection turnSide))
            {
                return TryTurnSide(layout, walker, turnSide, random);
            }

            if (random.Chance(config.forkChance) && TryPickSide(walker, bounds, config.deadEndStepsMin + 1, random, out GridDirection branchSide))
            {
                return TryFork(layout, walker, bounds, branchSide, random);
            }

            return TryForwardStep(layout, walker, bounds, random);
        }


        private bool TryForwardStep(FloorLayout layout, FloorWalker walker, LaneBounds bounds, DeterministicRandom random)
        {
            if (bounds.Contains(walker.CellAhead(STAIR_LENGTH)) && random.Chance(config.stairChance))
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


        private bool TryTurnSide(FloorLayout layout, FloorWalker walker, GridDirection side, DeterministicRandom random)
        {
            if (!TryTurn(layout, walker, side))
            {
                return false;
            }

            walker.MarkTurn(random.Range(config.sideStepsMin, config.sideStepsMax + 1));
            return true;
        }


        private bool TryTurnNorth(FloorLayout layout, FloorWalker walker)
        {
            if (!TryTurn(layout, walker, GridDirection.North))
            {
                return false;
            }

            walker.MarkTurn(0);
            return true;
        }


        private bool TryFork(FloorLayout layout, FloorWalker walker, LaneBounds bounds, GridDirection side, DeterministicRandom random)
        {
            WorldModuleDefinition fork = picker.GetSingle(WorldModuleRole.Fork);
            int entry = side == GridDirection.East ? FORK_EAST_ENTRY : FORK_WEST_ENTRY;
            int forward = entry == FORK_EAST_ENTRY ? FORK_WEST_ENTRY : FORK_EAST_ENTRY;

            if (!placer.TryPlace(layout, walker, fork, entry, out PlacedModule placed, out _))
            {
                return false;
            }

            walker.MoveTo(placed.GetPassageNeighbor(forward), placed.GetPassageDirection(forward));
            BuildDeadEnd(layout, ModulePlacer.WalkerFrom(placed, FORK_BRANCH_PASSAGE), bounds, random);
            return true;
        }


        private void BuildDeadEnd(FloorLayout layout, FloorWalker branch, LaneBounds bounds, DeterministicRandom random)
        {
            int steps = random.Range(config.deadEndStepsMin, config.deadEndStepsMax + 1);

            for (int step = 0; step < steps; step++)
            {
                if (!bounds.Contains(branch.CellAhead(1)) || !TryForwardBranchStep(layout, branch, bounds, random))
                {
                    break;
                }
            }

            if (bounds.Contains(branch.CellAhead(1)) && random.Chance(config.deadEndDoorwayChance))
            {
                placer.TryAdvance(layout, branch, picker.GetSingle(WorldModuleRole.Doorway), SYMMETRIC_ENTRY);
            }

            if (bounds.Contains(branch.Cell))
            {
                placer.TryPlace(layout, branch, picker.GetSingle(WorldModuleRole.DeadEnd), DEAD_END_ENTRY, out _, out _);
            }
        }


        private bool TryForwardBranchStep(FloorLayout layout, FloorWalker branch, LaneBounds bounds, DeterministicRandom random)
        {
            if (bounds.Contains(branch.CellAhead(STAIR_LENGTH)) && random.Chance(config.stairChance))
            {
                return TryClimb(layout, branch, PickStairDirection(branch.Level, random));
            }

            return TryPlaceWalkway(layout, branch, random);
        }


        private static bool TryPickSide(FloorWalker walker, LaneBounds bounds, int room, DeterministicRandom random, out GridDirection side)
        {
            bool east = bounds.HasRoom(walker.Cell.x, GridDirection.East, room);
            bool west = bounds.HasRoom(walker.Cell.x, GridDirection.West, room);

            if (east && west)
            {
                if (random.Chance(EVEN_CHANCE))
                {
                    side = GridDirection.East;
                }
                else
                {
                    side = GridDirection.West;
                }

                return true;
            }

            side = east ? GridDirection.East : GridDirection.West;
            return east || west;
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
    }
}
