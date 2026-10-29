using System.Collections.Generic;
using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal static class FloorLayoutValidator
    {
        private const int NONE = -1;


        public static bool IsValid(FloorLayout layout)
        {
            if (layout.StartIndex == NONE || layout.ExitIndex == NONE)
            {
                return false;
            }

            if (!HasMatchingJoints(layout) || !AreConnected(layout, layout.StartIndex, layout.ExitIndex))
            {
                return false;
            }

            if (layout.StartVariant == FloorStartVariant.Loop)
            {
                return IsLoopClosed(layout);
            }

            return true;
        }


        public static bool HasMatchingJoints(FloorLayout layout)
        {
            for (int moduleIndex = 0; moduleIndex < layout.Modules.Count; moduleIndex++)
            {
                PlacedModule module = layout.Modules[moduleIndex];
                for (int passage = 0; passage < module.PassageCount; passage++)
                {
                    if (!layout.TryGetModuleAt(module.GetPassageNeighbor(passage), out int neighborIndex))
                    {
                        continue;
                    }

                    if (neighborIndex == moduleIndex || FindMatchingPassage(layout, moduleIndex, passage, neighborIndex) == NONE)
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        public static bool AreConnected(FloorLayout layout, int from, int to)
        {
            HashSet<int> visited = new HashSet<int> { from };
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == to)
                {
                    return true;
                }

                PlacedModule module = layout.Modules[current];
                for (int passage = 0; passage < module.PassageCount; passage++)
                {
                    int neighbor = GetConnectedModule(layout, current, passage);
                    if (neighbor != NONE && visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return false;
        }


        public static bool IsLoopClosed(FloorLayout layout)
        {
            PlacedModule start = layout.Modules[layout.StartIndex];
            for (int passage = 0; passage < start.PassageCount; passage++)
            {
                if (start.GetPassageDirection(passage) == GridDirection.West)
                {
                    return GetConnectedModule(layout, layout.StartIndex, passage) != NONE;
                }
            }

            return false;
        }


        public static int GetConnectedModule(FloorLayout layout, int moduleIndex, int passage)
        {
            PlacedModule module = layout.Modules[moduleIndex];
            if (!layout.TryGetModuleAt(module.GetPassageNeighbor(passage), out int neighborIndex))
            {
                return NONE;
            }

            if (FindMatchingPassage(layout, moduleIndex, passage, neighborIndex) == NONE)
            {
                return NONE;
            }

            return neighborIndex;
        }


        private static int FindMatchingPassage(FloorLayout layout, int moduleIndex, int passage, int neighborIndex)
        {
            PlacedModule module = layout.Modules[moduleIndex];
            PlacedModule neighbor = layout.Modules[neighborIndex];
            Vector3Int neighborCell = module.GetPassageNeighbor(passage);
            GridDirection facing = GridDirections.Opposite(module.GetPassageDirection(passage));

            for (int candidate = 0; candidate < neighbor.PassageCount; candidate++)
            {
                if (neighbor.GetPassageCell(candidate) == neighborCell && neighbor.GetPassageDirection(candidate) == facing)
                {
                    return candidate;
                }
            }

            return NONE;
        }
    }
}
