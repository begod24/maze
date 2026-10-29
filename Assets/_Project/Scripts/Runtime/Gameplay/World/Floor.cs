using System;
using NoWayOut.Gameplay.World.Generation;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace NoWayOut.Gameplay.World
{
    internal sealed class Floor : IFloor, IDisposable
    {
        private const string ROOT_NAME = "Floor";

        private readonly FloorGenerator generator;
        private readonly FloorConfig config;
        private readonly IObjectResolver resolver;
        private GameObject root;

        public bool IsBuilt => root != null;
        public FloorLayout Layout { get; private set; }
        public PlayerSpawnPoints SpawnPoints { get; private set; }


        public Floor(FloorGenerator generator, FloorConfig config, IObjectResolver resolver)
        {
            this.generator = generator;
            this.config = config;
            this.resolver = resolver;
        }


        public void Build(int seed, Transform parent)
        {
            Clear();
            Layout = generator.Generate(seed);

            root = new GameObject(ROOT_NAME);
            root.transform.SetParent(parent, false);

            for (int i = 0; i < Layout.Modules.Count; i++)
            {
                GameObject instance = Spawn(Layout.Modules[i]);
                if (i == Layout.StartIndex)
                {
                    SpawnPoints = instance.GetComponent<PlayerSpawnPoints>();
                }
            }
        }


        public void Dispose()
        {
            Clear();
        }


        private GameObject Spawn(PlacedModule module)
        {
            Vector3 position = module.GetWorldPosition(config.cellSize);
            return resolver.Instantiate(module.Definition.prefab, position, module.GetWorldRotation(), root.transform);
        }


        private void Clear()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }

            root = null;
            Layout = null;
            SpawnPoints = null;
        }
    }
}
