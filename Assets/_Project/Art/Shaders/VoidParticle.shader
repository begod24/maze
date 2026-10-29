Shader "NoWayOut/Void Particle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 1
        _SoftDistance ("Soft Distance", Float) = 1
        _NearFade ("Near Fade", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float _Intensity;
                float _SoftDistance;
                float _NearFade;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionRWS : TEXCOORD0;
                float4 color : COLOR;
                float2 uv : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionRWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionRWS);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, input.positionCS.z, input.positionCS.w, input.positionRWS);
                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float sceneDepth = LinearEyeDepth(LoadCameraDepth(posInput.positionSS), _ZBufferParams);
                float softFade = saturate((sceneDepth - posInput.linearDepth) / max(_SoftDistance, 0.001));
                float nearFade = saturate((posInput.linearDepth - _ProjectionParams.y) / max(_NearFade, 0.001));
                float alpha = texel.a * input.color.a * _Tint.a * softFade * nearFade;

                float3 color = texel.rgb * input.color.rgb * _Tint.rgb * _Intensity * GetCurrentExposureMultiplier();
                float4 result = float4(color * alpha, alpha);

                float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
                float3 fogColor;
                float3 fogOpacity;
                EvaluateAtmosphericScattering(posInput, V, fogColor, fogOpacity);
                result.rgb = result.rgb * (1.0 - fogOpacity) + fogColor * alpha;
                return result;
            }
            ENDHLSL
        }
    }
}
