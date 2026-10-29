using NoWayOut.Networking.Development;
using NoWayOut.Networking.Scenes;
using NoWayOut.Networking.Sessions;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.Networking
{
    public sealed class NetworkingInstaller : IInstaller
    {
        private readonly NetworkManager networkManagerPrefab;
        private readonly NetworkingConfig networkingConfig;


        public NetworkingInstaller(NetworkManager networkManagerPrefab, NetworkingConfig networkingConfig)
        {
            this.networkManagerPrefab = networkManagerPrefab;
            this.networkingConfig = networkingConfig;
        }


        public void Install(IContainerBuilder builder)
        {
            RegisterConfig(builder);
            RegisterNetworkManager(builder);
            RegisterSessionService(builder);
            RegisterDevSession(builder);
            RegisterNetworkSceneLoader(builder);
        }


        private void RegisterConfig(IContainerBuilder builder)
        {
            builder.RegisterInstance(networkingConfig);
        }


        private void RegisterNetworkManager(IContainerBuilder builder)
        {
            builder.RegisterComponentInNewPrefab(networkManagerPrefab, Lifetime.Singleton).DontDestroyOnLoad();
        }


        private void RegisterSessionService(IContainerBuilder builder)
        {
            builder.Register<ISessionService, SessionService>(Lifetime.Singleton);
        }


        private void RegisterDevSession(IContainerBuilder builder)
        {
            builder.Register<IDevSession, DevSession>(Lifetime.Singleton);
        }


        private void RegisterNetworkSceneLoader(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<NetworkSceneLoader>().As<INetworkSceneLoader>();
        }
    }
}
