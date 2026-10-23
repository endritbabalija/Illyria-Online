#ifndef WUXIA_OCCLUDER_FADE_INCLUDED
#define WUXIA_OCCLUDER_FADE_INCLUDED

// Dithers away fragments between the camera and the player, and right in front of the lens, so scatter merged
// into one mesh per cell (ScatterBatcher) can still be cleared from the camera's line. Wuxia.Player.CameraOccluderFade
// sets these globals every frame and turns _OCCLUDER_FADE on only while something is near that line. Globals sit
// outside UnityPerMaterial, so the SRP Batcher is unaffected. The clip is screen-space ordered dithering on an opaque
// surface: no blending, no sorting.

float4 _OccluderFadeStart; // xyz: camera position, w: strength (0 off .. 1 fully cleared)
float4 _OccluderFadeAxis;  // xyz: unit direction from the camera to the player's chest, w: that distance
float4 _OccluderFadeShape; // x: capsule radius, y: dithered rim width, z: easing stretch before the chest, w: unused
float4 _OccluderFadeNear;  // x: radius cleared around the camera, y: its dithered rim width

// 4x4 ordered (Bayer) threshold for a pixel, in [0, 1): 16 levels, float-only for GLES.
float OccluderFadeBayer2(float2 a)
{
    a = floor(a);
    return frac(a.x * 0.5 + a.y * a.y * 0.75);
}

float OccluderFadeDither(float2 pixel)
{
    return OccluderFadeBayer2(pixel * 0.5) * 0.25 + OccluderFadeBayer2(pixel);
}

// 0: keep the fragment, 1: clear it.
float OccluderFadeAmount(float3 positionWS)
{
    float3 offset = positionWS - _OccluderFadeStart.xyz;
    float along = dot(offset, _OccluderFadeAxis.xyz);
    float3 toLine = offset - _OccluderFadeAxis.xyz * clamp(along, 0.0, _OccluderFadeAxis.w);
    float capsule = 1.0 - smoothstep(_OccluderFadeShape.x - _OccluderFadeShape.y, _OccluderFadeShape.x, length(toLine));
    capsule *= 1.0 - smoothstep(_OccluderFadeAxis.w - _OccluderFadeShape.z, _OccluderFadeAxis.w, along);
    float nearCamera = 1.0 - smoothstep(_OccluderFadeNear.x, _OccluderFadeNear.x + _OccluderFadeNear.y, length(offset));
    return max(capsule, nearCamera) * _OccluderFadeStart.w;
}

void OccluderFadeClip(float3 positionWS, float4 positionCS)
{
    #if defined(_OCCLUDER_FADE)
        clip(OccluderFadeDither(positionCS.xy) - OccluderFadeAmount(positionWS));
    #endif
}

#endif
