using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NoWayOut.Core.Events;
using NoWayOut.Core.States;
using NoWayOut.Infrastructure.Input;
using NoWayOut.Infrastructure.SceneManagement;
using NoWayOut.Networking.Events;
using NoWayOut.Networking.Scenes;

namespace NoWayOut.App.States
{
    internal sealed class GameplayState : IState
    {
        private readonly GameStateMachine gameStateMachine;
        private readonly INetworkSceneLoader networkSceneLoader;
        private readonly IInputService inputService;
        private readonly IEventBus eventBus;
        private IDisposable sessionEndedSubscription;
        private CancellationTokenSource loadCancellation;


        public GameplayState(GameStateMachine gameStateMachine, INetworkSceneLoader networkSceneLoader, IInputService inputService, IEventBus eventBus)
        {
            this.gameStateMachine = gameStateMachine;
            this.networkSceneLoader = networkSceneLoader;
            this.inputService = inputService;
            this.eventBus = eventBus;
        }


        public async UniTask EnterAsync()
        {
            sessionEndedSubscription = eventBus.Subscribe<SessionEndedEvent>(HandleSessionEnded);
            loadCancellation = new CancellationTokenSource();

            await networkSceneLoader.LoadAsync(SceneNames.GAMEPLAY, loadCancellation.Token);
            inputService.SetMode(InputMode.Gameplay);
        }


        public UniTask ExitAsync()
        {
            sessionEndedSubscription?.Dispose();
            sessionEndedSubscription = null;
            loadCancellation?.Cancel();
            loadCancellation?.Dispose();
            loadCancellation = null;
            inputService.SetMode(InputMode.Disabled);
            return UniTask.CompletedTask;
        }


        private void HandleSessionEnded(SessionEndedEvent payload)
        {
            gameStateMachine.EnterAsync<MainMenuState>().Forget();
        }
    }
}
