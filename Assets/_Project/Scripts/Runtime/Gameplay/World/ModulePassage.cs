using System;
using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    [Serializable]
    public sealed class ModulePassage
    {
        public Vector3Int cell;
        public GridDirection direction;
    }
}
