using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace NoWayOut.Infrastructure.AssetManagement
{
    public interface IAssetProvider
    {
        UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default) where T : Object;

        UniTask<T> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : Object;

        void Release(string address);

        void Release(AssetReference reference);
    }
}
