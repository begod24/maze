using System.Threading;
using Cysharp.Threading.Tasks;
using NoWayOut.App.States;
using VContainer.Unity;

namespace NoWayOut.App
{
    internal sealed class GameEntryPoint : IAsyncStartable
    {
        private readonly GameStateMachine gameStateMachine;


        public GameEntryPoint(GameStateMachine gameStateMachine)
        {
            this.gameStateMachine = gameStateMachine;
        }


        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            await gameStateMachine.EnterAsync<BootstrapState>();
        }
    }
}
