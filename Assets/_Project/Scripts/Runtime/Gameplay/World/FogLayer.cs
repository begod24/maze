using NoWayOut.Rendering.Fog;
using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    internal readonly struct FogLayer
    {
        public LocalFogVolume Volume { get; }
        public float BaseMeanFreePath { get; }
        public Vector3 BaseScroll { get; }
        public float PatchSize { get; }
        public float BreathAmplitude { get; }
        public float BreathPeriod { get; }
        public float BreathSeed { get; }


        public FogLayer(LocalFogVolume volume, float patchSize, float breathAmplitude, float breathPeriod, float breathSeed)
        {
            Volume = volume;
            BaseMeanFreePath = volume.MeanFreePath;
            BaseScroll = volume.NoiseScrollSpeed;
            PatchSize = patchSize;
            BreathAmplitude = breathAmplitude;
            BreathPeriod = breathPeriod;
            BreathSeed = breathSeed;
        }
    }
}
