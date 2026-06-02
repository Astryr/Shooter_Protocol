#ifndef LAB_COMMON_INCLUDED
#define LAB_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// UV cilíndrica (ideal para pilares / cilindros ProBuilder con eje Y).
float2 LabCylindricalUV(float3 positionOS, float scale)
{
    float angle = atan2(positionOS.z, positionOS.x) * 0.15915494309; // 1 / (2*pi)
    return float2(angle * scale, positionOS.y * scale);
}

float LabGridMask(float2 uv, float lineWidth)
{
    float2 grid = abs(frac(uv) - 0.5);
    float2 dist = grid / max(fwidth(uv), 0.0001);
    float gridLine = min(dist.x, dist.y);
    return 1.0 - smoothstep(lineWidth, lineWidth + 1.0, gridLine);
}

float LabDiagonalStripes(float2 uv, float angleRad, float scale, float width)
{
    float2 p = uv * scale;
    float2 dir = float2(cos(angleRad), sin(angleRad));
    float t = dot(p, dir);
    float stripe = abs(frac(t) - 0.5) / max(fwidth(t), 0.0001);
    return smoothstep(width, width + 1.0, stripe);
}

half3 LabApplyLighting(half3 albedo, float3 positionWS, half3 normalWS, half shadowAtten)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    half NdotL = saturate(dot(normalWS, mainLight.direction));
    half3 diffuse = albedo * mainLight.color * NdotL * shadowAtten;
    half3 ambient = SampleSH(normalWS) * albedo;
    return ambient + diffuse;
}

#endif
