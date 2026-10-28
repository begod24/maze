using NoWayOut.Gameplay;
using NoWayOut.Networking.Spawning;
using NoWayOut.UI;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.App.Scopes
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            InstallGameplay(builder);
            InstallUI(builder);
            RegisterNetworkPrefabInjection(builder);
        }


        private void InstallGameplay(IContainerBuilder builder)
        {
            new GameplayInstaller().Install(builder);
        }


        private void InstallUI(IContainerBuilder builder)
        {
            new UIInstaller().Install(builder);
        }


        private void RegisterNetworkPrefabInjection(IContainerBuilder builder)
        {
            builder.RegisterNetworkPrefabInjection();
        }
    }
}
