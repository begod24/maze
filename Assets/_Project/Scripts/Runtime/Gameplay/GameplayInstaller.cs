using NoWayOut.Gameplay.Player;
using NoWayOut.Gameplay.World;
using NoWayOut.Gameplay.World.Generation;
using NoWayOut.Gameplay.World.Weather;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.Gameplay
{
    public sealed class GameplayInstaller : IInstaller
    {
        private readonly PlayerConfig playerConfig;
        private readonly FloorConfig floorConfig;
        private readonly FloorAtmosphereConfig atmosphereConfig;
        private readonly WeatherConfig weatherConfig;
        private readonly WorldModuleCatalog moduleCatalog;


        public GameplayInstaller(PlayerConfig playerConfig, FloorConfig floorConfig, FloorAtmosphereConfig atmosphereConfig, WeatherConfig weatherConfig, WorldModuleCatalog moduleCatalog)
        {
            this.playerConfig = playerConfig;
            this.floorConfig = floorConfig;
            this.atmosphereConfig = atmosphereConfig;
            this.weatherConfig = weatherConfig;
            this.moduleCatalog = moduleCatalog;
        }


        public void Install(IContainerBuilder builder)
        {
            RegisterConfigs(builder);
            RegisterFloor(builder);
            RegisterWeather(builder);
            RegisterPlayerSpawner(builder);
        }


        private void RegisterConfigs(IContainerBuilder builder)
        {
            builder.RegisterInstance(playerConfig);
            builder.RegisterInstance(floorConfig);
            builder.RegisterInstance(atmosphereConfig);
            builder.RegisterInstance(weatherConfig);
            builder.RegisterInstance(moduleCatalog);
        }


        private void RegisterFloor(IContainerBuilder builder)
        {
            builder.Register<FloorGenerator>(Lifetime.Singleton);
            builder.Register<PuddleField>(Lifetime.Singleton);
            builder.Register<FogField>(Lifetime.Singleton);
            builder.Register<FloorAtmosphere>(Lifetime.Singleton);
            builder.Register<IFloor, Floor>(Lifetime.Singleton);
        }


        private void RegisterWeather(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<WeatherService>().As<IWeather>();
        }


        private void RegisterPlayerSpawner(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<PlayerSpawner>();
        }
    }
}
