using NoWayOut.App.States;
using NoWayOut.Infrastructure;
using NoWayOut.Networking;
using NoWayOut.Voice;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.App.Scopes
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private NetworkManager networkManagerPrefab;
        [SerializeField] private NetworkingConfig networkingConfig;


        protected override void Configure(IContainerBuilder builder)
        {
            InstallInfrastructure(builder);
            InstallNetworking(builder);
            InstallVoice(builder);
            RegisterGameStateMachine(builder);
            RegisterGameStates(builder);
            RegisterEntryPoint(builder);
        }


        private void InstallInfrastructure(IContainerBuilder builder)
        {
            new InfrastructureInstaller().Install(builder);
        }


        private void InstallNetworking(IContainerBuilder builder)
        {
            new NetworkingInstaller(networkManagerPrefab, networkingConfig).Install(builder);
        }


        private void InstallVoice(IContainerBuilder builder)
        {
            new VoiceInstaller().Install(builder);
        }


        private void RegisterGameStateMachine(IContainerBuilder builder)
        {
            builder.Register<GameStateMachine>(Lifetime.Singleton);
        }


        private void RegisterGameStates(IContainerBuilder builder)
        {
            builder.Register<BootstrapState>(Lifetime.Singleton);
            builder.Register<MainMenuState>(Lifetime.Singleton);
            builder.Register<LobbyState>(Lifetime.Singleton);
            builder.Register<DevSessionState>(Lifetime.Singleton);
            builder.Register<GameplayState>(Lifetime.Singleton);
        }


        private void RegisterEntryPoint(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
