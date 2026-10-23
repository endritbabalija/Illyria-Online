using System;
using System.Collections.Generic;
using UnityEngine;
using Wuxia.Data;

namespace Wuxia.Combat
{
    /// <summary>The weapon pivots of a character model (+Z runs along the blade), for blade trails and arrow muzzles.</summary>
    public struct WeaponHands
    {
        public Transform Main;
        public Transform OffHand;
    }

    /// <summary>
    /// Placeholder effects for combat, from a <see cref="CombatEffectSet"/>: per skill form a blade trail,
    /// bursts on the ground (slash arcs, spins, rings) and what travels out (arrows, a volley, a rain of
    /// arrows, a wave); arrows for bow basic attacks; a hit flash (burst plus a brief glow on the model)
    /// on everyone hit, held back until the arrow or wave that carries it arrives; and a burst as a
    /// status lands. Everything is pooled and made up front, without lights, so a cast costs a few
    /// particle bursts on a phone. Played for our player and for other players' casts alike.
    /// </summary>
    public class CombatEffects : MonoBehaviour
    {
        class Arrow
        {
            public GameObject Root;
            public TrailRenderer Trail;
            public Vector3 From, To;
            public Transform Homing;     // follows the target's aim point while flying
            public float HomingHeight;
            public float Start, Duration, Arc;
            public CombatEffectSet.FormEffect Impact;
            public Color HitColor;
        }

        class BladeTrail
        {
            public TrailRenderer Trail;
            public float Until;
        }

        struct Pending
        {
            public float At;
            public Action Run;
        }

        /// <summary>A form whose hits arrive by arrow or wave: hits from its caster wait for them.</summary>
        struct Carried
        {
            public Combatant Caster;
            public FormMotion Motion;
            public Vector3 Origin;
            public float Time;
            public CombatEffectSet.FormEffect Effect;
        }

        static readonly int EmissiveFactor = Shader.PropertyToID("emissiveFactor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] CombatEffectSet set;
        [Header("Arrows")]
        [SerializeField] float arrowSpeed = 45f;
        [Tooltip("Arrows are drawn this much larger than life, so they read on a phone.")]
        [SerializeField] float arrowScale = 1.6f;
        [Tooltip("Arc height per metre of flight.")]
        [SerializeField] float arrowArc = 0.03f;
        [SerializeField] float arrowTrailTime = 0.12f;
        [SerializeField] float arrowTrailWidth = 0.22f;
        [Tooltip("Height above the ground arrows of a rain start from.")]
        [SerializeField] float rainHeight = 9f;
        [Tooltip("Seconds over which a rain's arrows set off.")]
        [SerializeField] float rainSpread = 0.45f;
        [Tooltip("Muzzle height when a model has no weapon pivot.")]
        [SerializeField] float muzzleHeight = 1.4f;
        [Header("Wave")]
        [SerializeField] float waveSpeed = 14f;
        [Tooltip("Spacing of the impacts along a row of the wave, in metres.")]
        [SerializeField] float waveSpacing = 1.6f;
        [Header("Blade trails")]
        [SerializeField] float bladeTrailTime = 0.25f;
        [Tooltip("Ribbon width as a share of the blade length; it rides at the blade's middle-to-tip.")]
        [SerializeField] float bladeTrailWidth = 0.9f;
        [Header("Hits")]
        [Tooltip("Seconds a hit model glows.")]
        [SerializeField] float flashTime = 0.1f;
        [SerializeField] Color flashGlow = new Color(0.9f, 0.9f, 0.9f);
        [Tooltip("Pooled instances made per prefab on load.")]
        [SerializeField] int prewarm = 2;

        static Mesh arrowMesh;

        FxPool pool;
        readonly List<Arrow> flying = new List<Arrow>();
        readonly Stack<Arrow> arrows = new Stack<Arrow>();
        readonly Dictionary<Transform, BladeTrail> trails = new Dictionary<Transform, BladeTrail>();
        readonly List<Pending> pending = new List<Pending>();
        readonly List<Carried> carried = new List<Carried>();
        readonly Dictionary<Combatant, float> arrowArrivals = new Dictionary<Combatant, float>();
        readonly Dictionary<Combatant, KeyValuePair<CombatEffectSet.FormEffect, float>> lastForm = new Dictionary<Combatant, KeyValuePair<CombatEffectSet.FormEffect, float>>();
        readonly Dictionary<Combatant, Renderer[]> models = new Dictionary<Combatant, Renderer[]>();
        readonly List<KeyValuePair<Renderer[], float>> glowing = new List<KeyValuePair<Renderer[], float>>();
        MaterialPropertyBlock block;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            pool = new FxPool(transform);
            if (set == null) return;
            pool.Prewarm(set.hitFlash, prewarm * 2);
            foreach (var f in set.forms)
            {
                if (f.bursts != null) foreach (var b in f.bursts) pool.Prewarm(b.prefab, prewarm);
                pool.Prewarm(f.impact, f.motion == FormMotion.None ? 0 : prewarm * 2);
            }
            foreach (var s in set.statuses) pool.Prewarm(s.prefab, prewarm);
            for (var i = 0; i < 8; i++) arrows.Push(CreateArrow());
        }

        void OnEnable()
        {
            Combatant.AnyAttackResolved += OnAttackResolved;
            Combatant.AnyStatusApplied += OnStatusApplied;
        }

        void OnDisable()
        {
            Combatant.AnyAttackResolved -= OnAttackResolved;
            Combatant.AnyStatusApplied -= OnStatusApplied;
        }

        /// <summary>A skill form went off: its blade trail, bursts and whatever it sends flying.</summary>
        /// <param name="caster">The caster's Combatant when there is one here (our player), so its hits wait for its arrows.</param>
        /// <param name="facing">Ground-plane direction the form faced.</param>
        /// <param name="target">Locked target, if any.</param>
        public void PlayForm(FormData form, string weaponId, Vector3 origin, Vector3 facing, Combatant target, WeaponHands hands, Combatant caster = null)
        {
            var fx = set != null ? set.FindForm(form.id) : null;
            if (fx == null) return;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            facing.Normalize();
            var turn = Quaternion.LookRotation(facing);
            var targetPoint = target != null ? target.transform.position : origin;
            if (caster != null) lastForm[caster] = new KeyValuePair<CombatEffectSet.FormEffect, float>(fx, Time.time);

            if (fx.bladeTrail)
            {
                var weapon = set.FindWeapon(weaponId);
                if (weapon != null && weapon.bladeLength > 0f)
                {
                    StartTrail(hands.Main, weapon.bladeLength, fx.color, form.castTime);
                    if (weapon.offHand) StartTrail(hands.OffHand, weapon.bladeLength, fx.color, form.castTime);
                }
            }

            if (fx.bursts != null)
                foreach (var b in fx.bursts)
                {
                    if (b.prefab == null) continue;
                    var ground = b.at == BurstAt.Target ? targetPoint : origin;
                    var scale = b.scale * (b.scaleToArea ? AreaSize(form) : 1f);
                    var burst = b;
                    Later(b.delay, () => pool.Spawn(burst.prefab, ground + Vector3.up * burst.height, turn * Quaternion.Euler(burst.euler), scale, burst.tint));
                }

            var muzzle = Muzzle(hands, origin);
            switch (fx.motion)
            {
                case FormMotion.Arrow:
                    if (target != null) Shoot(muzzle, target, fx.color, fx);
                    break;
                case FormMotion.Volley:
                    Volley(form, fx, muzzle, origin, facing);
                    break;
                case FormMotion.Rain:
                    Rain(form, fx, origin, targetPoint);
                    break;
                case FormMotion.Wave:
                    Wave(form, fx, origin, facing);
                    break;
            }
            if (fx.motion != FormMotion.None && fx.motion != FormMotion.Arrow && caster != null)
                carried.Add(new Carried { Caster = caster, Motion = fx.motion, Origin = origin, Time = Time.time, Effect = fx });
        }

        /// <summary>A basic attack swung or shot: bows send an arrow at the target.</summary>
        public void PlayBasic(string weaponId, WeaponHands hands, Vector3 origin, Combatant target)
        {
            var weapon = set != null ? set.FindWeapon(weaponId) : null;
            if (weapon == null || !weapon.arrows || target == null) return;
            Shoot(Muzzle(hands, origin), target, weapon.color, null);
        }

        Vector3 Muzzle(WeaponHands hands, Vector3 origin) =>
            hands.Main != null ? hands.Main.position : origin + Vector3.up * muzzleHeight;

        static float AreaSize(FormData form)
        {
            switch (form.Shape)
            {
                case SkillShape.Circle:
                case SkillShape.CircleAtTarget: return form.radius;
                case SkillShape.Cone: return form.range;
                default: return 1f;
            }
        }

        // --- Arrows, volleys, rain and waves ---

        void Shoot(Vector3 from, Combatant target, Color color, CombatEffectSet.FormEffect impact)
        {
            var aim = target.transform.position + Vector3.up * target.AimHeight;
            var arrow = Launch(from, aim, color, impact);
            arrow.Homing = target.transform;
            arrow.HomingHeight = target.AimHeight;
            arrowArrivals[target] = arrow.Start + arrow.Duration;
        }

        Arrow Launch(Vector3 from, Vector3 to, Color color, CombatEffectSet.FormEffect impact, float speed = 0f)
        {
            var arrow = arrows.Count > 0 ? arrows.Pop() : CreateArrow();
            arrow.From = from;
            arrow.To = to;
            arrow.Homing = null;
            arrow.Start = Time.time;
            var distance = Vector3.Distance(from, to);
            arrow.Duration = Mathf.Max(0.05f, distance / (speed > 0f ? speed : arrowSpeed));
            arrow.Arc = distance * arrowArc;
            arrow.Impact = impact;
            arrow.HitColor = color;
            arrow.Root.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from + Vector3.up * 0.0001f));
            var c = color;
            c.a = 0.9f;
            arrow.Trail.startColor = c;
            c.a = 0f;
            arrow.Trail.endColor = c;
            arrow.Root.SetActive(true);
            arrow.Trail.Clear();
            arrow.Trail.emitting = true;
            flying.Add(arrow);
            return arrow;
        }

        void Volley(FormData form, CombatEffectSet.FormEffect fx, Vector3 muzzle, Vector3 origin, Vector3 facing)
        {
            var n = Mathf.Max(1, fx.count);
            var spread = form.angle * 0.8f;
            for (var i = 0; i < n; i++)
            {
                var yaw = n == 1 ? 0f : -spread * 0.5f + spread * i / (n - 1);
                var dir = Quaternion.Euler(0f, yaw, 0f) * facing;
                // Staggered a touch so the volley reads as several shots, not one plank.
                var from = muzzle;
                var to = origin + dir * form.range + Vector3.up * 0.15f;
                Later(i * 0.025f, () => Launch(from, to, fx.color, fx));
            }
        }

        void Rain(FormData form, CombatEffectSet.FormEffect fx, Vector3 origin, Vector3 centre)
        {
            var n = Mathf.Max(1, fx.count);
            var away = centre - origin;
            away.y = 0f;
            away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
            for (var i = 0; i < n; i++)
            {
                var spot = UnityEngine.Random.insideUnitCircle * (form.radius * 0.9f);
                var land = centre + new Vector3(spot.x, 0.05f, spot.y);
                // Falling steeply, a little away from the archer, as if loosed high over their head.
                var from = land + Vector3.up * rainHeight - away * (rainHeight * 0.25f);
                Later(rainSpread * i / n, () => Launch(from, land, fx.color, fx, arrowSpeed * 0.8f));
            }
        }

        void Wave(FormData form, CombatEffectSet.FormEffect fx, Vector3 origin, Vector3 facing)
        {
            var rows = Mathf.Max(1, fx.count);
            for (var r = 0; r < rows; r++)
            {
                var distance = form.range * (r + 1) / rows;
                var arc = Mathf.Deg2Rad * form.angle * 0.85f * distance;
                var n = Mathf.Max(1, Mathf.RoundToInt(arc / waveSpacing) + 1);
                for (var i = 0; i < n; i++)
                {
                    var yaw = n == 1 ? 0f : -form.angle * 0.425f + form.angle * 0.85f * i / (n - 1);
                    var at = origin + Quaternion.Euler(0f, yaw, 0f) * facing * distance;
                    var turn = Quaternion.LookRotation(Quaternion.Euler(0f, yaw, 0f) * facing);
                    Later(distance / waveSpeed, () => pool.Spawn(fx.impact, at, turn, fx.impactScale, fx.impactTint));
                }
            }
        }

        Arrow CreateArrow()
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * arrowScale;
            go.AddComponent<MeshFilter>().sharedMesh = ArrowMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = set != null ? set.arrowMaterial : null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = set != null ? set.trailMaterial : null;
            trail.time = arrowTrailTime;
            trail.widthCurve = AnimationCurve.Linear(0f, arrowTrailWidth, 1f, 0f);
            trail.minVertexDistance = 0.3f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            go.SetActive(false);
            return new Arrow { Root = go, Trail = trail };
        }

        void UpdateArrows(float now)
        {
            for (var i = flying.Count - 1; i >= 0; i--)
            {
                var arrow = flying[i];
                if (arrow.Homing != null && arrow.Homing.gameObject.activeInHierarchy)
                    arrow.To = arrow.Homing.position + Vector3.up * arrow.HomingHeight;
                var k = Mathf.Clamp01((now - arrow.Start) / arrow.Duration);
                var position = Vector3.Lerp(arrow.From, arrow.To, k) + Vector3.up * (arrow.Arc * 4f * k * (1f - k));
                var t = arrow.Root.transform;
                var step = position - t.position;
                if (step.sqrMagnitude > 0.000001f) t.rotation = Quaternion.LookRotation(step);
                t.position = position;
                if (k < 1f) continue;

                if (arrow.Impact != null && arrow.Impact.impact != null)
                    pool.Spawn(arrow.Impact.impact, arrow.To, t.rotation, arrow.Impact.impactScale, arrow.Impact.impactTint);
                arrow.Trail.emitting = false;
                arrow.Root.SetActive(false);
                arrows.Push(arrow);
                flying.RemoveAt(i);
            }
        }

        /// <summary>Unit arrow along +Z (tail at the origin, 0.75 m): crossed quads for shaft, head and fletching, both sides.</summary>
        static Mesh ArrowMesh()
        {
            if (arrowMesh != null) return arrowMesh;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            const float length = 0.75f, shaft = 0.012f, head = 0.045f, headLength = 0.12f, fletch = 0.035f, fletchLength = 0.14f;
            var tip = length;
            var neck = length - headLength;
            Quad(new Vector3(-shaft, 0f, 0f), new Vector3(shaft, 0f, 0f), new Vector3(shaft, 0f, neck), new Vector3(-shaft, 0f, neck));
            Quad(new Vector3(0f, -shaft, 0f), new Vector3(0f, shaft, 0f), new Vector3(0f, shaft, neck), new Vector3(0f, -shaft, neck));
            Quad(new Vector3(-head, 0f, neck), new Vector3(head, 0f, neck), new Vector3(0f, 0f, tip), new Vector3(0f, 0f, tip));
            Quad(new Vector3(0f, -head, neck), new Vector3(0f, head, neck), new Vector3(0f, 0f, tip), new Vector3(0f, 0f, tip));
            Quad(new Vector3(-fletch, 0f, 0f), new Vector3(fletch, 0f, 0f), new Vector3(shaft, 0f, fletchLength), new Vector3(-shaft, 0f, fletchLength));
            Quad(new Vector3(0f, -fletch, 0f), new Vector3(0f, fletch, 0f), new Vector3(0f, shaft, fletchLength), new Vector3(0f, -shaft, fletchLength));
            arrowMesh = new Mesh { name = "Arrow" };
            arrowMesh.SetVertices(vertices);
            arrowMesh.SetTriangles(triangles, 0);
            arrowMesh.RecalculateNormals();
            arrowMesh.RecalculateBounds();
            return arrowMesh;
        }

        // --- Blade trails ---

        void StartTrail(Transform pivot, float bladeLength, Color color, float duration)
        {
            if (pivot == null || !pivot.gameObject.activeInHierarchy) return;
            if (!trails.TryGetValue(pivot, out var blade) || blade.Trail == null)
            {
                var go = new GameObject("BladeTrail");
                go.transform.SetParent(pivot, false);
                var trail = go.AddComponent<TrailRenderer>();
                trail.sharedMaterial = set.trailMaterial;
                trail.time = bladeTrailTime;
                trail.minVertexDistance = 0.05f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.emitting = false;
                trails[pivot] = blade = new BladeTrail { Trail = trail };
            }
            // Rides between the blade's middle and tip, about as wide as that span.
            blade.Trail.transform.localPosition = new Vector3(0f, 0f, bladeLength * 0.7f);
            blade.Trail.widthCurve = AnimationCurve.Linear(0f, bladeLength * bladeTrailWidth, 1f, 0f);
            var c = color;
            c.a = 0.85f;
            blade.Trail.startColor = c;
            c.a = 0f;
            blade.Trail.endColor = c;
            blade.Trail.Clear();
            blade.Trail.emitting = true;
            blade.Until = Time.time + duration;
        }

        // --- Hits and statuses ---

        void OnAttackResolved(Combatant target, AttackResult result)
        {
            if (!result.Hit || set == null || target == null) return;
            var color = set.basicHit;
            var scale = set.hitFlashScale;
            var attacker = target.LastAttacker;
            var delay = 0f;
            switch (result.Kind)
            {
                case HitKind.Poison:
                    color = set.poisonHit;
                    break;
                case HitKind.Skill:
                    color = set.skillHit;
                    if (attacker != null && lastForm.TryGetValue(attacker, out var last) && Time.time - last.Value < 2f)
                    {
                        color = last.Key.color;
                        color.a = 1f;
                        delay = CarriedDelay(attacker, last.Key, target);
                    }
                    break;
                default:
                    if (target.Team == Team.Player) color = set.takenHit;
                    break;
            }
            if (result.Crit)
            {
                color = set.critHit;
                scale = set.critFlashScale;
            }
            // A hit that an arrow brings shows when the arrow gets there.
            if (arrowArrivals.TryGetValue(target, out var arrival)) delay = Mathf.Max(delay, arrival - Time.time);
            if (delay > 0.02f) Later(delay, () => Flash(target, color, scale));
            else Flash(target, color, scale);
        }

        /// <summary>Seconds until the volley, rain or wave of <paramref name="effect"/> reaches <paramref name="target"/>.</summary>
        float CarriedDelay(Combatant caster, CombatEffectSet.FormEffect effect, Combatant target)
        {
            for (var i = carried.Count - 1; i >= 0; i--)
            {
                var c = carried[i];
                if (c.Caster != caster || c.Effect != effect) continue;
                var distance = Vector3.Distance(c.Origin, target.transform.position);
                float travel;
                switch (c.Motion)
                {
                    case FormMotion.Volley: travel = distance / arrowSpeed; break;
                    case FormMotion.Rain: travel = rainSpread * 0.5f + rainHeight / (arrowSpeed * 0.8f); break;
                    case FormMotion.Wave: travel = distance / waveSpeed; break;
                    default: travel = 0f; break;
                }
                return c.Time + travel - Time.time;
            }
            return 0f;
        }

        void Flash(Combatant target, Color color, float scale)
        {
            if (target == null || !target.isActiveAndEnabled) return;
            var point = target.transform.position + Vector3.up * target.AimHeight;
            var view = Camera.main;
            var facing = view != null ? Quaternion.LookRotation(view.transform.position - point) : Quaternion.identity;
            pool.Spawn(set.hitFlash, point, facing, scale, color);
            Glow(target);
        }

        void Glow(Combatant target)
        {
            // Monsters only: the player's renderers carry their own property blocks (cloth tint).
            if (target.Team != Team.Monster) return;
            if (!models.TryGetValue(target, out var renderers) || renderers == null)
            {
                var found = new List<Renderer>();
                foreach (var r in target.GetComponentsInChildren<Renderer>(true))
                    if (r is SkinnedMeshRenderer || r is MeshRenderer) found.Add(r);
                models[target] = renderers = found.ToArray();
            }
            block.Clear();
            block.SetColor(EmissiveFactor, flashGlow);
            block.SetColor(EmissionColor, flashGlow);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(block);
            glowing.Add(new KeyValuePair<Renderer[], float>(renderers, Time.time + flashTime));
        }

        void OnStatusApplied(Combatant target, StatusData status)
        {
            var fx = set != null ? set.FindStatus(status.id) : null;
            if (fx == null || fx.prefab == null) return;
            var point = target.transform.position + Vector3.up * (target.AimHeight * 0.5f);
            pool.Spawn(fx.prefab, point, target.transform.rotation, fx.scale, fx.tint);
        }

        // --- Frame ---

        void Later(float delay, Action run)
        {
            if (delay <= 0f) run();
            else pending.Add(new Pending { At = Time.time + delay, Run = run });
        }

        void LateUpdate()
        {
            var now = Time.time;
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                if (now < pending[i].At) continue;
                var run = pending[i].Run;
                pending.RemoveAt(i);
                run();
            }

            UpdateArrows(now);

            foreach (var blade in trails.Values)
                if (blade.Trail != null && blade.Trail.emitting && now >= blade.Until) blade.Trail.emitting = false;

            for (var i = glowing.Count - 1; i >= 0; i--)
            {
                if (now < glowing[i].Value) continue;
                foreach (var r in glowing[i].Key) if (r != null) r.SetPropertyBlock(null);
                glowing.RemoveAt(i);
            }

            for (var i = carried.Count - 1; i >= 0; i--)
                if (now - carried[i].Time > 3f) carried.RemoveAt(i);
            if (arrowArrivals.Count > 0 && flying.Count == 0) arrowArrivals.Clear();

            pool.Update();
        }
    }
}
