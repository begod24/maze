using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    public sealed class WorldAlignedFollower : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset;

        private Transform anchor;


        private void Awake()
        {
            anchor = transform.parent;
        }


        private void LateUpdate()
        {
            transform.SetPositionAndRotation(anchor.position + worldOffset, Quaternion.identity);
        }
    }
}
