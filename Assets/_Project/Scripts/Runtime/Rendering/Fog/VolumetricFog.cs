using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoWayOut.Rendering.Fog
{
    [Serializable]
    [VolumeComponentMenu("No Way Out/Volumetric Fog")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class VolumetricFog : VolumeComponent
    {
        public BoolParameter enabled = new BoolParameter(false);
        public MinFloatParameter meanFreePath = new MinFloatParameter(7f, 0.1f);
        public FloatParameter baseHeight = new FloatParameter(24f);
        public FloatParameter maximumHeight = new FloatParameter(64f);
        public ColorParameter albedo = new ColorParameter(new Color(0.62f, 0.67f, 0.76f), false, false, true);
        public ColorParameter ambientLight = new ColorParameter(new Color(0.1f, 0.11f, 0.13f), true, false, true);
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.7f, -0.95f, 0.95f);
        public MinFloatParameter mainLightScattering = new MinFloatParameter(1f, 0f);
        public MinFloatParameter additionalLightsScattering = new MinFloatParameter(1f, 0f);
        public MinFloatParameter maxDistance = new MinFloatParameter(40f, 1f);


        public bool IsActive()
        {
            return enabled.value;
        }
    }
}
