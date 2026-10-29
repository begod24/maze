using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NoWayOut.Networking.Sessions;
using UnityEngine;

namespace NoWayOut.Networking.Development
{
    internal sealed class DevSession : IDevSession
    {
        private const int DEVICE_ID_LENGTH = 16;

        private readonly NetworkingConfig config;
        private readonly ISessionService sessionService;

        public bool IsEnabled => Application.isEditor && config.devQuickStart;


        public DevSession(NetworkingConfig config, ISessionService sessionService)
        {
            this.config = config;
            this.sessionService = sessionService;
        }


        public async UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            if (!IsMainEditor())
            {
                await UniTask.Delay(TimeSpan.FromSeconds(config.devJoinDelaySeconds), cancellationToken: cancellationToken);
            }

            await sessionService.CreateOrJoinAsync(CreateSessionId(), cancellationToken);
        }


        private string CreateSessionId()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            int length = Math.Min(DEVICE_ID_LENGTH, deviceId.Length);
            return config.devSessionPrefix + deviceId.Substring(0, length);
        }


        private static bool IsMainEditor()
        {
#if UNITY_EDITOR
            return Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
#else
            return true;
#endif
        }
    }
}
