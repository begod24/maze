using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace NoWayOut.Networking.Sessions
{
    internal sealed class SessionService : ISessionService
    {
        private const string PROFILE_PREFIX = "launch_";
        private const int PROFILE_ID_LENGTH = 12;

        private readonly NetworkingConfig config;
        private readonly NetworkManager networkManager;
        private ISession session;

        public bool IsInSession => session != null;
        public string JoinCode => session?.Code;


        public SessionService(NetworkingConfig config, NetworkManager networkManager)
        {
            this.config = config;
            this.networkManager = networkManager;
        }


        public async UniTask HostAsync(CancellationToken cancellationToken = default)
        {
            await EnsureSignedInAsync();

            SessionOptions options = new SessionOptions
            {
                MaxPlayers = config.maxPlayers,
                IsPrivate = true
            }.WithRelayNetwork();

            session = await MultiplayerService.Instance.CreateSessionAsync(options, cancellationToken);
        }


        public async UniTask JoinAsync(string joinCode, CancellationToken cancellationToken = default)
        {
            await EnsureSignedInAsync();
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode, new JoinSessionOptions(), cancellationToken);
        }


        public async UniTask LeaveAsync()
        {
            if (session == null)
            {
                return;
            }

            ISession leavingSession = session;
            session = null;
            await leavingSession.LeaveAsync();
            ShutdownNetwork();
        }


        private async UniTask EnsureSignedInAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                InitializationOptions options = new InitializationOptions().SetProfile(CreateLaunchProfileName());
                await UnityServices.InitializeAsync(options);
            }

            await UniTask.WaitWhile(IsServicesInitializing);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }


        private void ShutdownNetwork()
        {
            if (networkManager.IsListening)
            {
                networkManager.Shutdown();
            }
        }


        private static bool IsServicesInitializing()
        {
            return UnityServices.State == ServicesInitializationState.Initializing;
        }


        private static string CreateLaunchProfileName()
        {
            string id = Guid.NewGuid().ToString("N").Substring(0, PROFILE_ID_LENGTH);
            return PROFILE_PREFIX + id;
        }
    }
}
