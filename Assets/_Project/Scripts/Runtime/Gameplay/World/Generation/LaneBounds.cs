using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal readonly struct LaneBounds
    {
        public int MinX { get; }
        public int MaxX { get; }


        public LaneBounds(int minX, int maxX)
        {
            MinX = minX;
            MaxX = maxX;
        }


        public bool Contains(Vector3Int cell)
        {
            return cell.x >= MinX && cell.x <= MaxX;
        }


        public bool HasRoom(int x, GridDirection side, int cells)
        {
            if (side == GridDirection.East)
            {
                return x + cells <= MaxX;
            }

            return x - cells >= MinX;
        }
    }
}
