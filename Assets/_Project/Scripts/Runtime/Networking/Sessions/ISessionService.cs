using System.Threading;
using Cysharp.Threading.Tasks;

namespace NoWayOut.Networking.Sessions
{
    public interface ISessionService
    {
        bool IsInSession { get; }

        bool IsHost { get; }

        string JoinCode { get; }

        int PlayerCount { get; }

        int MaxPlayers { get; }

        UniTask HostAsync(CancellationToken cancellationToken = default);

        UniTask JoinAsync(string joinCode, CancellationToken cancellationToken = default);

        UniTask CreateOrJoinAsync(string sessionId, CancellationToken cancellationToken = default);

        UniTask LeaveAsync();
    }
}
