using UnityEngine;
using UnityEngine.Rendering;

namespace Wuxia.World
{
    /// <summary>
    /// One mood of a zone (sun, ambient, fog, sky and a post-processing profile), matched to an area's
    /// key art. <see cref="ZoneAtmosphere"/> eases between them as the player walks from area to area.
    /// Everything here is cheap on phones: a directional light, trilight ambient, linear fog, the
    /// <c>Wuxia/Sky</c> skybox and colour grading that bakes into URP's LUT.
    /// </summary>
    [CreateAssetMenu(menuName = "Wuxia/Atmosphere Profile")]
    public class AtmosphereProfile : ScriptableObject
    {
        [Header("Sun")]
        [Tooltip("Degrees above the horizon.")]
        public float sunElevation = 30f;
        [Tooltip("Compass direction the sunlight comes from (0 = north, 90 = east).")]
        public float sunAzimuth = 292f;
        public Color sunColor = new Color(1f, 0.86f, 0.66f);
        public float sunIntensity = 1.5f;
        [Range(0f, 1f)] public float shadowStrength = 0.85f;

        [Header("Ambient (trilight)")]
        public Color ambientSky = new Color(0.55f, 0.62f, 0.78f);
        public Color ambientEquator = new Color(0.62f, 0.55f, 0.45f);
        public Color ambientGround = new Color(0.27f, 0.24f, 0.18f);

        [Header("Fog (linear)")]
        public Color fogColor = new Color(0.86f, 0.78f, 0.68f);
        public float fogStart = 40f;
        public float fogEnd = 260f;

        [Header("Sky (Wuxia/Sky)")]
        public Color skyZenith = new Color(0.30f, 0.46f, 0.78f);
        public Color skyHorizon = new Color(0.98f, 0.80f, 0.64f);
        [Tooltip("Glow along the horizon on the sun's side.")]
        public Color sunGlow = new Color(1f, 0.62f, 0.32f);
        public Color cloudColor = new Color(1f, 0.82f, 0.74f);
        [Range(0f, 1f)] public float cloudCover = 0.45f;
        [Tooltip("Distant ranges painted into the sky: far layer, then near layer.")]
        public Color ridgeFar = new Color(0.72f, 0.70f, 0.74f);
        public Color ridgeNear = new Color(0.55f, 0.56f, 0.62f);
        [Range(0f, 1f)] public float sunDisc = 1f;

        [Header("Post-processing")]
        [Tooltip("Colour grading for this mood; blended on top of the zone's base volume.")]
        public VolumeProfile volume;
        [Tooltip("Seconds to ease into this mood.")]
        public float fadeTime = 4f;

        public Quaternion SunRotation => Quaternion.Euler(sunElevation, sunAzimuth + 180f, 0f);
    }

    /// <summary>The blendable values of an <see cref="AtmosphereProfile"/>, applied to the scene by <see cref="ZoneAtmosphere"/>.</summary>
    public struct AtmosphereState
    {
        public Quaternion SunRotation;
        public Color SunColor;
        public float SunIntensity, ShadowStrength;
        public Color AmbientSky, AmbientEquator, AmbientGround;
        public Color FogColor;
        public float FogStart, FogEnd;
        public Color SkyZenith, SkyHorizon, SunGlow, CloudColor, RidgeFar, RidgeNear;
        public float CloudCover, SunDisc;

        public static AtmosphereState From(AtmosphereProfile p) => new AtmosphereState
        {
            SunRotation = p.SunRotation,
            SunColor = p.sunColor,
            SunIntensity = p.sunIntensity,
            ShadowStrength = p.shadowStrength,
            AmbientSky = p.ambientSky,
            AmbientEquator = p.ambientEquator,
            AmbientGround = p.ambientGround,
            FogColor = p.fogColor,
            FogStart = p.fogStart,
            FogEnd = p.fogEnd,
            SkyZenith = p.skyZenith,
            SkyHorizon = p.skyHorizon,
            SunGlow = p.sunGlow,
            CloudColor = p.cloudColor,
            CloudCover = p.cloudCover,
            RidgeFar = p.ridgeFar,
            RidgeNear = p.ridgeNear,
            SunDisc = p.sunDisc,
        };

        public static AtmosphereState Lerp(AtmosphereState a, AtmosphereState b, float t) => new AtmosphereState
        {
            SunRotation = Quaternion.Slerp(a.SunRotation, b.SunRotation, t),
            SunColor = Color.Lerp(a.SunColor, b.SunColor, t),
            SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t),
            ShadowStrength = Mathf.Lerp(a.ShadowStrength, b.ShadowStrength, t),
            AmbientSky = Color.Lerp(a.AmbientSky, b.AmbientSky, t),
            AmbientEquator = Color.Lerp(a.AmbientEquator, b.AmbientEquator, t),
            AmbientGround = Color.Lerp(a.AmbientGround, b.AmbientGround, t),
            FogColor = Color.Lerp(a.FogColor, b.FogColor, t),
            FogStart = Mathf.Lerp(a.FogStart, b.FogStart, t),
            FogEnd = Mathf.Lerp(a.FogEnd, b.FogEnd, t),
            SkyZenith = Color.Lerp(a.SkyZenith, b.SkyZenith, t),
            SkyHorizon = Color.Lerp(a.SkyHorizon, b.SkyHorizon, t),
            SunGlow = Color.Lerp(a.SunGlow, b.SunGlow, t),
            CloudColor = Color.Lerp(a.CloudColor, b.CloudColor, t),
            CloudCover = Mathf.Lerp(a.CloudCover, b.CloudCover, t),
            RidgeFar = Color.Lerp(a.RidgeFar, b.RidgeFar, t),
            RidgeNear = Color.Lerp(a.RidgeNear, b.RidgeNear, t),
            SunDisc = Mathf.Lerp(a.SunDisc, b.SunDisc, t),
        };
    }
}
