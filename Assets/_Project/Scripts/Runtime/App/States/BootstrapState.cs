using Cysharp.Threading.Tasks;
using NoWayOut.Core.States;
using NoWayOut.Infrastructure.SceneManagement;
using UnityEngine.SceneManagement;

namespace NoWayOut.App.States
{
    internal sealed class BootstrapState : IState
    {
        private readonly GameStateMachine gameStateMachine;


        public BootstrapState(GameStateMachine gameStateMachine)
        {
            this.gameStateMachine = gameStateMachine;
        }


        public async UniTask EnterAsync()
        {
            if (!IsLaunchedFromBootScene())
            {
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
