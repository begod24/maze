Shader "NoWayOut/CRT Screen"
{
    Properties
    {
        _SourceTex ("Source", 2D) = "black" {}
        _Distortion ("Distortion", Range(0, 1)) = 0
        _ScreenTime ("Screen Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _Noise ("Noise", Range(0, 1)) = 0.12
        _ScanlineCount ("Scanline Count", Float) = 240
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.35
        _Curvature ("Curvature", Range(0, 0.5)) = 0.12
        _Vignette ("Vignette", Range(0, 2)) = 0.9
        _ChromaticShift ("Chromatic Shift", Range(0, 0.02)) = 0.0025
        _Brightness ("Brightness", Float) = 1.1
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            Name "CRTScreen"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment Fragment
            #pragma target 3.0

            sampler2D _SourceTex;
            float _Distortion;
            float _ScreenTime;
            float _Seed;
            float _Noise;
            float _ScanlineCount;
            float _ScanlineStrength;
            float _Curvature;
            float _Vignette;
            float _ChromaticShift;
            float _Brightness;

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Curve(float2 uv)
            {
                float2 centered = uv * 2.0 - 1.0;
                centered *= 1.0 + _Curvature * dot(centered.yx, centered.yx);
                return centered * 0.5 + 0.5;
            }

            float4 Fragment(v2f_customrendertexture input) : SV_Target
            {
                float time = _ScreenTime + _Seed * 17.0;
                float2 uv = Curve(input.globalTexcoord.xy);

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return float4(0.0, 0.0, 0.0, 1.0);
                }

                float strong = _Distortion * _Distortion;
                float frame = floor(time * 24.0);
                float row = floor(uv.y * _ScanlineCount);
                float jitter = (Hash(float2(row, frame)) - 0.5) * strong * 0.12;
                float wave = sin(uv.y * 30.0 + time * 8.0) * 0.006 * _Distortion;
                float tear = step(0.985 - _Distortion * 0.1, Hash(float2(floor(uv.y * 24.0), frame))) * _Distortion * 0.06;
                float rollOffset = strong * frac(time * 0.7) * 0.35;
                float2 sampleUv = float2(uv.x + jitter + wave + tear, frac(uv.y - rollOffset));

                float shift = _ChromaticShift + _Distortion * 0.012;
                float3 color;
                color.r = tex2D(_SourceTex, sampleUv + float2(shift, 0.0)).r;
                color.g = tex2D(_SourceTex, sampleUv).g;
                color.b = tex2D(_SourceTex, sampleUv - float2(shift, 0.0)).b;

                float noise = Hash(uv * 437.0 + frame);
                color = lerp(color, noise.xxx, saturate(_Noise + _Distortion * 0.55));

                float scan = 0.5 + 0.5 * sin(uv.y * _ScanlineCount * 6.2831853);
                color *= 1.0 - _ScanlineStrength * scan;

                float band = frac(uv.y - time * 0.12);
                color *= 1.0 + 0.08 * smoothstep(0.0, 0.08, band) * smoothstep(0.16, 0.08, band);

                float flicker = 0.96 + 0.04 * Hash(float2(frame, _Seed));
                float2 centered = uv * 2.0 - 1.0;
                float vignette = saturate(1.0 - dot(centered, centered) * 0.5 * _Vignette);

                return float4(color * flicker * vignette * _Brightness, 1.0);
            }
            ENDCG
        }
    }
}
