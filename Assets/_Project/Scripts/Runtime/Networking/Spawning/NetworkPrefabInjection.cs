using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.Networking.Spawning
{
    internal sealed class NetworkPrefabInjection : IInitializable, IDisposable
    {
        private readonly NetworkManager networkManager;
        private readonly IObjectResolver resolver;
        private readonly List<GameObject> registeredPrefabs = new List<GameObject>();


        public NetworkPrefabInjection(NetworkManager networkManager, IObjectResolver resolver)
        {
            this.networkManager = networkManager;
            this.resolver = resolver;
        }


        public void Initialize()
        {
            foreach (NetworkPrefabsList prefabsList in networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists)
            {
                RegisterHandlers(prefabsList);
            }
        }


        public void Dispose()
        {
            if (networkManager == null)
            {
                return;
            }

            foreach (GameObject prefab in registeredPrefabs)
            {
                networkManager.PrefabHandler.RemoveHandler(prefab);
            }

            registeredPrefabs.Clear();
        }


        private void RegisterHandlers(NetworkPrefabsList prefabsList)
        {
            foreach (NetworkPrefab networkPrefab in prefabsList.PrefabList)
            {
                RegisterHandler(networkPrefab.Prefab);
            }
        }


        private void RegisterHandler(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
            InjectingNetworkPrefabHandler handler = new InjectingNetworkPrefabHandler(resolver, networkObject);

            if (networkManager.PrefabHandler.AddHandler(prefab, handler))
            {
                registeredPrefabs.Add(prefab);
            }
        }
    }
}
