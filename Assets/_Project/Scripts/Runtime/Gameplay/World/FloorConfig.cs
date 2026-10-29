using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/Floor Config", fileName = "FloorConfig")]
    public sealed class FloorConfig : ScriptableObject
    {
        public float cellSize = 4f;
        public int seed = 0;
        public float loopStartChance = 0.5f;
        public int bridgeRowsMin = 8;
        public int bridgeRowsMax = 11;
        public int loopRowsMin = 2;
        public int loopRowsMax = 4;
        public int separationRowsMin = 4;
        public int separationRowsMax = 6;
        public int minLevel = -3;
        public int maxLevel = 3;
        public float stairChance = 0.25f;
        public float doorwayChance = 0.12f;
        public int maxDoorwaysPerLane = 1;
        public int maxAttempts = 64;
    }
}
