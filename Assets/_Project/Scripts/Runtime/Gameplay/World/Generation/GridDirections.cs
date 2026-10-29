using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal static class GridDirections
    {
        private const int DIRECTION_COUNT = 4;
        private const int HALF_TURN = 2;


        public static Vector3Int ToOffset(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.North:
                    return new Vector3Int(0, 0, 1);
                case GridDirection.East:
                    return new Vector3Int(1, 0, 0);
                case GridDirection.South:
                    return new Vector3Int(0, 0, -1);
                default:
                    return new Vector3Int(-1, 0, 0);
            }
        }


        public static GridDirection Rotate(GridDirection direction, int quarterTurns)
        {
            int index = Wrap((int)direction + quarterTurns);
            return (GridDirection)index;
        }


        public static GridDirection Opposite(GridDirection direction)
        {
            return Rotate(direction, HALF_TURN);
        }


        public static int TurnsBetween(GridDirection from, GridDirection to)
        {
            return Wrap((int)to - (int)from);
        }


        public static Vector3Int RotateOffset(Vector3Int offset, int quarterTurns)
        {
            Vector3Int result = offset;
            int turns = Wrap(quarterTurns);

            for (int i = 0; i < turns; i++)
            {
                result = new Vector3Int(result.z, result.y, -result.x);
            }

            return result;
        }


        private static int Wrap(int value)
        {
            int wrapped = value % DIRECTION_COUNT;
            if (wrapped < 0)
            {
                wrapped += DIRECTION_COUNT;
            }

            return wrapped;
        }
    }
}
