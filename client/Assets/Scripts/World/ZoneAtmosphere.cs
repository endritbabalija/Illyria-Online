using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Wuxia.World
{
    /// <summary>
    /// Gives each part of the zone the mood of its key art (Oak Valley: golden afternoon at the Han and
    /// the meadow, low gold in the oak forest, sunset in the upper valley, dusk at the bandit camp,
    /// overcast twilight at the wolf den, a misty dusk on the road north). When
    /// <see cref="ZoneSession.AreaChanged"/> fires it eases sun, ambient, fog, sky and the mood volumes
    /// to that area's <see cref="AtmosphereProfile"/>; outside a fade it costs nothing per frame.
    /// The mood volumes are global <see cref="Volume"/>s among this object's children, one per profile
    /// (other child volumes, like the zone's base post-processing, are left alone); the camera's volume
    /// stack is updated only while they change.
    /// </summary>
    public class ZoneAtmosphere : MonoBehaviour
    {
        [Serializable]
        public struct AreaMood
        {
            public string area;
            public AtmosphereProfile profile;
        }

        [SerializeField] AtmosphereProfile defaultProfile;
        [Tooltip("Areas (by ZoneArea name) and their moods; areas not listed use the default.")]
        [SerializeField] List<AreaMood> areas = new List<AreaMood>();
        [SerializeField] Light sun;
        [Tooltip("Wuxia/Sky material; a copy is driven at runtime so the asset keeps the default mood.")]
        [SerializeField] Material skyMaterial;
        [SerializeField] Camera gameCamera;

        static readonly int ZenithId = Shader.PropertyToID("_ZenithColor");
        static readonly int HorizonId = Shader.PropertyToID("_HorizonColor");
        static readonly int SunGlowId = Shader.PropertyToID("_SunGlowColor");
        static readonly int CloudColorId = Shader.PropertyToID("_CloudColor");
        static readonly int CloudCoverId = Shader.PropertyToID("_CloudCover");
        static readonly int RidgeFarId = Shader.PropertyToID("_RidgeFarColor");
        static readonly int RidgeNearId = Shader.PropertyToID("_RidgeNearColor");
        static readonly int SunDiscId = Shader.PropertyToID("_SunDisc");

        readonly List<Volume> volumes = new List<Volume>();
        float[] fromWeights = Array.Empty<float>();
        AtmosphereProfile current;
        AtmosphereState state, from, to;
        float fadeStart, fadeTime;
        bool fading, stackDirty, scriptedVolumes;
        Material runtimeSky;

        public AtmosphereProfile Current => current;
        public AtmosphereProfile DefaultProfile => defaultProfile;

        /// <summary>Wires the component (OakValleyBuilder).</summary>
        public void Configure(AtmosphereProfile start, List<AreaMood> moods, Light sunLight, Material sky, Camera camera)
        {
            defaultProfile = start;
            areas = moods;
            sun = sunLight;
            skyMaterial = sky;
            gameCamera = camera;
        }

        void Awake()
        {
            if (skyMaterial != null)
            {
                runtimeSky = new Material(skyMaterial) { name = skyMaterial.name + " (runtime)" };
                RenderSettings.skybox = runtimeSky;
            }
            if (gameCamera == null) gameCamera = Camera.main;
            ApplyInstant(defaultProfile);
        }

        void OnEnable()
        {
            if (ZoneSession.Instance != null) ZoneSession.Instance.AreaChanged += OnAreaChanged;
        }

        void Start()
        {
            // ZoneSession may wake after us.
            if (ZoneSession.Instance != null)
            {
                ZoneSession.Instance.AreaChanged -= OnAreaChanged;
                ZoneSession.Instance.AreaChanged += OnAreaChanged;
            }
        }

        void OnDisable()
        {
            if (ZoneSession.Instance != null) ZoneSession.Instance.AreaChanged -= OnAreaChanged;
        }

        void OnDestroy()
        {
            if (runtimeSky != null)
            {
                if (RenderSettings.skybox == runtimeSky) RenderSettings.skybox = skyMaterial;
                Destroy(runtimeSky);
            }
        }

        void OnAreaChanged(ZoneArea area)
        {
            if (area == null) return; // between areas: keep the mood
            FadeTo(ProfileFor(area.AreaName));
        }

        public AtmosphereProfile ProfileFor(string areaName)
        {
            foreach (var mood in areas)
                if (mood.area == areaName && mood.profile != null)
                    return mood.profile;
            return defaultProfile;
        }

        /// <summary>Eases to <paramref name="profile"/> over its <see cref="AtmosphereProfile.fadeTime"/>.</summary>
        public void FadeTo(AtmosphereProfile profile)
        {
            if (profile == null || profile == current) return;
            current = profile;
            from = state;
            to = AtmosphereState.From(profile);
            CollectVolumes();
            if (fromWeights.Length != volumes.Count) fromWeights = new float[volumes.Count];
            for (var i = 0; i < volumes.Count; i++) fromWeights[i] = volumes[i].weight;
            fadeStart = Time.time;
            fadeTime = Mathf.Max(0.01f, profile.fadeTime);
            fading = true;
        }

        void Update()
        {
            if (fading)
            {
                var t = Mathf.Clamp01((Time.time - fadeStart) / fadeTime);
                var eased = t * t * (3f - 2f * t);
                Apply(AtmosphereState.Lerp(from, to, eased));
                for (var i = 0; i < volumes.Count; i++)
                    volumes[i].weight = Mathf.Lerp(fromWeights[i], volumes[i].sharedProfile == current.volume ? 1f : 0f, eased);
                stackDirty = true;
                if (t >= 1f) fading = false;
            }
            // The volume manager only exists once the pipeline is up (not yet in Awake). Switching the camera
            // to scripted updates refreshes its stack once; from then on only fades do.
            if (stackDirty && gameCamera != null && VolumeManager.instance.isInitialized)
            {
                if (!scriptedVolumes)
                {
                    gameCamera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.ViaScripting);
                    scriptedVolumes = true;
                }
                gameCamera.UpdateVolumeStack();
                stackDirty = false;
            }
        }

        /// <summary>Sets <paramref name="profile"/> at once (also from the editor, to preview a mood or after a rebuild).</summary>
        public void ApplyInstant(AtmosphereProfile profile)
        {
            if (profile == null) return;
            current = profile;
            fading = false;
            Apply(AtmosphereState.From(profile));
            CollectVolumes();
            foreach (var volume in volumes) volume.weight = volume.sharedProfile == profile.volume ? 1f : 0f;
            stackDirty = Application.isPlaying;
        }

        /// <summary>The child volumes that belong to a mood.</summary>
        void CollectVolumes()
        {
            volumes.Clear();
            GetComponentsInChildren(true, volumes);
            volumes.RemoveAll(volume => !IsMoodVolume(volume.sharedProfile));
        }

        bool IsMoodVolume(VolumeProfile profile)
        {
            if (profile == null) return false;
            if (defaultProfile != null && defaultProfile.volume == profile) return true;
            foreach (var mood in areas)
                if (mood.profile != null && mood.profile.volume == profile)
                    return true;
            return false;
        }

        void Apply(AtmosphereState s)
        {
            state = s;
            if (sun != null)
            {
                sun.transform.rotation = s.SunRotation;
                sun.color = s.SunColor;
                sun.intensity = s.SunIntensity;
                sun.shadowStrength = s.ShadowStrength;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = s.AmbientSky;
            RenderSettings.ambientEquatorColor = s.AmbientEquator;
            RenderSettings.ambientGroundColor = s.AmbientGround;
            RenderSettings.fogColor = s.FogColor;
            RenderSettings.fogStartDistance = s.FogStart;
            RenderSettings.fogEndDistance = s.FogEnd;

            var sky = runtimeSky != null ? runtimeSky : skyMaterial;
            if (sky == null) return;
            sky.SetColor(ZenithId, s.SkyZenith);
            sky.SetColor(HorizonId, s.SkyHorizon);
            sky.SetColor(SunGlowId, s.SunGlow);
            sky.SetColor(CloudColorId, s.CloudColor);
            sky.SetFloat(CloudCoverId, s.CloudCover);
            sky.SetColor(RidgeFarId, s.RidgeFar);
            sky.SetColor(RidgeNearId, s.RidgeNear);
            sky.SetFloat(SunDiscId, s.SunDisc);
        }
    }
}
