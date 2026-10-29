using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NoWayOut.Core.Events;
using NoWayOut.Infrastructure.SceneManagement;
using NoWayOut.Networking.Events;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace NoWayOut.Networking.Scenes
{
    internal sealed class NetworkSceneLoader : INetworkSceneLoader, IInitializable, IDisposable
    {
        private readonly NetworkManager networkManager;
        private readonly IEventBus eventBus;
        private NetworkSceneManager sceneManager;


        public NetworkSceneLoader(NetworkManager networkManager, IEventBus eventBus)
        {
            this.networkManager = networkManager;
            this.eventBus = eventBus;
        }


        public void Initialize()
        {
            networkManager.OnClientStarted += HandleClientStarted;
            networkManager.OnClientStopped += HandleClientStopped;
        }


        public void Dispose()
        {
            DetachSceneManager();

            if (networkManager == null)
            {
                return;
            }

            networkManager.OnClientStarted -= HandleClientStarted;
            networkManager.OnClientStopped -= HandleClientStopped;
        }


        public async UniTask LoadAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            if (networkManager.IsServer)
            {
                StartServerLoad(sceneName);
            }

            await UniTask.WaitUntil(() => IsActiveScene(sceneName), cancellationToken: cancellationToken);
        }


        private void StartServerLoad(string sceneName)
        {
            SceneEventProgressStatus status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                throw new InvalidOperationException("Network scene load of " + sceneName + " failed: " + status);
            }
        }


        private void HandleClientStarted()
        {
            DetachSceneManager();
            sceneManager = networkManager.SceneManager;
            sceneManager.VerifySceneBeforeLoading = IsNetworkScene;
            sceneManager.OnLoad += HandleLoad;
        }


        private void HandleClientStopped(bool wasHost)
        {
            DetachSceneManager();
        }


        private void DetachSceneManager()
        {
            if (sceneManager == null)
            {
                return;
            }

            sceneManager.OnLoad -= HandleLoad;
            sceneManager.VerifySceneBeforeLoading = null;
            sceneManager = null;
        }


        private void HandleLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            if (clientId != networkManager.LocalClientId)
            {
                return;
            }

            eventBus.Publish(new NetworkSceneLoadStartedEvent(sceneName));
        }


        private static bool IsNetworkScene(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            return sceneName == SceneNames.GAMEPLAY;
        }


        private static bool IsActiveScene(string sceneName)
        {
            return SceneManager.GetActiveScene().name == sceneName;
        }
    }
}
