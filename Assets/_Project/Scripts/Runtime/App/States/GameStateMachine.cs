using NoWayOut.Core.States;

namespace NoWayOut.App.States
{
    public sealed class GameStateMachine : StateMachine
    {
        public GameStateMachine(IStateFactory stateFactory) : base(stateFactory)
        {
        }
    }
}
