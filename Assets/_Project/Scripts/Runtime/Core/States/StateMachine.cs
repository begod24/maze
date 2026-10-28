using Cysharp.Threading.Tasks;

namespace NoWayOut.Core.States
{
    public abstract class StateMachine
    {
        private readonly IStateFactory stateFactory;
        private IExitableState activeState;

        public IExitableState ActiveState => activeState;


        protected StateMachine(IStateFactory stateFactory)
        {
            this.stateFactory = stateFactory;
        }


        public async UniTask EnterAsync<TState>() where TState : class, IState
        {
            TState state = await ChangeStateAsync<TState>();
            await state.EnterAsync();
        }


        public async UniTask EnterAsync<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        {
            TState state = await ChangeStateAsync<TState>();
            await state.EnterAsync(payload);
        }


        private async UniTask<TState> ChangeStateAsync<TState>() where TState : class, IExitableState
        {
            if (activeState != null)
            {
                await activeState.ExitAsync();
            }

            TState state = stateFactory.Create<TState>();
            activeState = state;
            return state;
        }
    }
}
