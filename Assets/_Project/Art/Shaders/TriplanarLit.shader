Shader "NoWayOut/Triplanar Lit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _MaskMap ("Mask Map (R metallic, G occlusion, A smoothness)", 2D) = "white" {}
        _MetallicRemapMin ("Metallic Min", Range(0, 1)) = 0
        _MetallicRemapMax ("Metallic Max", Range(0, 1)) = 0
        _SmoothnessRemapMin ("Smoothness Min", Range(0, 1)) = 0
        _SmoothnessRemapMax ("Smoothness Max", Range(0, 1)) = 0.5
        _AORemapMin ("Occlusion Min", Range(0, 1)) = 0
        _AORemapMax ("Occlusion Max", Range(0, 1)) = 1
        _TexWorldScale ("World Scale", Float) = 0.25
        _BlendSharpness ("Blend Sharpness", Range(1, 16)) = 4
        [HideInInspector] _Cutoff ("Alpha Cutoff", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _MASKMAP

            #include "Includes/TriplanarLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Includes/FogCommon.hlsl"

            half4 FragmentPBRInFog(InputData inputData, SurfaceData surfaceData)
            {
                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);
                BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
                uint meshRenderingLayers = GetMeshRenderingLayer();

                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                mainLight.distanceAttenuation *= NoWayOutSunTransmittance(inputData.positionWS.y, mainLight.direction);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

                LightingData lightingData = CreateLightingData(inputData, surfaceData);
                lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                    inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                    inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                {
                    lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat, mainLight,
                        inputData.normalWS, inputData.viewDirectionWS, surfaceData.clearCoatMask, false);
                }

            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();

            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                    {
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                            inputData.normalWS, inputData.viewDirectionWS, surfaceData.clearCoatMask, false);
                    }
                }
            #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                    {
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                            inputData.normalWS, inputData.viewDirectionWS, surfaceData.clearCoatMask, false);
                    }
                LIGHT_LOOP_END
            #endif

                return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 geometricNormal = SafeNormalize(input.normalWS);
                TriplanarCoordinates coordinates = GetTriplanarCoordinates(input.positionWS, geometricNormal);
                half4 albedo = SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), coordinates);

            #if defined(_NORMALMAP)
                half3 normalWS = SampleTriplanarNormal(coordinates, geometricNormal);
            #else
                half3 normalWS = geometricNormal;
            #endif

            #if defined(_MASKMAP)
                half4 mask = SampleTriplanar(TEXTURE2D_ARGS(_MaskMap, sampler_MaskMap), coordinates);
            #else
                half4 mask = half4(0.0, 1.0, 0.0, 1.0);
            #endif

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo.rgb * _BaseColor.rgb;
                surface.alpha = 1.0;
                surface.metallic = lerp(_MetallicRemapMin, _MetallicRemapMax, mask.r);
                surface.occlusion = lerp(_AORemapMin, _AORemapMax, mask.g);
                surface.smoothness = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, mask.a);
                surface.normalTS = half3(0.0, 0.0, 1.0);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.shadowMask = half4(1.0, 1.0, 1.0, 1.0);

            #if defined(_DBUFFER)
                ApplyDecalToSurfaceData(input.positionCS, surface, inputData);
            #endif

                return FragmentPBRInFog(inputData, surface);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Includes/TriplanarLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Includes/TriplanarLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #include "Includes/TriplanarLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
