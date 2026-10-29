using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NoWayOut.Rendering.Fog
{
    internal sealed class VolumetricFogPass : ScriptableRenderPass
    {
        private const int MAX_VOLUMES = 16;
        private const int RAYMARCH_PASS = 0;
        private const int BLUR_PASS = 1;
        private const int COMPOSITE_PASS = 2;
        private const int MAX_BLUR_PASSES = 4;
        private const int FRAME_PERIOD = 64;
        private const float HEIGHT_SCALE_FROM_LAYER_DEPTH = 0.144765f;
        private const float MIN_LAYER_DEPTH = 0.01f;
        private const float MIN_FADE = 0.0001f;
        private const float HALF = 0.5f;
        private const string RAYMARCH_NAME = "Volumetric Fog Raymarch";
        private const string BLUR_NAME = "Volumetric Fog Blur";
        private const string COMPOSITE_NAME = "Volumetric Fog Composite";
        private const string FOG_TEXTURE_NAME = "_VolumetricFog";
        private const string BLUR_TEXTURE_NAME = "_VolumetricFogBlur";

        private static readonly int FogParams0Id = Shader.PropertyToID("_FogParams0");
        private static readonly int FogParams1Id = Shader.PropertyToID("_FogParams1");
        private static readonly int FogAlbedoId = Shader.PropertyToID("_FogAlbedo");
        private static readonly int FogAmbientId = Shader.PropertyToID("_FogAmbient");
        private static readonly int FogTargetId = Shader.PropertyToID("_FogTarget");
        private static readonly int FogSourceId = Shader.PropertyToID("_FogSource");
        private static readonly int FogBlurId = Shader.PropertyToID("_FogBlur");
        private static readonly int FogNoiseId = Shader.PropertyToID("_FogNoise");
        private static readonly int FogTextureId = Shader.PropertyToID("_FogTexture");
        private static readonly int CameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        private static readonly int VolumeMatricesId = Shader.PropertyToID("_FogVolumeMatrices");
        private static readonly int VolumeAlbedoId = Shader.PropertyToID("_FogVolumeAlbedo");
        private static readonly int VolumePositiveFadeId = Shader.PropertyToID("_FogVolumePositiveFade");
        private static readonly int VolumeNegativeFadeId = Shader.PropertyToID("_FogVolumeNegativeFade");
        private static readonly int VolumeNoiseTilingId = Shader.PropertyToID("_FogVolumeNoiseTiling");
        private static readonly int VolumeNoiseOffsetId = Shader.PropertyToID("_FogVolumeNoiseOffset");
        private static readonly int GlobalFogParamsId = Shader.PropertyToID("_NoWayOutFogParams");
        private static readonly int GlobalFogColorId = Shader.PropertyToID("_NoWayOutFogColor");

        private readonly Material material;
        private readonly VolumetricFogSettings settings;
        private readonly MaterialPropertyBlock raymarchProperties = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock compositeProperties = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock[] blurProperties = new MaterialPropertyBlock[MAX_BLUR_PASSES * 2];
        private readonly Matrix4x4[] volumeMatrices = new Matrix4x4[MAX_VOLUMES];
        private readonly Vector4[] volumeAlbedo = new Vector4[MAX_VOLUMES];
        private readonly Vector4[] volumePositiveFade = new Vector4[MAX_VOLUMES];
        private readonly Vector4[] volumeNegativeFade = new Vector4[MAX_VOLUMES];
        private readonly Vector4[] volumeNoiseTiling = new Vector4[MAX_VOLUMES];
        private readonly Vector4[] volumeNoiseOffset = new Vector4[MAX_VOLUMES];


        public VolumetricFogPass(Material material, VolumetricFogSettings settings)
        {
            this.material = material;
            this.settings = settings;
            renderPassEvent = settings.renderPassEvent;
            ConfigureInput(ScriptableRenderPassInput.Depth);

            for (int i = 0; i < blurProperties.Length; i++)
            {
                blurProperties[i] = new MaterialPropertyBlock();
            }
        }


        public static void SetGlobalFog(VolumetricFog fog, bool active)
        {
            if (!active)
            {
                Shader.SetGlobalVector(GlobalFogParamsId, Vector4.zero);
                return;
            }

            Shader.SetGlobalVector(GlobalFogParamsId, GetGlobalFogParams(fog));
            Color albedo = fog.albedo.value;
            Color ambient = fog.ambientLight.value;
            Shader.SetGlobalVector(GlobalFogColorId, new Vector4(albedo.r * ambient.r, albedo.g * ambient.g, albedo.b * ambient.b, 0f));
        }


        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType != CameraType.Game && cameraData.cameraType != CameraType.SceneView)
            {
                return;
            }

            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            TextureHandle depth = resources.cameraDepthTexture;
            if (resources.isActiveTargetBackBuffer || !depth.IsValid())
            {
                return;
            }

            VolumetricFog fog = VolumeManager.instance.stack.GetComponent<VolumetricFog>();
            RenderTextureDescriptor cameraDescriptor = cameraData.cameraTargetDescriptor;
            int downsample = Mathf.Max(1, settings.downsample);
            int width = Mathf.Max(1, cameraDescriptor.width / downsample);
            int height = Mathf.Max(1, cameraDescriptor.height / downsample);
            Vector4 target = new Vector4(width, height, 1f / width, 1f / height);
            Vector4 source = new Vector4(cameraDescriptor.width, cameraDescriptor.height, downsample, settings.blurDepthSharpness);

            TextureHandle fogTexture = CreateFogTexture(renderGraph, width, height, FOG_TEXTURE_NAME);
            FillRaymarchProperties(fog, cameraData.camera, target, source);
            AddFullscreenPass(renderGraph, RAYMARCH_NAME, TextureHandle.nullHandle, depth, fogTexture, AccessFlags.Write, RAYMARCH_PASS, raymarchProperties, true);

            int blurPasses = Mathf.Min(settings.blurPasses, MAX_BLUR_PASSES);
            if (blurPasses > 0)
            {
                TextureHandle blurTexture = CreateFogTexture(renderGraph, width, height, BLUR_TEXTURE_NAME);
                for (int i = 0; i < blurPasses; i++)
                {
                    MaterialPropertyBlock horizontal = blurProperties[i * 2];
                    MaterialPropertyBlock vertical = blurProperties[i * 2 + 1];
                    FillBlurProperties(horizontal, target, source, Vector2.right);
                    FillBlurProperties(vertical, target, source, Vector2.up);
                    AddFullscreenPass(renderGraph, BLUR_NAME, fogTexture, depth, blurTexture, AccessFlags.Write, BLUR_PASS, horizontal, false);
                    AddFullscreenPass(renderGraph, BLUR_NAME, blurTexture, depth, fogTexture, AccessFlags.Write, BLUR_PASS, vertical, false);
                }
            }

            FillBlurProperties(compositeProperties, target, source, Vector2.zero);
            AddFullscreenPass(renderGraph, COMPOSITE_NAME, fogTexture, depth, resources.activeColorTexture, AccessFlags.ReadWrite, COMPOSITE_PASS, compositeProperties, false);
        }


        private static Vector4 GetGlobalFogParams(VolumetricFog fog)
        {
            float layerDepth = Mathf.Max(fog.maximumHeight.value - fog.baseHeight.value, MIN_LAYER_DEPTH);
            float heightScale = layerDepth * HEIGHT_SCALE_FROM_LAYER_DEPTH;
            return new Vector4(1f / fog.meanFreePath.value, fog.baseHeight.value, 1f / heightScale, fog.maxDistance.value);
        }


        private static TextureHandle CreateFogTexture(RenderGraph renderGraph, int width, int height, string name)
        {
            TextureDesc descriptor = new TextureDesc(width, height);
            descriptor.format = GraphicsFormat.R16G16B16A16_SFloat;
            descriptor.filterMode = FilterMode.Bilinear;
            descriptor.wrapMode = TextureWrapMode.Clamp;
            descriptor.clearBuffer = false;
            descriptor.name = name;
            return renderGraph.CreateTexture(descriptor);
        }


        private void AddFullscreenPass(RenderGraph renderGraph, string name, TextureHandle source, TextureHandle depth, TextureHandle target, AccessFlags targetAccess, int shaderPass, MaterialPropertyBlock properties, bool readGlobals)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(name, out FogPassData data))
            {
                data.material = material;
                data.properties = properties;
                data.source = source;
                data.depth = depth;
                data.shaderPass = shaderPass;

                if (source.IsValid())
                {
                    builder.UseTexture(source);
                }

                builder.UseTexture(depth);
                builder.SetRenderAttachment(target, 0, targetAccess);
                builder.UseAllGlobalTextures(readGlobals);
                builder.SetRenderFunc((FogPassData passData, RasterGraphContext context) => ExecutePass(passData, context));
            }
        }


        private static void ExecutePass(FogPassData data, RasterGraphContext context)
        {
            if (data.source.IsValid())
            {
                data.properties.SetTexture(FogTextureId, (RTHandle)data.source);
            }

            data.properties.SetTexture(CameraDepthTextureId, (RTHandle)data.depth);
            context.cmd.DrawProcedural(Matrix4x4.identity, data.material, data.shaderPass, MeshTopology.Triangles, 3, 1, data.properties);
        }


        private void FillRaymarchProperties(VolumetricFog fog, Camera camera, Vector4 target, Vector4 source)
        {
            Color albedo = fog.albedo.value;
            Color ambient = fog.ambientLight.value;
            int volumeCount = CollectVolumes(camera.transform.position, fog.maxDistance.value);
            float frame = Time.frameCount % FRAME_PERIOD;

            raymarchProperties.SetVector(FogParams0Id, GetGlobalFogParams(fog));
            raymarchProperties.SetVector(FogParams1Id, new Vector4(fog.anisotropy.value, fog.mainLightScattering.value, fog.additionalLightsScattering.value, settings.stepCount));
            raymarchProperties.SetVector(FogAlbedoId, new Vector4(albedo.r, albedo.g, albedo.b, volumeCount));
            raymarchProperties.SetVector(FogAmbientId, new Vector4(ambient.r, ambient.g, ambient.b, frame));
            raymarchProperties.SetVector(FogTargetId, target);
            raymarchProperties.SetVector(FogSourceId, source);
            raymarchProperties.SetMatrixArray(VolumeMatricesId, volumeMatrices);
            raymarchProperties.SetVectorArray(VolumeAlbedoId, volumeAlbedo);
            raymarchProperties.SetVectorArray(VolumePositiveFadeId, volumePositiveFade);
            raymarchProperties.SetVectorArray(VolumeNegativeFadeId, volumeNegativeFade);
            raymarchProperties.SetVectorArray(VolumeNoiseTilingId, volumeNoiseTiling);
            raymarchProperties.SetVectorArray(VolumeNoiseOffsetId, volumeNoiseOffset);

            if (settings.noise != null)
            {
                raymarchProperties.SetTexture(FogNoiseId, settings.noise);
            }
        }


        private static void FillBlurProperties(MaterialPropertyBlock properties, Vector4 target, Vector4 source, Vector2 direction)
        {
            properties.SetVector(FogTargetId, target);
            properties.SetVector(FogSourceId, source);
            properties.SetVector(FogBlurId, new Vector4(direction.x, direction.y, 0f, 0f));
        }


        private int CollectVolumes(Vector3 cameraPosition, float maxDistance)
        {
            FogVolumeSet set = settings.volumes;
            if (set == null)
            {
                return 0;
            }

            float noiseEnabled = settings.noise != null ? 1f : 0f;
            int count = 0;
            for (int i = 0; i < set.Count && count < MAX_VOLUMES; i++)
            {
                LocalFogVolume volume = set.Get(i);
                if (volume == null || !volume.isActiveAndEnabled || volume.MeanFreePath <= 0f)
                {
                    continue;
                }

                Transform volumeTransform = volume.transform;
                Vector3 size = volume.Size;
                float reach = size.magnitude * HALF + maxDistance;
                if ((volumeTransform.position - cameraPosition).sqrMagnitude > reach * reach)
                {
                    continue;
                }

                Color albedo = volume.Albedo;
                Vector3 offset = volume.NoiseOffset;
                volumeMatrices[count] = Matrix4x4.TRS(volumeTransform.position, volumeTransform.rotation, size).inverse;
                volumeAlbedo[count] = new Vector4(albedo.r, albedo.g, albedo.b, 1f / volume.MeanFreePath);
                volumePositiveFade[count] = Reciprocal(volume.PositiveFade);
                volumeNegativeFade[count] = Reciprocal(volume.NegativeFade);
                volumeNoiseTiling[count] = new Vector4(volume.NoiseTiling.x, volume.NoiseTiling.y, volume.NoiseTiling.z, noiseEnabled);
                volumeNoiseOffset[count] = new Vector4(offset.x, offset.y, offset.z, 0f);
                count++;
            }

            return count;
        }


        private static Vector4 Reciprocal(Vector3 fade)
        {
            return new Vector4(1f / Mathf.Max(fade.x, MIN_FADE), 1f / Mathf.Max(fade.y, MIN_FADE), 1f / Mathf.Max(fade.z, MIN_FADE), 0f);
        }
    }
}
