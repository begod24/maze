using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace NoWayOut.Rendering.Fog
{
    internal sealed class FogPassData
    {
        public Material material;
        public MaterialPropertyBlock properties;
        public TextureHandle source;
        public TextureHandle depth;
        public int shaderPass;
    }
}
