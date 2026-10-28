using System.Threading;
using Cysharp.Threading.Tasks;

namespace NoWayOut.Infrastructure.SceneManagement
{
    public interface ISceneLoader
    {
        UniTask LoadAsync(string sceneName, CancellationToken cancellationToken = default);
    }
}
