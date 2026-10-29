using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoWayOut.Rendering.Fog
{
    [DisallowMultipleRendererFeature("Volumetric Fog")]
    public sealed class VolumetricFogFeature : ScriptableRendererFeature
    {
        [SerializeField] private VolumetricFogSettings settings = new VolumetricFogSettings();

        private Material material;
        private VolumetricFogPass pass;


        public override void Create()
        {
            if (settings.shader == null)
            {
                return;
            }

            material = CoreUtils.CreateEngineMaterial(settings.shader);
            pass = new VolumetricFogPass(material, settings);
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            VolumetricFog fog = VolumeManager.instance.stack.GetComponent<VolumetricFog>();
            bool active = pass != null && fog != null && fog.IsActive();
            VolumetricFogPass.SetGlobalFog(fog, active);

            if (active)
            {
                renderer.EnqueuePass(pass);
            }
        }


        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }
    }
}
