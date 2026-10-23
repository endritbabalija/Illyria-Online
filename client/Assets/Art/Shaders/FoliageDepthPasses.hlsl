#ifndef WUXIA_FOLIAGE_DEPTH_PASSES_INCLUDED
#define WUXIA_FOLIAGE_DEPTH_PASSES_INCLUDED

// DepthOnly and DepthNormals for Wuxia/Foliage: URP Lit's passes for a surface without normal, parallax, detail or
// alpha-clip maps, plus a world position so they clip exactly like the colour pass (a depth prepass that kept the
// cleared fragments would hide what's behind them).

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#include "OccluderFade.hlsl"

struct FoliageDepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct FoliageDepthVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    #if defined(FOLIAGE_DEPTH_NORMALS)
        half3 normalWS : TEXCOORD1;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

FoliageDepthVaryings FoliageDepthVertex(FoliageDepthAttributes input)
{
    FoliageDepthVaryings output = (FoliageDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    #if defined(FOLIAGE_DEPTH_NORMALS)
        output.normalWS = half3(TransformObjectToWorldNormal(input.normalOS));
    #endif
    return output;
}

void FoliageDepthClip(FoliageDepthVaryings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    OccluderFadeClip(input.positionWS, input.positionCS);
    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif
}

half FoliageDepthOnlyFragment(FoliageDepthVaryings input) : SV_TARGET
{
    FoliageDepthClip(input);
    return input.positionCS.z;
}

#if defined(FOLIAGE_DEPTH_NORMALS)
void FoliageDepthNormalsFragment(
    FoliageDepthVaryings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    FoliageDepthClip(input);

    #if defined(_GBUFFER_NORMALS_OCT)
        float3 normalWS = normalize(input.normalWS);
        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
        outNormalWS = half4(PackFloat2To888(remappedOctNormalWS), 0.0);
    #else
        outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
    #endif

    #ifdef _WRITE_RENDERING_LAYERS
        outRenderingLayers = EncodeMeshRenderingLayer();
    #endif
}
#endif

#endif
