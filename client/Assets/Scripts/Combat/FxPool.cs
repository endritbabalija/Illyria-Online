using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.Combat
{
    /// <summary>
    /// Pooled one-shot particle prefabs (Cartoon FX and our own): each instance is made once, has its
    /// lights and any other behaviour stripped (camera shake, self-destroy; cheap on phones and safe to
    /// reuse), and is switched off again when its particles are done. Spawning recolours it towards a
    /// tint, from the colours the prefab was authored with.
    /// </summary>
    public class FxPool
    {
        class Instance
        {
            public GameObject Root;
            public ParticleSystem[] Systems;
            public ParticleSystem[] Tops; // systems with no system above them; Play() starts their children
            public ParticleSystem.MinMaxGradient[] StartColors;
            public ParticleSystem.MinMaxGradient[] LifetimeColors;
            public Color Tint;
            public float Lifetime;
            public float EndTime;
        }

        const float MaxLifetime = 4f;

        readonly Transform parent;
        readonly Transform staging; // inactive, so new instances don't wake up (play, shake the camera) before they're stripped
        readonly Dictionary<GameObject, Stack<Instance>> free = new Dictionary<GameObject, Stack<Instance>>();
        readonly Dictionary<GameObject, Instance> template = new Dictionary<GameObject, Instance>();
        readonly List<KeyValuePair<GameObject, Instance>> live = new List<KeyValuePair<GameObject, Instance>>();

        public FxPool(Transform parent)
        {
            this.parent = parent;
            var go = new GameObject("FxStaging");
            go.SetActive(false);
            staging = go.transform;
            staging.SetParent(parent, false);
        }

        /// <summary>Makes instances ahead of time so the first cast doesn't hitch on Instantiate.</summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;
            if (!free.TryGetValue(prefab, out var stack)) free[prefab] = stack = new Stack<Instance>();
            while (stack.Count < count) stack.Push(Create(prefab));
        }

        /// <param name="tint">Recolour towards this; alpha is how far (0 keeps the prefab's colours).</param>
        public void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale, Color tint)
        {
            if (prefab == null) return;
            if (!free.TryGetValue(prefab, out var stack)) free[prefab] = stack = new Stack<Instance>();
            var fx = stack.Count > 0 ? stack.Pop() : Create(prefab);

            var t = fx.Root.transform;
            t.SetPositionAndRotation(position, rotation);
            t.localScale = Vector3.one * scale;
            if (fx.Tint != tint) Recolour(fx, tint);
            fx.Root.SetActive(true);
            foreach (var ps in fx.Tops)
            {
                ps.Clear(true);
                ps.Play(true);
            }
            fx.EndTime = Time.time + fx.Lifetime;
            live.Add(new KeyValuePair<GameObject, Instance>(prefab, fx));
        }

        /// <summary>Returns finished instances to the pool; call once a frame.</summary>
        public void Update()
        {
            var now = Time.time;
            for (var i = live.Count - 1; i >= 0; i--)
            {
                var fx = live[i].Value;
                if (now < fx.EndTime) continue;
                foreach (var ps in fx.Tops) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                fx.Root.SetActive(false);
                free[live[i].Key].Push(fx);
                live.RemoveAt(i);
            }
        }

        Instance Create(GameObject prefab)
        {
            var root = Object.Instantiate(prefab, staging, false);
            root.name = prefab.name;
            root.SetActive(false);
            // Lights and scripts (CFXR_Effect: camera shake, animated lights, Destroy when done) go;
            // the pool times and recycles the particles itself.
            foreach (var light in root.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
            root.transform.SetParent(parent, false);

            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            var tops = new List<ParticleSystem>();
            var lifetime = 0f;
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.playOnAwake = false;
                main.stopAction = ParticleSystemStopAction.None;
                lifetime = Mathf.Max(lifetime, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
                var above = ps.transform.parent;
                while (above != null && above != root.transform.parent && above.GetComponent<ParticleSystem>() == null) above = above.parent;
                if (above == null || above == root.transform.parent) tops.Add(ps);
            }

            var fx = new Instance
            {
                Root = root,
                Systems = systems,
                Tops = tops.ToArray(),
                StartColors = new ParticleSystem.MinMaxGradient[systems.Length],
                LifetimeColors = new ParticleSystem.MinMaxGradient[systems.Length],
                Tint = Color.clear,
                Lifetime = Mathf.Min(lifetime, MaxLifetime),
            };
            for (var i = 0; i < systems.Length; i++)
            {
                fx.StartColors[i] = systems[i].main.startColor;
                fx.LifetimeColors[i] = systems[i].colorOverLifetime.color;
            }
            return fx;
        }

        static void Recolour(Instance fx, Color tint)
        {
            fx.Tint = tint;
            for (var i = 0; i < fx.Systems.Length; i++)
            {
                var main = fx.Systems[i].main;
                main.startColor = Recolour(fx.StartColors[i], tint);
                var overLifetime = fx.Systems[i].colorOverLifetime;
                if (overLifetime.enabled) overLifetime.color = Recolour(fx.LifetimeColors[i], tint);
            }
        }

        static ParticleSystem.MinMaxGradient Recolour(ParticleSystem.MinMaxGradient g, Color tint)
        {
            if (tint.a <= 0f) return g;
            switch (g.mode)
            {
                case ParticleSystemGradientMode.Color: return new ParticleSystem.MinMaxGradient(Recolour(g.color, tint));
                case ParticleSystemGradientMode.TwoColors: return new ParticleSystem.MinMaxGradient(Recolour(g.colorMin, tint), Recolour(g.colorMax, tint));
                case ParticleSystemGradientMode.TwoGradients: return new ParticleSystem.MinMaxGradient(Recolour(g.gradientMin, tint), Recolour(g.gradientMax, tint));
                default:
                    return new ParticleSystem.MinMaxGradient(Recolour(g.gradient, tint)) { mode = g.mode };
            }
        }

        static Gradient Recolour(Gradient g, Color tint)
        {
            if (g == null) return null;
            var keys = g.colorKeys;
            for (var i = 0; i < keys.Length; i++) keys[i].color = Recolour(keys[i].color, tint);
            var copy = new Gradient { mode = g.mode };
            copy.SetKeys(keys, g.alphaKeys);
            return copy;
        }

        /// <summary>Keeps the colour's brightness and alpha, takes the tint's hue, by tint.a.</summary>
        public static Color Recolour(Color c, Color tint)
        {
            var bright = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            var tintBright = Mathf.Max(0.001f, Mathf.Max(tint.r, Mathf.Max(tint.g, tint.b)));
            var target = new Color(tint.r, tint.g, tint.b) * (bright / tintBright);
            var mixed = Color.Lerp(c, target, tint.a);
            mixed.a = c.a;
            return mixed;
        }
    }
}
