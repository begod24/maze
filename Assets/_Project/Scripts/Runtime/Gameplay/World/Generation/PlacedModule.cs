using UnityEngine;

namespace NoWayOut.Gameplay.World.Generation
{
    internal readonly struct PlacedModule
    {
        private const float QUARTER_TURN_DEGREES = 90f;

        public WorldModuleDefinition Definition { get; }
        public Vector3Int Anchor { get; }
        public int Rotation { get; }
        public int CellCount => Definition.cells.Count;
        public int PassageCount => Definition.passages.Count;


        public PlacedModule(WorldModuleDefinition definition, Vector3Int anchor, int rotation)
        {
            Definition = definition;
            Anchor = anchor;
            Rotation = rotation;
        }


        public Vector3Int GetCell(int index)
        {
            return Anchor + GridDirections.RotateOffset(Definition.cells[index], Rotation);
        }


        public Vector3Int GetPassageCell(int index)
        {
            return Anchor + GridDirections.RotateOffset(Definition.passages[index].cell, Rotation);
        }


        public GridDirection GetPassageDirection(int index)
        {
            return GridDirections.Rotate(Definition.passages[index].direction, Rotation);
        }


        public Vector3Int GetPassageNeighbor(int index)
        {
            return GetPassageCell(index) + GridDirections.ToOffset(GetPassageDirection(index));
        }


        public Vector3 GetWorldPosition(float cellSize)
        {
            return new Vector3(Anchor.x, Anchor.y, Anchor.z) * cellSize;
        }


        public Quaternion GetWorldRotation()
        {
            return Quaternion.Euler(0f, Rotation * QUARTER_TURN_DEGREES, 0f);
        }
    }
}
