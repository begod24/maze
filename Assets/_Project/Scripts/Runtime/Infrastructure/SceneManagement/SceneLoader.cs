using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace NoWayOut.Infrastructure.SceneManagement
{
    internal sealed class SceneLoader : ISceneLoader
    {
        public async UniTask LoadAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                return;
            }

            await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
