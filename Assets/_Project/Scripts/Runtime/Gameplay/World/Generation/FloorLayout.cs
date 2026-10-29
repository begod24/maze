using System.Collections.Generic;
using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class FloorLayout
    {
        private const int NONE = -1;

        private readonly List<PlacedModule> modules = new List<PlacedModule>();
        private readonly Dictionary<Vector3Int, int> occupancy = new Dictionary<Vector3Int, int>();

        public int Seed { get; }
        public FloorStartVariant StartVariant { get; }
        public IReadOnlyList<PlacedModule> Modules => modules;
        public int CellCount => occupancy.Count;
        public int StartIndex { get; private set; } = NONE;
        public int ExitIndex { get; private set; } = NONE;


        public FloorLayout(int seed, FloorStartVariant startVariant)
        {
            Seed = seed;
            StartVariant = startVariant;
        }


        public bool IsFree(PlacedModule module)
        {
            for (int i = 0; i < module.CellCount; i++)
            {
                if (occupancy.ContainsKey(module.GetCell(i)))
                {
                    return false;
                }
            }

            return true;
        }


        public int Add(PlacedModule module)
        {
            int index = modules.Count;
            modules.Add(module);

            for (int i = 0; i < module.CellCount; i++)
            {
                occupancy[module.GetCell(i)] = index;
            }

            return index;
        }


        public bool TryGetModuleAt(Vector3Int cell, out int index)
        {
            return occupancy.TryGetValue(cell, out index);
        }


        public void MarkStart(int index)
        {
            StartIndex = index;
        }


        public void MarkExit(int index)
        {
            ExitIndex = index;
        }
    }
}
