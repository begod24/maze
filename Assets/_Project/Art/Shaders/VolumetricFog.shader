Shader "Hidden/NoWayOut/Volumetric Fog"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #define MAX_FOG_VOLUMES 16

        TEXTURE2D_X_FLOAT(_CameraDepthTexture);
        TEXTURE2D(_FogTexture);

        float4 _FogTarget;
        float4 _FogSource;
        float4 _FogBlur;

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        Varyings Vert(uint vertexID : SV_VertexID)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
            return output;
        }

        float LoadRawDepth(float2 fullPixel)
        {
            uint2 pixel = min(uint2(fullPixel), uint2(_FogSource.xy) - 1);
            return LOAD_TEXTURE2D_X(_CameraDepthTexture, pixel).r;
        }

        float LoadFogDepth(int2 fogPixel)
        {
            float2 fullPixel = (fogPixel + 0.5) * _FogSource.z;
            return LinearEyeDepth(LoadRawDepth(fullPixel), _ZBufferParams);
        }
        ENDHLSL

        Pass
        {
            Name "Raymarch"
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE3D(_FogNoise);
            SAMPLER(sampler_FogNoise);

            float4 _FogParams0;
            float4 _FogParams1;
            float4 _FogAlbedo;
            float4 _FogAmbient;
            float4x4 _FogVolumeMatrices[MAX_FOG_VOLUMES];
            float4 _FogVolumeAlbedo[MAX_FOG_VOLUMES];
            float4 _FogVolumePositiveFade[MAX_FOG_VOLUMES];
            float4 _FogVolumeNegativeFade[MAX_FOG_VOLUMES];
            float4 _FogVolumeNoiseTiling[MAX_FOG_VOLUMES];
            float4 _FogVolumeNoiseOffset[MAX_FOG_VOLUMES];

            float GlobalExtinction(float height)
            {
                return _FogParams0.x * exp(-max(height - _FogParams0.y, 0.0) * _FogParams0.z);
            }

            float SampleExtinction(float3 positionWS, out float3 albedo)
            {
                float globalExtinction = GlobalExtinction(positionWS.y);
                float extinction = globalExtinction;
                float3 scattering = globalExtinction * _FogAlbedo.rgb;
                int volumeCount = (int)_FogAlbedo.w;

                for (int i = 0; i < volumeCount; i++)
                {
                    float3 local = mul(_FogVolumeMatrices[i], float4(positionWS, 1.0)).xyz;
                    if (any(abs(local) > 0.5))
                    {
                        continue;
                    }

                    float3 coord = local + 0.5;
                    float3 positiveFade = saturate((1.0 - coord) * _FogVolumePositiveFade[i].xyz);
                    float3 negativeFade = saturate(coord * _FogVolumeNegativeFade[i].xyz);
                    float density = positiveFade.x * positiveFade.y * positiveFade.z * negativeFade.x * negativeFade.y * negativeFade.z;
                    float3 volumeAlbedo = _FogVolumeAlbedo[i].rgb;

                    if (_FogVolumeNoiseTiling[i].w > 0.5)
                    {
                        float3 uvw = coord * _FogVolumeNoiseTiling[i].xyz + _FogVolumeNoiseOffset[i].xyz;
                        float4 mask = SAMPLE_TEXTURE3D_LOD(_FogNoise, sampler_FogNoise, uvw, 0);
                        density *= mask.a;
                        volumeAlbedo *= mask.rgb;
                    }

                    float volumeExtinction = _FogVolumeAlbedo[i].a * density;
                    extinction += volumeExtinction;
                    scattering += volumeExtinction * volumeAlbedo;
                }

                albedo = scattering / max(extinction, 1e-6);
                return extinction;
            }

            float Phase(float cosTheta)
            {
                float g = _FogParams1.x;
                float g2 = g * g;
                float denominator = 1.0 + g2 - 2.0 * g * cosTheta;
                return 0.25 * (1.0 - g2) / (denominator * sqrt(denominator));
            }

            float SunTransmittance(float height, float3 lightDirection)
            {
                float heightScale = 1.0 / max(_FogParams0.z, 1e-4);
                float above = max(height - _FogParams0.y, 0.0);
                float opticalDepth = _FogParams0.x * (max(_FogParams0.y - height, 0.0) + heightScale * exp(-above * _FogParams0.z));
                return exp(-opticalDepth / max(lightDirection.y, 0.05));
            }

            float3 EvaluateLight(float3 positionWS, float3 direction, float2 screenUV)
            {
                float3 light = _FogAmbient.rgb;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                float mainAttenuation = mainLight.shadowAttenuation * SunTransmittance(positionWS.y, mainLight.direction);
                light += mainLight.color * (mainAttenuation * Phase(dot(direction, mainLight.direction)) * _FogParams1.y);

            #if USE_CLUSTER_LIGHT_LOOP
                half4 shadowMask = half4(1.0, 1.0, 1.0, 1.0);
                float3 additional = 0.0;

                for (uint directionalIndex = 0; directionalIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); directionalIndex++)
                {
                    Light directionalLight = GetAdditionalLight(directionalIndex, positionWS, shadowMask);
                    additional += directionalLight.color * (directionalLight.shadowAttenuation * Phase(dot(direction, directionalLight.direction)));
                }

                uint lightIndex;
                ClusterIterator iterator = ClusterInit(screenUV, positionWS, 0);
                [loop] while (ClusterNext(iterator, lightIndex))
                {
                    lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT;
                    Light pointLight = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                    additional += pointLight.color * (pointLight.distanceAttenuation * pointLight.shadowAttenuation * Phase(dot(direction, pointLight.direction)));
                }

                light += additional * _FogParams1.z;
            #endif

                return light;
            }

            float Distribution(float x)
            {
                return x * x;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 fullPixel = input.positionCS.xy * _FogSource.z;
                float2 uv = fullPixel / _FogSource.xy;
                float2 screenUV = GetNormalizedScreenSpaceUV(fullPixel);
                float rawDepth = LoadRawDepth(fullPixel);

            #if UNITY_REVERSED_Z
                bool isSky = rawDepth <= 1e-7;
                float deviceDepth = rawDepth;
            #else
                bool isSky = rawDepth >= 1.0 - 1e-7;
                float deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
            #endif

                float3 origin = GetCameraPositionWS();
                float3 positionWS = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
                float3 ray = positionWS - origin;
                float sceneDistance = length(ray);
                float3 direction = ray / max(sceneDistance, 1e-5);
                float maxDistance = _FogParams0.w;
                float marchDistance = isSky ? maxDistance : min(sceneDistance, maxDistance);

                int steps = (int)_FogParams1.w;
                float jitter = InterleavedGradientNoise(input.positionCS.xy, (uint)_FogAmbient.w);
                float3 scattered = 0.0;
                float transmittance = 1.0;

                [loop] for (int i = 0; i < steps; i++)
                {
                    float start = Distribution(i / (float)steps) * marchDistance;
                    float end = Distribution((i + 1) / (float)steps) * marchDistance;
                    float sampleDistance = Distribution((i + jitter) / (float)steps) * marchDistance;
                    float3 samplePosition = origin + direction * sampleDistance;

                    float3 albedo;
                    float extinction = SampleExtinction(samplePosition, albedo);
                    if (extinction > 1e-5)
                    {
                        float3 inScattered = extinction * albedo * EvaluateLight(samplePosition, direction, screenUV);
                        float stepTransmittance = exp(-extinction * (end - start));
                        scattered += transmittance * inScattered * (1.0 - stepTransmittance) / extinction;
                        transmittance *= stepTransmittance;
                    }

                    if (transmittance < 0.002)
                    {
                        break;
                    }
                }

                float tail = isSky ? 1e5 : max(sceneDistance - marchDistance, 0.0);
                float3 tailPosition = origin + direction * marchDistance;
                float tailExtinction = GlobalExtinction(tailPosition.y);
                float tailTransmittance = exp(-tailExtinction * tail);
                scattered += transmittance * _FogAlbedo.rgb * _FogAmbient.rgb * (1.0 - tailTransmittance);
                transmittance *= tailTransmittance;

                return float4(scattered, transmittance);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Blur"
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            static const int BLUR_RADIUS = 3;
            static const float BLUR_WEIGHTS[4] = { 0.2270270, 0.1945946, 0.1216216, 0.0540541 };

            float4 Frag(Varyings input) : SV_Target
            {
                int2 center = int2(input.positionCS.xy);
                int2 maxPixel = int2(_FogTarget.xy) - 1;
                int2 step = int2(_FogBlur.xy);
                float centerDepth = LoadFogDepth(center);
                float4 sum = LOAD_TEXTURE2D(_FogTexture, center) * BLUR_WEIGHTS[0];
                float weightSum = BLUR_WEIGHTS[0];

                [unroll] for (int i = 1; i <= BLUR_RADIUS; i++)
                {
                    [unroll] for (int side = -1; side <= 1; side += 2)
                    {
                        int2 pixel = clamp(center + step * (i * side), int2(0, 0), maxPixel);
                        float depth = LoadFogDepth(pixel);
                        float weight = BLUR_WEIGHTS[i] * exp(-abs(depth - centerDepth) * _FogSource.w / max(centerDepth, 1e-3));
                        sum += LOAD_TEXTURE2D(_FogTexture, pixel) * weight;
                        weightSum += weight;
                    }
                }

                return sum / weightSum;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            Blend One SrcAlpha, Zero One

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float depth = LinearEyeDepth(LoadRawDepth(input.positionCS.xy), _ZBufferParams);
                float2 fogPosition = input.positionCS.xy / _FogSource.z - 0.5;
                int2 basePixel = int2(floor(fogPosition));
                float2 fraction = fogPosition - basePixel;
                int2 maxPixel = int2(_FogTarget.xy) - 1;

                float4 sum = 0.0;
                float weightSum = 0.0;
                float4 nearest = 0.0;
                float nearestDifference = 1e20;

                [unroll] for (int y = 0; y <= 1; y++)
                {
                    [unroll] for (int x = 0; x <= 1; x++)
                    {
                        int2 pixel = clamp(basePixel + int2(x, y), int2(0, 0), maxPixel);
                        float4 fog = LOAD_TEXTURE2D(_FogTexture, pixel);
                        float difference = abs(LoadFogDepth(pixel) - depth);
                        float bilinear = (x == 0 ? 1.0 - fraction.x : fraction.x) * (y == 0 ? 1.0 - fraction.y : fraction.y);
                        float weight = bilinear * exp(-difference * _FogSource.w / max(depth, 1e-3));
                        sum += fog * weight;
                        weightSum += weight;

                        if (difference < nearestDifference)
                        {
                            nearestDifference = difference;
                            nearest = fog;
                        }
                    }
                }

                return weightSum > 1e-4 ? sum / weightSum : nearest;
            }
            ENDHLSL
        }
    }
}
