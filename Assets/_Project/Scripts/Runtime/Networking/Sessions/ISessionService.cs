using System.Threading;
using Cysharp.Threading.Tasks;

namespace NoWayOut.Networking.Sessions
{
    public interface ISessionService
    {
        bool IsInSession { get; }

        string JoinCode { get; }

        UniTask HostAsync(CancellationToken cancellationToken = default);

        UniTask JoinAsync(string joinCode, CancellationToken cancellationToken = default);

        UniTask LeaveAsync();
    }
}
