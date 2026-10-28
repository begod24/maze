namespace NoWayOut.Core.States
{
    public interface IStateFactory
    {
        TState Create<TState>() where TState : class, IExitableState;
    }
}
