using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NoWayOut.Networking.Spawning
{
    internal sealed class InjectingNetworkPrefabHandler : INetworkPrefabInstanceHandler
    {
        private readonly IObjectResolver resolver;
        private readonly NetworkObject prefab;


        public InjectingNetworkPrefabHandler(IObjectResolver resolver, NetworkObject prefab)
        {
            this.resolver = resolver;
            this.prefab = prefab;
        }


        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            return resolver.Instantiate(prefab, position, rotation);
        }


        public void Destroy(NetworkObject networkObject)
        {
            Object.Destroy(networkObject.gameObject);
        }
    }
}
