using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace NoWayOut.Infrastructure.AssetManagement
{
    internal sealed class AddressablesAssetProvider : IAssetProvider, IDisposable
    {
        private readonly Dictionary<object, AsyncOperationHandle> handles = new Dictionary<object, AsyncOperationHandle>();


        public UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default) where T : Object
        {
            return LoadByKeyAsync<T>(address, cancellationToken);
        }


        public UniTask<T> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : Object
        {
            return LoadByKeyAsync<T>(reference.RuntimeKey, cancellationToken);
        }


        public void Release(string address)
        {
            ReleaseByKey(address);
        }


        public void Release(AssetReference reference)
        {
            ReleaseByKey(reference.RuntimeKey);
        }


        public void Dispose()
        {
            foreach (AsyncOperationHandle handle in handles.Values)
            {
                Addressables.Release(handle);
            }

            handles.Clear();
        }


        private async UniTask<T> LoadByKeyAsync<T>(object key, CancellationToken cancellationToken) where T : Object
        {
            if (!handles.TryGetValue(key, out AsyncOperationHandle handle))
            {
                handle = Addressables.LoadAssetAsync<T>(key);
                handles.Add(key, handle);
            }

            await handle.ToUniTask(cancellationToken: cancellationToken);
            return (T)handle.Result;
        }


        private void ReleaseByKey(object key)
        {
            if (handles.Remove(key, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
            }
        }
    }
}
