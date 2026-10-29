using NoWayOut.Gameplay.Player;
using NoWayOut.Gameplay.World;
using NoWayOut.Gameplay.World.Generation;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.Gameplay
{
    public sealed class GameplayInstaller : IInstaller
    {
        private readonly PlayerConfig playerConfig;
        private readonly FloorConfig floorConfig;
        private readonly WorldModuleCatalog moduleCatalog;


        public GameplayInstaller(PlayerConfig playerConfig, FloorConfig floorConfig, WorldModuleCatalog moduleCatalog)
        {
            this.playerConfig = playerConfig;
            this.floorConfig = floorConfig;
            this.moduleCatalog = moduleCatalog;
        }


        public void Install(IContainerBuilder builder)
        {
            RegisterConfigs(builder);
            RegisterFloor(builder);
            RegisterPlayerSpawner(builder);
        }


        private void RegisterConfigs(IContainerBuilder builder)
        {
            builder.RegisterInstance(playerConfig);
            builder.RegisterInstance(floorConfig);
            builder.RegisterInstance(moduleCatalog);
        }


        private void RegisterFloor(IContainerBuilder builder)
        {
            builder.Register<FloorGenerator>(Lifetime.Singleton);
            builder.Register<IFloor, Floor>(Lifetime.Singleton);
        }


        private void RegisterPlayerSpawner(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<PlayerSpawner>();
        }
    }
}
