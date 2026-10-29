using System;
using Cysharp.Threading.Tasks;
using NoWayOut.Core.Events;
using NoWayOut.Core.States;
using NoWayOut.Infrastructure.SceneManagement;
using NoWayOut.Networking.Events;
using NoWayOut.Networking.Sessions;
using NoWayOut.UI.Events;

namespace NoWayOut.App.States
{
    internal sealed class LobbyState : IState
    {
        private readonly GameStateMachine gameStateMachine;
        private readonly ISessionService sessionService;
        private readonly IEventBus eventBus;
        private IDisposable gameStartSubscription;
        private IDisposable sceneLoadSubscription;
        private IDisposable sessionEndedSubscription;


        public LobbyState(GameStateMachine gameStateMachine, ISessionService sessionService, IEventBus eventBus)
        {
            this.gameStateMachine = gameStateMachine;
            this.sessionService = sessionService;
            this.eventBus = eventBus;
        }


        public UniTask EnterAsync()
        {
            gameStartSubscription = eventBus.Subscribe<GameStartRequestedEvent>(HandleGameStartRequested);
            sceneLoadSubscription = eventBus.Subscribe<NetworkSceneLoadStartedEvent>(HandleNetworkSceneLoadStarted);
            sessionEndedSubscription = eventBus.Subscribe<SessionEndedEvent>(HandleSessionEnded);
            return UniTask.CompletedTask;
        }


        public UniTask ExitAsync()
        {
            gameStartSubscription?.Dispose();
            sceneLoadSubscription?.Dispose();
            sessionEndedSubscription?.Dispose();
            gameStartSubscription = null;
            sceneLoadSubscription = null;
            sessionEndedSubscription = null;
            return UniTask.CompletedTask;
        }


        private void HandleGameStartRequested(GameStartRequestedEvent payload)
        {
            if (!sessionService.IsHost)
            {
                return;
            }

            gameStateMachine.EnterAsync<GameplayState>().Forget();
        }


        private void HandleNetworkSceneLoadStarted(NetworkSceneLoadStartedEvent payload)
        {
            if (payload.SceneName != SceneNames.GAMEPLAY)
            {
                return;
            }

            gameStateMachine.EnterAsync<GameplayState>().Forget();
        }


        private void HandleSessionEnded(SessionEndedEvent payload)
        {
            gameStateMachine.EnterAsync<MainMenuState>().Forget();
        }
    }
}
