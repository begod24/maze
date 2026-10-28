using VContainer;
using VContainer.Unity;

namespace NoWayOut.Networking.Spawning
{
    public static class NetworkContainerBuilderExtensions
    {
        public static void RegisterNetworkPrefabInjection(this IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<NetworkPrefabInjection>();
        }
    }
}
