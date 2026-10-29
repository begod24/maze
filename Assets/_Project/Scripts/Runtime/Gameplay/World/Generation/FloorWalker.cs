using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class FloorWalker
    {
        public Vector3Int Cell { get; private set; }
        public GridDirection Heading { get; private set; }
        public int DoorwayCount { get; private set; }
        public int SideStepsLeft { get; private set; }
        public int StepsSinceTurn { get; private set; }
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


        public Vector3Int CellAhead(int distance)
        {
            return Cell + GridDirections.ToOffset(Heading) * distance;
        }


        public void CountDoorway()
        {
            DoorwayCount++;
        }


        public void MarkTurn(int sideSteps)
        {
            SideStepsLeft = sideSteps;
            StepsSinceTurn = 0;
        }


        public void CountStep()
        {
            StepsSinceTurn++;
            if (SideStepsLeft > 0)
            {
                SideStepsLeft--;
            }
        }
    }
}
