using NoWayOut.Rendering.Fog;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NoWayOut.Gameplay.World
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/Floor Atmosphere Config", fileName = "FloorAtmosphereConfig")]
    public sealed class FloorAtmosphereConfig : ScriptableObject
    {
        public LocalFogVolume mistPrefab;
        public LocalFogVolume driftFogPrefab;
        public LocalFogVolume wispFogPrefab;
        public VoidDrift blockPrefab;
        public DecalProjector puddlePrefab;
        public float horizontalMargin = 24f;
        public float mistDepth = 16f;
        public float mistTopOffset = 2f;
        public float mistPatchSize = 12f;
        public float mistBreathAmplitude = 0.3f;
        public float mistBreathPeriod = 50f;
        public float driftFogHeadroom = 12f;
        public float driftFogPatchSize = 20f;
        public float driftFogBreathAmplitude = 0.45f;
        public float driftFogBreathPeriod = 40f;
        public float wispFogPatchSize = 7f;
        public float wispFogBreathAmplitude = 0.3f;
        public float wispFogBreathPeriod = 17f;
        public float blocksPerThousandSquareMeters = 9f;
        public int blockCountMax = 400;
        public int blockAttemptsPerBlock = 8;
        public int blockSeedOffset = 7919;
        public float blockShellMargin = 32f;
        public float blockVerticalMargin = 24f;
        public float blockClearance = 3f;
        public float blockSizeMin = 1f;
        public float blockSizeMax = 6f;
        public float blockSlabChance = 0.35f;
        public float blockSlabThickness = 0.2f;
        public float blockTiltMax = 25f;
        public float driftAmplitudeMin = 0.2f;
        public float driftAmplitudeMax = 1.2f;
        public float driftPeriodMin = 10f;
        public float driftPeriodMax = 24f;
        public float spinSpeedMax = 2f;
        public int puddleSeedOffset = 104729;
        public float puddleChancePerCell = 0.4f;
        public float puddleChanceOnBridge = 0.12f;
        public int puddleAtlasCells = 2;
        public float puddleSizeMin = 1.2f;
        public float puddleSizeMax = 3f;
        public float puddleAspectMin = 0.5f;
        public float puddleOffsetMax = 1.2f;
        public float puddleHeight = 0.3f;
        public float puddleDepth = 0.6f;
    }
}
