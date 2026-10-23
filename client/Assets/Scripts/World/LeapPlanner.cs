using System.Collections.Generic;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.World
{
    public struct LeapLanding
    {
        public LeapLedge Ledge;
        public Vector3 Point;
    }

    /// <summary>
    /// Picks where a Drangue Leap lands: the best marked ledge inside the aim wedge that passes
    /// <see cref="LeapRules"/> and whose top is actually clear at the landing point. If none qualifies
    /// there is no landing and the leap becomes a ground dash.
    /// </summary>
    public static class LeapPlanner
    {
        /// <summary>How far above a landing point to look down from when checking it's clear.</summary>
        const float ProbeHeight = 3f;

        /// <param name="aim">Direction the player wants to go; only its ground-plane part is used.</param>
        /// <param name="surfaceMask">Layers that can stand on or cover a ledge (world geometry; not Player or Monster).</param>
        public static bool TryFindLanding(Vector3 from, Vector3 aim, IReadOnlyList<LeapLedge> ledges, LeapData rules,
            int surfaceMask, out LeapLanding landing)
        {
            landing = default;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) return false;

            var bestScore = float.MaxValue;
            var halfAngle = rules.aimAngle * 0.5f;
            foreach (var ledge in ledges)
            {
                if (ledge == null || !ledge.isActiveAndEnabled) continue;

                var point = ledge.LandingPoint(from, rules.landingInset);
                var offset = point - from;
                offset.y = 0f;
                var distance = offset.magnitude;
                // Already standing on it.
                if (distance < 1f && Mathf.Abs(point.y - from.y) < 0.5f) continue;

                var angle = distance < 0.001f ? 0f : Vector3.Angle(aim, offset);
                if (angle > halfAngle) continue;
                if (LeapRules.Check(rules, from.ToNumerics(), point.ToNumerics(), IsClearLedgeTop(ledge, point, surfaceMask)) != LeapCheck.Ok)
                    continue;

                // Favour ledges straight ahead, then nearer ones.
                var score = angle / Mathf.Max(1f, halfAngle) + distance / Mathf.Max(0.01f, rules.maxDistance);
                if (score >= bestScore) continue;
                bestScore = score;
                landing = new LeapLanding { Ledge = ledge, Point = point };
            }
            return landing.Ledge != null;
        }

        /// <summary>
        /// True when looking straight down at <paramref name="point"/> hits this ledge's own collider
        /// first, so the spot really is its top and nothing (a tree, a roof above) sits on it.
        /// </summary>
        public static bool IsClearLedgeTop(LeapLedge ledge, Vector3 point, int surfaceMask)
        {
            var start = point + Vector3.up * ProbeHeight;
            return Physics.Raycast(start, Vector3.down, out var hit, ProbeHeight + 0.2f, surfaceMask, QueryTriggerInteraction.Ignore)
                && hit.collider == ledge.Surface;
        }
    }
}
