using Cysharp.Threading.Tasks;
using NoWayOut.Core.States;
using NoWayOut.Networking.Development;

namespace NoWayOut.App.States
{
    internal sealed class DevSessionState : IState
    {
        private readonly GameStateMachine gameStateMachine;
        private readonly IDevSession devSession;


        public DevSessionState(GameStateMachine gameStateMachine, IDevSession devSession)
        {
            this.gameStateMachine = gameStateMachine;
            this.devSession = devSession;
        }


        public async UniTask EnterAsync()
        {
            await devSession.StartAsync();
            await gameStateMachine.EnterAsync<GameplayState>();
        }


        public UniTask ExitAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
