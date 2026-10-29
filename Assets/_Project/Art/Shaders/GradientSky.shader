Shader "NoWayOut/Gradient Sky"
{
    Properties
    {
        [HDR] _TopColor ("Top", Color) = (0.32, 0.36, 0.42, 1)
        [HDR] _MiddleColor ("Middle", Color) = (0.16, 0.18, 0.21, 1)
        [HDR] _BottomColor ("Bottom", Color) = (0.02, 0.022, 0.026, 1)
        _GradientDiffusion ("Gradient Diffusion", Float) = 1
        _Intensity ("Intensity", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _MiddleColor;
                half4 _BottomColor;
                float _GradientDiffusion;
                float _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float height = normalize(input.direction).y * _GradientDiffusion;
                half3 color = lerp(_MiddleColor.rgb, _TopColor.rgb, saturate(height));
                color = lerp(color, _BottomColor.rgb, saturate(-height));
                return half4(color * _Intensity, 1.0);
            }
            ENDHLSL
        }
    }
}
