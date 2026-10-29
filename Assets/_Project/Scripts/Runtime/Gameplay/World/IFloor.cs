using NoWayOut.Gameplay.World.Generation;
using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    internal interface IFloor
    {
        bool IsBuilt { get; }

        FloorLayout Layout { get; }

        PlayerSpawnPoints SpawnPoints { get; }

        void Build(int seed, Transform parent);
    }
}
