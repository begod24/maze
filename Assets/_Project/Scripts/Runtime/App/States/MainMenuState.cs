using System;
using Cysharp.Threading.Tasks;
using NoWayOut.Core.Events;
using NoWayOut.Core.States;
using NoWayOut.Infrastructure.Input;
using NoWayOut.Infrastructure.SceneManagement;
using NoWayOut.Networking.Events;
using NoWayOut.UI.Events;
using UnityEngine;

namespace NoWayOut.App.States
{
    internal sealed class MainMenuState : IState
    {
        private readonly GameStateMachine gameStateMachine;
        private readonly ISceneLoader sceneLoader;
        private readonly IInputService inputService;
        private readonly IEventBus eventBus;
        private IDisposable sessionStartedSubscription;
        private IDisposable quitRequestedSubscription;


        public MainMenuState(GameStateMachine gameStateMachine, ISceneLoader sceneLoader, IInputService inputService, IEventBus eventBus)
        {
            this.gameStateMachine = gameStateMachine;
            this.sceneLoader = sceneLoader;
            this.inputService = inputService;
            this.eventBus = eventBus;
        }


        public async UniTask EnterAsync()
        {
            await sceneLoader.LoadAsync(SceneNames.MAIN_MENU);
            inputService.SetMode(InputMode.UI);
            sessionStartedSubscription = eventBus.Subscribe<SessionStartedEvent>(HandleSessionStarted);
            quitRequestedSubscription = eventBus.Subscribe<QuitRequestedEvent>(HandleQuitRequested);
        }


        public UniTask ExitAsync()
        {
            sessionStartedSubscription?.Dispose();
            quitRequestedSubscription?.Dispose();
            sessionStartedSubscription = null;
            quitRequestedSubscription = null;
            return UniTask.CompletedTask;
        }


        private void HandleSessionStarted(SessionStartedEvent payload)
        {
            gameStateMachine.EnterAsync<LobbyState>().Forget();
        }


        private void HandleQuitRequested(QuitRequestedEvent payload)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
