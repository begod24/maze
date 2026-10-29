using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    public sealed class PlayerSpawnPoints : MonoBehaviour
    {
        [SerializeField] private Transform[] points;

        public int Count => points.Length;


        public Transform GetPoint(int index)
        {
            return points[index];
        }
    }
}
