using NoWayOut.Core.Events;
using NoWayOut.Core.States;
using NoWayOut.Infrastructure.AssetManagement;
using NoWayOut.Infrastructure.Input;
using NoWayOut.Infrastructure.SceneManagement;
using NoWayOut.Infrastructure.States;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.Infrastructure
{
    public sealed class InfrastructureInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            RegisterEventBus(builder);
            RegisterStateFactory(builder);
            RegisterSceneLoader(builder);
            RegisterAssetProvider(builder);
            RegisterInputService(builder);
        }


        private void RegisterEventBus(IContainerBuilder builder)
        {
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
        }


        private void RegisterStateFactory(IContainerBuilder builder)
        {
            builder.Register<IStateFactory, ContainerStateFactory>(Lifetime.Scoped);
        }


        private void RegisterSceneLoader(IContainerBuilder builder)
        {
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
        }


        private void RegisterAssetProvider(IContainerBuilder builder)
        {
            builder.Register<IAssetProvider, AddressablesAssetProvider>(Lifetime.Scoped);
        }


        private void RegisterInputService(IContainerBuilder builder)
        {
            builder.Register<IInputService, InputService>(Lifetime.Singleton);
        }
    }
}
