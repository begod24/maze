#ifndef NO_WAY_OUT_TRIPLANAR_LIT_INPUT_INCLUDED
#define NO_WAY_OUT_TRIPLANAR_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    half _BumpScale;
    half _MetallicRemapMin;
    half _MetallicRemapMax;
    half _SmoothnessRemapMin;
    half _SmoothnessRemapMax;
    half _AORemapMin;
    half _AORemapMax;
    float _TexWorldScale;
    half _BlendSharpness;
CBUFFER_END

struct TriplanarCoordinates
{
    float2 uvX;
    float2 uvY;
    float2 uvZ;
    half3 weights;
};

TriplanarCoordinates GetTriplanarCoordinates(float3 positionWS, half3 normalWS)
{
    TriplanarCoordinates coordinates;
    float3 position = positionWS * _TexWorldScale;
    coordinates.uvX = position.zy;
    coordinates.uvY = position.xz;
    coordinates.uvZ = position.xy;

    half3 weights = pow(abs(normalWS), _BlendSharpness);
    coordinates.weights = weights / max(weights.x + weights.y + weights.z, HALF_MIN);
    return coordinates;
}

half4 SampleTriplanar(TEXTURE2D_PARAM(textureMap, samplerMap), TriplanarCoordinates coordinates)
{
    half4 x = SAMPLE_TEXTURE2D(textureMap, samplerMap, coordinates.uvX);
    half4 y = SAMPLE_TEXTURE2D(textureMap, samplerMap, coordinates.uvY);
    half4 z = SAMPLE_TEXTURE2D(textureMap, samplerMap, coordinates.uvZ);
    return x * coordinates.weights.x + y * coordinates.weights.y + z * coordinates.weights.z;
}

half3 SampleTriplanarNormal(TriplanarCoordinates coordinates, half3 normalWS)
{
    half3 normalX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, coordinates.uvX), _BumpScale);
    half3 normalY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, coordinates.uvY), _BumpScale);
    half3 normalZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, coordinates.uvZ), _BumpScale);

    normalX = half3(normalX.xy + normalWS.zy, abs(normalX.z) * normalWS.x);
    normalY = half3(normalY.xy + normalWS.xz, abs(normalY.z) * normalWS.y);
    normalZ = half3(normalZ.xy + normalWS.xy, abs(normalZ.z) * normalWS.z);

    half3 blended = normalX.zyx * coordinates.weights.x + normalY.xzy * coordinates.weights.y + normalZ.xyz * coordinates.weights.z;
    return SafeNormalize(blended);
}

#endif
