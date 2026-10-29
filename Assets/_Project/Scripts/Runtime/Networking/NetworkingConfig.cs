using UnityEngine;

namespace NoWayOut.Networking
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/Networking Config", fileName = "NetworkingConfig")]
    public sealed class NetworkingConfig : ScriptableObject
    {
        public int maxPlayers = 4;
        public bool devQuickStart = false;
        public string devSessionPrefix = "nwo-dev-";
        public float devJoinDelaySeconds = 2f;
    }
}
