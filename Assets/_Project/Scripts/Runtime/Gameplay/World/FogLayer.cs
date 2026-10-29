using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NoWayOut.Gameplay.World
{
    internal readonly struct FogLayer
    {
        public LocalVolumetricFog Volume { get; }
        public float BaseMeanFreePath { get; }
        public Vector3 BaseScroll { get; }
        public float PatchSize { get; }
        public float BreathAmplitude { get; }
        public float BreathPeriod { get; }
        public float BreathSeed { get; }


        public FogLayer(LocalVolumetricFog volume, float patchSize, float breathAmplitude, float breathPeriod, float breathSeed)
        {
            Volume = volume;
            BaseMeanFreePath = volume.parameters.meanFreePath;
            BaseScroll = volume.parameters.textureScrollingSpeed;
            PatchSize = patchSize;
            BreathAmplitude = breathAmplitude;
            BreathPeriod = breathPeriod;
            BreathSeed = breathSeed;
        }
    }
}
