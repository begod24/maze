using System;
using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class ModulePlacer
    {
        private const int NOT_FOUND = -1;

        private readonly FloorConfig config;


        public ModulePlacer(FloorConfig config)
        {
            this.config = config;
        }


        public bool TryPlace(FloorLayout layout, FloorWalker walker, WorldModuleDefinition definition, int entryPassage, out PlacedModule placed, out int index)
        {
            ModulePassage entry = definition.passages[entryPassage];
            GridDirection required = GridDirections.Opposite(walker.Heading);
            int rotation = GridDirections.TurnsBetween(entry.direction, required);
            Vector3Int anchor = walker.Cell - GridDirections.RotateOffset(entry.cell, rotation);

            placed = new PlacedModule(definition, anchor, rotation);
            index = NOT_FOUND;

            if (!IsWithinLevels(placed) || !layout.IsFree(placed))
            {
                return false;
            }

            index = layout.Add(placed);
            return true;
        }


        public bool TryAdvance(FloorLayout layout, FloorWalker walker, WorldModuleDefinition definition, int entryPassage)
        {
            if (!TryPlace(layout, walker, definition, entryPassage, out PlacedModule placed, out _))
            {
                return false;
            }

            int exitPassage = OtherPassage(entryPassage);
            walker.MoveTo(placed.GetPassageNeighbor(exitPassage), placed.GetPassageDirection(exitPassage));
            return true;
        }


        public bool TryAdvanceToward(FloorLayout layout, FloorWalker walker, WorldModuleDefinition definition, GridDirection exitDirection)
        {
            GridDirection required = GridDirections.Opposite(walker.Heading);

            for (int entry = 0; entry < definition.passages.Count; entry++)
            {
                int rotation = GridDirections.TurnsBetween(definition.passages[entry].direction, required);
                GridDirection exit = GridDirections.Rotate(definition.passages[OtherPassage(entry)].direction, rotation);
                if (exit == exitDirection)
                {
                    return TryAdvance(layout, walker, definition, entry);
                }
            }

            return false;
        }


        public static FloorWalker WalkerFrom(PlacedModule module, int passage)
        {
            return new FloorWalker(module.GetPassageNeighbor(passage), module.GetPassageDirection(passage));
        }


        public static bool Reaches(FloorWalker walker, PlacedModule module, int passage)
        {
            bool sameCell = walker.Cell == module.GetPassageCell(passage);
            bool facing = walker.Heading == GridDirections.Opposite(module.GetPassageDirection(passage));
            return sameCell && facing;
        }


        public static int FindPassage(WorldModuleDefinition definition, GridDirection direction, int sideSign)
        {
            for (int i = 0; i < definition.passages.Count; i++)
            {
                ModulePassage passage = definition.passages[i];
                if (passage.direction == direction && Math.Sign(passage.cell.x) == sideSign)
                {
                    return i;
                }
            }

            throw new InvalidOperationException(definition.name + " has no " + direction + " passage on side " + sideSign);
        }


        public static int FindPassageAtLevel(WorldModuleDefinition definition, int level)
        {
            for (int i = 0; i < definition.passages.Count; i++)
            {
                if (definition.passages[i].cell.y == level)
                {
                    return i;
                }
            }

            throw new InvalidOperationException(definition.name + " has no passage at level " + level);
        }


        private bool IsWithinLevels(PlacedModule placed)
        {
            for (int i = 0; i < placed.CellCount; i++)
            {
                int level = placed.GetCell(i).y;
                if (level < config.minLevel || level > config.maxLevel)
                {
                    return false;
                }
            }

            return true;
        }


        private static int OtherPassage(int passage)
        {
            if (passage == 0)
            {
                return 1;
            }

            return 0;
        }
    }
}
