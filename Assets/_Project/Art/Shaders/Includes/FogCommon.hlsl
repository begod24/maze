#ifndef NO_WAY_OUT_FOG_COMMON_INCLUDED
#define NO_WAY_OUT_FOG_COMMON_INCLUDED

float4 _NoWayOutFogParams;
float4 _NoWayOutFogColor;

float NoWayOutFogExtinction(float height)
{
    return _NoWayOutFogParams.x * exp(-max(height - _NoWayOutFogParams.y, 0.0) * _NoWayOutFogParams.z);
}

float NoWayOutFogTransmittance(float3 cameraPositionWS, float3 positionWS)
{
    float distance = length(positionWS - cameraPositionWS);
    float3 middle = (cameraPositionWS + positionWS) * 0.5;
    float extinction = (NoWayOutFogExtinction(cameraPositionWS.y) + 2.0 * NoWayOutFogExtinction(middle.y) + NoWayOutFogExtinction(positionWS.y)) * 0.25;
    return exp(-extinction * distance);
}

float NoWayOutSunTransmittance(float height, float3 lightDirection)
{
    float heightScale = 1.0 / max(_NoWayOutFogParams.z, 1e-4);
    float above = max(height - _NoWayOutFogParams.y, 0.0);
    float opticalDepth = _NoWayOutFogParams.x * (max(_NoWayOutFogParams.y - height, 0.0) + heightScale * exp(-above * _NoWayOutFogParams.z));
    return exp(-opticalDepth / max(lightDirection.y, 0.05));
}

float4 ApplyNoWayOutFogPremultiplied(float4 color, float3 cameraPositionWS, float3 positionWS)
{
    float transmittance = NoWayOutFogTransmittance(cameraPositionWS, positionWS);
    color.rgb = color.rgb * transmittance + _NoWayOutFogColor.rgb * (1.0 - transmittance) * color.a;
    return color;
}

#endif
