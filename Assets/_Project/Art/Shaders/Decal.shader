Shader "NoWayOut/Decal"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _MaskMap ("Mask Map (R metallic, G occlusion, A smoothness)", 2D) = "white" {}
        _MetallicRemapMin ("Metallic Min", Range(0, 1)) = 0
        _MetallicRemapMax ("Metallic Max", Range(0, 1)) = 1
        _SmoothnessRemapMin ("Smoothness Min", Range(0, 1)) = 0
        _SmoothnessRemapMax ("Smoothness Max", Range(0, 1)) = 1
        _AORemapMin ("Occlusion Min", Range(0, 1)) = 0
        _AORemapMax ("Occlusion Max", Range(0, 1)) = 1
        _FlatNormalOpacity ("Flat Normal Opacity", Range(0, 1)) = 0
        [Enum(Off, 0, On, 15)] _DecalColorMask0 ("Affect Albedo", Float) = 15
        [Enum(Off, 0, On, 15)] _DecalColorMask1 ("Affect Normal", Float) = 0
        [Enum(Off, 0, On, 15)] _DecalColorMask2 ("Affect Metallic, Occlusion, Smoothness", Float) = 15
        _DrawOrder ("Draw Order", Range(-50, 50)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Plane" }

        Pass
        {
            Name "DBufferProjector"
            Tags { "LightMode" = "DBufferProjector" }

            Cull Front
            ZTest Greater
            ZWrite Off
            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            ColorMask [_DecalColorMask0]
            ColorMask [_DecalColorMask1] 1
            ColorMask [_DecalColorMask2] 2

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma shader_feature_local_fragment _MASKMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _MetallicRemapMin;
                half _MetallicRemapMax;
                half _SmoothnessRemapMin;
                half _SmoothnessRemapMax;
                half _AORemapMin;
                half _AORemapMax;
                half _FlatNormalOpacity;
                float _DecalColorMask0;
                float _DecalColorMask1;
                float _DecalColorMask2;
                float _DrawOrder;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Decal)
                UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
                UNITY_DEFINE_INSTANCED_PROP(float, _DecalLayerMaskFromDecal)
            UNITY_INSTANCING_BUFFER_END(Decal)

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            void Frag(Varyings input, OUTPUT_DBUFFER(outDBuffer))
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            #if UNITY_REVERSED_Z
                float depth = LoadSceneDepth(input.positionCS.xy);
            #else
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, LoadSceneDepth(input.positionCS.xy));
            #endif

                float2 positionSS = input.positionCS.xy * _ScreenSize.zw;
                float3 positionWS = ComputeWorldSpacePosition(positionSS, depth, UNITY_MATRIX_I_VP);
                float3 positionDS = TransformWorldToObject(positionWS) * float3(1.0, -1.0, 1.0);
                clip(0.5 - Max3(abs(positionDS.x), abs(positionDS.y), abs(positionDS.z)));

                float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                half fadeFactor = saturate(normalToWorld[0][3]);
                float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
                float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
                float2 uv = (positionDS.xz + 0.5) * scale + offset;

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half opacity = baseColor.a * fadeFactor;

            #if defined(_MASKMAP)
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
            #else
                half4 mask = half4(0.0, 1.0, 0.0, 1.0);
            #endif

                DecalSurfaceData surfaceData = (DecalSurfaceData)0;
                surfaceData.baseColor = half4(baseColor.rgb, opacity);
                surfaceData.normalWS = half4(normalize(normalToWorld[2].xyz), opacity * _FlatNormalOpacity);
                surfaceData.metallic = lerp(_MetallicRemapMin, _MetallicRemapMax, mask.r);
                surfaceData.occlusion = lerp(_AORemapMin, _AORemapMax, mask.g);
                surfaceData.smoothness = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, mask.a);
                surfaceData.MAOSAlpha = opacity;

                ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
            }
            ENDHLSL
        }
    }
}
