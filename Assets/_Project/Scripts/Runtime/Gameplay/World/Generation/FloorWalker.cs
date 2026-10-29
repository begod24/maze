using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class FloorWalker
    {
        public Vector3Int Cell { get; private set; }
        public GridDirection Heading { get; private set; }
        public int DoorwayCount { get; private set; }
        public int Level => Cell.y;


        public FloorWalker(Vector3Int cell, GridDirection heading)
        {
            Cell = cell;
            Heading = heading;
        }


        public void MoveTo(Vector3Int cell, GridDirection heading)
        {
            Cell = cell;
            Heading = heading;
        }


        public int DistanceAlongHeadingFrom(Vector3Int origin)
        {
            Vector3Int offset = Cell - origin;
            Vector3Int forward = GridDirections.ToOffset(Heading);
            return offset.x * forward.x + offset.z * forward.z;
        }


        public void CountDoorway()
        {
            DoorwayCount++;
        }
    }
}
