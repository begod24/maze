using Unity.Netcode;
using UnityEngine;

namespace NoWayOut.Gameplay.Player
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        public NetworkObject playerPrefab;
        public float walkSpeed = 3.5f;
        public float sprintSpeed = 6f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;
        public float groundedVerticalSpeed = -2f;
        public float lookSensitivity = 0.1f;
        public float minPitch = -85f;
        public float maxPitch = 85f;
        public float fallRespawnHeight = -30f;
    }
}
