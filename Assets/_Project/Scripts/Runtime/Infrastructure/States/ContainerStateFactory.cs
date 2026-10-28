using NoWayOut.Core.States;
using VContainer;

namespace NoWayOut.Infrastructure.States
{
    internal sealed class ContainerStateFactory : IStateFactory
    {
        private readonly IObjectResolver resolver;


        public ContainerStateFactory(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }


        public TState Create<TState>() where TState : class, IExitableState
        {
            return resolver.Resolve<TState>();
        }
    }
}
