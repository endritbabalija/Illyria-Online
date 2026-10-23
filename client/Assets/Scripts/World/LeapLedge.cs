using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.World
{
    /// <summary>
    /// Marks the top of this box collider as a spot the Drangue Leap may land on: a ledge, wall top or
    /// tower roof. Nothing else is ever a landing spot. Keep ledges upright (rotate around Y only).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LeapLedge : MonoBehaviour
    {
        static readonly List<LeapLedge> all = new List<LeapLedge>();
        public static IReadOnlyList<LeapLedge> All => all;

        BoxCollider surface;

        public BoxCollider Surface => surface != null ? surface : surface = GetComponent<BoxCollider>();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        /// <summary>
        /// The point on top of the ledge nearest to <paramref name="from"/>, kept <paramref name="inset"/>
        /// metres inside its edges (or at the centre line if the ledge is narrower than that).
        /// </summary>
        public Vector3 LandingPoint(Vector3 from, float inset)
        {
            var box = Surface;
            var t = transform;
            var scale = t.lossyScale;
            var half = box.size * 0.5f;
            var spanX = Mathf.Max(0f, half.x - inset / Mathf.Abs(scale.x));
            var spanZ = Mathf.Max(0f, half.z - inset / Mathf.Abs(scale.z));

            var local = t.InverseTransformPoint(from);
            local.x = Mathf.Clamp(local.x, box.center.x - spanX, box.center.x + spanX);
            local.z = Mathf.Clamp(local.z, box.center.z - spanZ, box.center.z + spanZ);
            local.y = box.center.y + half.y;
            return t.TransformPoint(local);
        }

        void OnDrawGizmos()
        {
            var box = Surface;
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            var top = box.center + Vector3.up * (box.size.y * 0.5f);
            Gizmos.DrawWireCube(top, new Vector3(box.size.x, 0f, box.size.z));
        }
    }
}
