using Cysharp.Threading.Tasks;
using NoWayOut.Core.States;
using NoWayOut.Infrastructure.SceneManagement;
using NoWayOut.Networking.Development;
using UnityEngine.SceneManagement;

namespace NoWayOut.App.States
{
    internal sealed class BootstrapState : IState
    {
        private readonly GameStateMachine gameStateMachine;
        private readonly IDevSession devSession;


        public BootstrapState(GameStateMachine gameStateMachine, IDevSession devSession)
        {
            this.gameStateMachine = gameStateMachine;
            this.devSession = devSession;
        }


        public async UniTask EnterAsync()
        {
            if (!IsLaunchedFromBootScene())
            {
                return;
            }

            if (devSession.IsEnabled)
            {
                await gameStateMachine.EnterAsync<DevSessionState>();
                return;
            }

            await gameStateMachine.EnterAsync<MainMenuState>();
        }


        public UniTask ExitAsync()
        {
            return UniTask.CompletedTask;
        }


        private bool IsLaunchedFromBootScene()
        {
            return SceneManager.GetActiveScene().name == SceneNames.BOOT;
        }
    }
}
