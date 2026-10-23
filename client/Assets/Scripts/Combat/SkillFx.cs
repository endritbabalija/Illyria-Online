using System.Collections.Generic;
using UnityEngine;
using Wuxia.Data;

namespace Wuxia.Combat
{
    /// <summary>
    /// Greybox skill effect: flashes the exact area a form covers (cone, circle, or a small ring on a
    /// single target) on the ground, tinted by the status it carries, and fades it out.
    /// </summary>
    public class SkillFx : MonoBehaviour
    {
        class Flash
        {
            public Transform Transform;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Color Color;
            public float Age;
        }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("Transparent unlit material; its _BaseColor is overridden per flash.")]
        [SerializeField] Material material;
        [SerializeField] float lifetime = 0.5f;
        [SerializeField] float groundOffset = 0.06f;
        [Tooltip("Ring radius drawn under the target of a single-target form.")]
        [SerializeField] float singleRadius = 0.9f;
        [SerializeField] Color plain = new Color(1f, 0.8f, 0.3f, 0.45f);
        [SerializeField] Color poison = new Color(0.35f, 0.95f, 0.3f, 0.45f);
        [SerializeField] Color paralysis = new Color(0.35f, 0.85f, 1f, 0.45f);
        [SerializeField] Color stun = new Color(0.75f, 0.45f, 1f, 0.45f);

        readonly List<Flash> active = new List<Flash>();
        readonly Stack<Flash> pool = new Stack<Flash>();
        readonly Dictionary<int, Mesh> fans = new Dictionary<int, Mesh>();
        MaterialPropertyBlock block;

        void Awake() => block = new MaterialPropertyBlock();

        /// <param name="facing">Ground-plane direction the caster faced as the form fired.</param>
        /// <param name="target">Locked target's position (the caster's if there is none).</param>
        public void Play(FormData form, Vector3 caster, Vector3 facing, Vector3 target)
        {
            var color = ColorFor(form.status);
            switch (form.Shape)
            {
                case SkillShape.Single: Spawn(target, facing, singleRadius, 360f, color); break;
                case SkillShape.Cone: Spawn(caster, facing, form.range, form.angle, color); break;
                case SkillShape.Circle: Spawn(caster, facing, form.radius, 360f, color); break;
                case SkillShape.CircleAtTarget: Spawn(target, facing, form.radius, 360f, color); break;
            }
        }

        Color ColorFor(StatusApplication status)
        {
            if (!status.Any) return plain;
            switch (status.id)
            {
                case "poison": return poison;
                case "paralysis": return paralysis;
                default: return stun;
            }
        }

        void Spawn(Vector3 centre, Vector3 facing, float radius, float angle, Color color)
        {
            var flash = pool.Count > 0 ? pool.Pop() : Create();
            flash.Filter.sharedMesh = Fan(angle);
            flash.Transform.SetPositionAndRotation(centre + Vector3.up * groundOffset,
                Quaternion.LookRotation(facing.sqrMagnitude > 0.0001f ? facing : Vector3.forward));
            flash.Transform.localScale = new Vector3(radius, 1f, radius);
            flash.Color = color;
            flash.Age = 0f;
            flash.Renderer.gameObject.SetActive(true);
            Apply(flash, 1f);
            active.Add(flash);
        }

        Flash Create()
        {
            var go = new GameObject("SkillFlash");
            go.transform.SetParent(transform, false);
            var flash = new Flash
            {
                Transform = go.transform,
                Filter = go.AddComponent<MeshFilter>(),
                Renderer = go.AddComponent<MeshRenderer>(),
            };
            flash.Renderer.sharedMaterial = material;
            flash.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flash.Renderer.receiveShadows = false;
            return flash;
        }

        void LateUpdate()
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var flash = active[i];
                flash.Age += Time.deltaTime;
                var k = flash.Age / lifetime;
                if (k >= 1f)
                {
                    flash.Renderer.gameObject.SetActive(false);
                    pool.Push(flash);
                    active.RemoveAt(i);
                    continue;
                }
                Apply(flash, 1f - k * k);
            }
        }

        void Apply(Flash flash, float fade)
        {
            var c = flash.Color;
            c.a *= fade;
            block.SetColor(BaseColor, c);
            flash.Renderer.SetPropertyBlock(block);
        }

        /// <summary>Unit-radius wedge on the ground plane, centred on +Z; 360 gives a disc. Cached per angle.</summary>
        Mesh Fan(float angle)
        {
            var key = Mathf.RoundToInt(angle);
            if (fans.TryGetValue(key, out var mesh)) return mesh;

            var segments = Mathf.Max(6, Mathf.CeilToInt(key / 6f));
            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (var s = 0; s <= segments; s++)
            {
                var a = Mathf.Deg2Rad * (-key * 0.5f + key * s / (float)segments);
                vertices[s + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
            for (var s = 0; s < segments; s++)
            {
                triangles[s * 3] = 0;
                triangles[s * 3 + 1] = s + 1;
                triangles[s * 3 + 2] = s + 2;
            }

            mesh = new Mesh { name = $"SkillFan{key}", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            fans[key] = mesh;
            return mesh;
        }
    }
}
