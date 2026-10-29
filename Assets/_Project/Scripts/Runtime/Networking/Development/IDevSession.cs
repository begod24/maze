using System.Threading;
using Cysharp.Threading.Tasks;

namespace NoWayOut.Networking.Development
{
    public interface IDevSession
    {
        bool IsEnabled { get; }

        UniTask StartAsync(CancellationToken cancellationToken = default);
    }
}
