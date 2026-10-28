using UnityEngine;

namespace NoWayOut.Networking
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/Networking Config", fileName = "NetworkingConfig")]
    public sealed class NetworkingConfig : ScriptableObject
    {
        public int maxPlayers = 4;
    }
}
