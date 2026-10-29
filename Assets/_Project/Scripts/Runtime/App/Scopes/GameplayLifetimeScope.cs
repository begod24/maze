using NoWayOut.Gameplay;
using NoWayOut.Gameplay.Player;
using NoWayOut.Gameplay.World;
using NoWayOut.Networking.Spawning;
using NoWayOut.UI;
using NoWayOut.UI.Common;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.App.Scopes
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerConfig playerConfig;
        [SerializeField] private FloorConfig floorConfig;
        [SerializeField] private WorldModuleCatalog moduleCatalog;
        [SerializeField] private UIScreensConfig uiScreensConfig;


        protected override void Configure(IContainerBuilder builder)
        {
            InstallGameplay(builder);
            InstallUI(builder);
            RegisterNetworkPrefabInjection(builder);
        }


        private void InstallGameplay(IContainerBuilder builder)
        {
            new GameplayInstaller(playerConfig, floorConfig, moduleCatalog).Install(builder);
        }


        private void InstallUI(IContainerBuilder builder)
        {
            new UIInstaller(uiScreensConfig).Install(builder);
        }


        private void RegisterNetworkPrefabInjection(IContainerBuilder builder)
        {
            builder.RegisterNetworkPrefabInjection();
        }
    }
}
