using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NoWayOut.Core.Events;
using NoWayOut.Networking.Events;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace NoWayOut.Networking.Sessions
{
    internal sealed class SessionService : ISessionService, IDisposable
    {
        private const string PROFILE_PREFIX = "launch_";
        private const int PROFILE_ID_LENGTH = 12;

        private readonly NetworkingConfig config;
        private readonly NetworkManager networkManager;
        private readonly IEventBus eventBus;
        private ISession session;

        public bool IsInSession => session != null;
        public bool IsHost => session != null && session.IsHost;
        public string JoinCode => session?.Code;
        public int PlayerCount => session == null ? 0 : session.PlayerCount;
        public int MaxPlayers => config.maxPlayers;


        public SessionService(NetworkingConfig config, NetworkManager networkManager, IEventBus eventBus)
        {
            this.config = config;
            this.networkManager = networkManager;
            this.eventBus = eventBus;
        }


        public async UniTask HostAsync(CancellationToken cancellationToken = default)
        {
            await EnsureSignedInAsync();

            SessionOptions options = new SessionOptions
            {
                MaxPlayers = config.maxPlayers,
                IsPrivate = true
            }.WithRelayNetwork();

            AttachSession(await MultiplayerService.Instance.CreateSessionAsync(options, cancellationToken));
        }


        public async UniTask JoinAsync(string joinCode, CancellationToken cancellationToken = default)
        {
            await EnsureSignedInAsync();

            string normalizedCode = joinCode.Trim().ToUpperInvariant();
            AttachSession(await MultiplayerService.Instance.JoinSessionByCodeAsync(normalizedCode, new JoinSessionOptions(), cancellationToken));
        }


        public async UniTask CreateOrJoinAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await EnsureSignedInAsync();

            SessionOptions options = new SessionOptions
            {
                MaxPlayers = config.maxPlayers,
                IsPrivate = false
            }.WithRelayNetwork();

            AttachSession(await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options, cancellationToken));
        }


        public async UniTask LeaveAsync()
        {
            if (session == null)
            {
                return;
            }

            ISession leavingSession = DetachSession();

            try
            {
                await leavingSession.LeaveAsync();
            }
            finally
            {
                ShutdownNetwork();
                eventBus.Publish(new SessionEndedEvent());
            }
        }


        public void Dispose()
        {
            if (session == null)
            {
                return;
            }

            ISession leavingSession = DetachSession();
            leavingSession.LeaveAsync().AsUniTask().Forget();
        }


        private void AttachSession(ISession newSession)
        {
            session = newSession;
            session.Changed += HandleSessionChanged;
            session.RemovedFromSession += HandleSessionLost;
            session.Deleted += HandleSessionLost;
            networkManager.OnClientStopped += HandleClientStopped;
            eventBus.Publish(new SessionStartedEvent(session.IsHost));
        }


        private ISession DetachSession()
        {
            ISession detachedSession = session;
            session = null;
            detachedSession.Changed -= HandleSessionChanged;
            detachedSession.RemovedFromSession -= HandleSessionLost;
            detachedSession.Deleted -= HandleSessionLost;

            if (networkManager != null)
            {
                networkManager.OnClientStopped -= HandleClientStopped;
            }

            return detachedSession;
        }


        private void HandleSessionChanged()
        {
            eventBus.Publish(new SessionChangedEvent());
        }


        private void HandleSessionLost()
        {
            if (session == null)
            {
                return;
            }

            DetachSession();
            ShutdownNetwork();
            eventBus.Publish(new SessionEndedEvent());
        }


        private void HandleClientStopped(bool wasHost)
        {
            LeaveAsync().Forget();
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
            if (networkManager != null && networkManager.IsListening)
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
