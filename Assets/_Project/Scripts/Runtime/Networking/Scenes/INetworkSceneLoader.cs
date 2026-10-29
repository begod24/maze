using System.Threading;
using Cysharp.Threading.Tasks;

namespace NoWayOut.Networking.Scenes
{
    public interface INetworkSceneLoader
    {
        UniTask LoadAsync(string sceneName, CancellationToken cancellationToken = default);
    }
}
