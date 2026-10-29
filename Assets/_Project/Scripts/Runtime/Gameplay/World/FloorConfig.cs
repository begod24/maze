using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/Floor Config", fileName = "FloorConfig")]
    public sealed class FloorConfig : ScriptableObject
    {
        public float cellSize = 4f;
        public int seed = 0;
        public float loopStartChance = 0.5f;
        public int upperLaneStepsMin = 140;
        public int upperLaneStepsMax = 160;
        public int lowerLaneStepsMin = 90;
        public int lowerLaneStepsMax = 110;
        public int laneHalfWidth = 8;
        public int loopRowsMin = 2;
        public int loopRowsMax = 4;
        public int separationRowsMin = 4;
        public int separationRowsMax = 6;
        public int minLevel = -5;
        public int maxLevel = 5;
        public float stairChance = 0.2f;
        public float turnChance = 0.18f;
        public int minStepsBetweenTurns = 3;
        public int sideStepsMin = 2;
        public int sideStepsMax = 5;
        public float forkChance = 0.05f;
        public int deadEndStepsMin = 2;
        public int deadEndStepsMax = 5;
        public float deadEndDoorwayChance = 0.35f;
        public float doorwayChance = 0.05f;
        public int maxDoorwaysPerLane = 4;
        public int maxAttempts = 64;
    }
}
