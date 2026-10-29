using System;
using NoWayOut.Gameplay.World;
using NoWayOut.Infrastructure.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace NoWayOut.Gameplay.Player
{
    internal sealed class PlayerSpawner : IInitializable, IDisposable
    {
        private readonly NetworkManager networkManager;
        private readonly PlayerConfig config;
        private readonly IFloor floor;
        private NetworkSceneManager sceneManager;
        private int nextSpawnIndex;


        public PlayerSpawner(NetworkManager networkManager, PlayerConfig config, IFloor floor)
        {
            this.networkManager = networkManager;
            this.config = config;
            this.floor = floor;
        }


        public void Initialize()
        {
            if (!networkManager.IsServer)
            {
                return;
            }

            sceneManager = networkManager.SceneManager;
            sceneManager.OnLoadComplete += HandleLoadComplete;
            sceneManager.OnSynchronizeComplete += HandleSynchronizeComplete;
        }


        public void Dispose()
        {
            if (sceneManager == null)
            {
                return;
            }

            sceneManager.OnLoadComplete -= HandleLoadComplete;
            sceneManager.OnSynchronizeComplete -= HandleSynchronizeComplete;
            sceneManager = null;
        }


        private void HandleLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName != SceneNames.GAMEPLAY)
            {
                return;
            }

            SpawnPlayer(clientId);
        }


        private void HandleSynchronizeComplete(ulong clientId)
        {
            SpawnPlayer(clientId);
        }


        private void SpawnPlayer(ulong clientId)
        {
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            {
                return;
            }

            if (client.PlayerObject != null)
            {
                return;
            }

            PlayerSpawnPoints spawnPoints = floor.SpawnPoints;
            Transform point = spawnPoints.GetPoint(nextSpawnIndex % spawnPoints.Count);
            nextSpawnIndex++;

            networkManager.SpawnManager.InstantiateAndSpawn(
                config.playerPrefab,
                clientId,
                destroyWithScene: true,
                isPlayerObject: true,
                position: point.position,
                rotation: point.rotation);
        }
    }
}
