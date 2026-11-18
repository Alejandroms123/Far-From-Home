#ifndef MAIN_LIGHT_SHADOWS_INCLUDED
#define MAIN_LIGHT_SHADOWS_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif

void SampleMainLightShadow_float(
    float3 WorldPos,
    out float Shadow
)
{
#if defined(SHADERGRAPH_PREVIEW)
        Shadow = 1.0;
#else
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light mainLight = GetMainLight(shadowCoord);
    Shadow = mainLight.shadowAttenuation;
#endif
}

#endif