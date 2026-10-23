using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.World
{
    /// <summary>
    /// A named part of a zone and its monster level band ("Oak Forest, Lv 5–8"): a box on the ground
    /// plane around this transform (rotate around Y only). Where areas overlap, the higher
    /// <see cref="priority"/> wins. <see cref="ZoneSession"/> tracks which one the player is in.
    /// </summary>
    public class ZoneArea : MonoBehaviour
    {
        static readonly List<ZoneArea> all = new List<ZoneArea>();

        [SerializeField] string areaName = "Area";
        [Tooltip("Lowest monster level here; 0 for a safe spot with no level band.")]
        [SerializeField] int minLevel;
        [SerializeField] int maxLevel;
        [Tooltip("Width (x) and depth (z) of the area on the ground plane.")]
        [SerializeField] Vector2 size = new Vector2(50f, 50f);
        [SerializeField] int priority;

        public string AreaName => areaName;
        public int MinLevel => minLevel;
        public int MaxLevel => maxLevel;

        /// <summary>"Lv 5–8", "Lv 11", or empty for an area without a level band.</summary>
        public string LevelText => minLevel <= 0 ? "" : maxLevel <= minLevel ? $"Lv {minLevel}" : $"Lv {minLevel}–{maxLevel}";

        public void Configure(string name, int min, int max, Vector2 groundSize, int areaPriority)
        {
            areaName = name;
            minLevel = min;
            maxLevel = max;
            size = groundSize;
            priority = areaPriority;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public bool Contains(Vector3 point)
        {
            var local = Quaternion.Inverse(transform.rotation) * (point - transform.position);
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.y * 0.5f;
        }

        /// <summary>The highest-priority enabled area containing <paramref name="point"/>, or null.</summary>
        public static ZoneArea At(Vector3 point) => At(point, all);

        public static ZoneArea At(Vector3 point, IReadOnlyList<ZoneArea> areas)
        {
            ZoneArea best = null;
            foreach (var area in areas)
                if (area.Contains(point) && (best == null || area.priority > best.priority))
                    best = area;
            return best;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.7f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.up * 2f, new Vector3(size.x, 4f, size.y));
        }
    }
}
