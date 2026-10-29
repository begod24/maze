using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NoWayOut.Rendering.Fog
{
    [Serializable]
    public sealed class VolumetricFogSettings
    {
        public Shader shader;
        public FogVolumeSet volumes;
        public Texture3D noise;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        [Range(1, 4)] public int downsample = 2;
        [Range(8, 128)] public int stepCount = 48;
        [Range(0, 4)] public int blurPasses = 1;
        public float blurDepthSharpness = 8f;
    }
}
