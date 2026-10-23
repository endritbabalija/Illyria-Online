using System;
using System.Collections.Generic;
using System.Numerics;
using Wuxia.Data;

namespace Wuxia.Combat
{
    /// <summary>
    /// Which targets a skill form hits, kept free of Unity types so the server can run the same rules
    /// in M4. Positions are world points (Y up); shapes are measured on the ground plane (X, Z) from
    /// the target's centre, and edges count as inside. A target more than <c>reachHeight</c> above or
    /// below the caster is out of reach whatever the shape.
    /// </summary>
    public static class SkillShapes
    {
        /// <summary>Slack so a target exactly on an edge stays inside despite float rounding.</summary>
        const float Epsilon = 1e-4f;

        /// <summary>Ground-plane distance between two world points.</summary>
        public static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(Flat(a), Flat(b));

        public static Vector2 Flat(Vector3 v) => new Vector2(v.X, v.Z);

        /// <summary>Can a single or circle-at-target form be aimed at <paramref name="target"/> from here?</summary>
        public static bool InReach(FormData form, Vector3 caster, Vector3 target, float reachHeight) =>
            FlatDistance(caster, target) <= form.range + Epsilon && WithinHeight(caster, target, reachHeight);

        /// <summary>
        /// Fills <paramref name="hits"/> with the indices of every candidate the form covers.
        /// </summary>
        /// <param name="facing">Cone direction on the ground plane (X, Z); needn't be normalised. Other shapes ignore it.</param>
        /// <param name="lockedIndex">Index of the locked target in <paramref name="candidates"/>, or -1 for none.</param>
        public static void SelectTargets(FormData form, Vector3 caster, Vector2 facing, IReadOnlyList<Vector3> candidates,
            int lockedIndex, float reachHeight, List<int> hits)
        {
            hits.Clear();
            switch (form.Shape)
            {
                case SkillShape.Single:
                    if (lockedIndex >= 0 && InReach(form, caster, candidates[lockedIndex], reachHeight)) hits.Add(lockedIndex);
                    break;

                case SkillShape.CircleAtTarget:
                    if (lockedIndex < 0 || !InReach(form, caster, candidates[lockedIndex], reachHeight)) break;
                    var centre = Flat(candidates[lockedIndex]);
                    for (var i = 0; i < candidates.Count; i++)
                        if (WithinHeight(caster, candidates[i], reachHeight) && InCircle(centre, form.radius, Flat(candidates[i])))
                            hits.Add(i);
                    break;

                case SkillShape.Circle:
                    var self = Flat(caster);
                    for (var i = 0; i < candidates.Count; i++)
                        if (WithinHeight(caster, candidates[i], reachHeight) && InCircle(self, form.radius, Flat(candidates[i])))
                            hits.Add(i);
                    break;

                case SkillShape.Cone:
                    var apex = Flat(caster);
                    for (var i = 0; i < candidates.Count; i++)
                        if (WithinHeight(caster, candidates[i], reachHeight) && InCone(apex, facing, form.range, form.angle, Flat(candidates[i])))
                            hits.Add(i);
                    break;
            }
        }

        public static bool InCircle(Vector2 centre, float radius, Vector2 point)
        {
            var r = radius + Epsilon;
            return Vector2.DistanceSquared(centre, point) <= r * r;
        }

        /// <param name="angleDegrees">Full width of the cone; 360 makes it a circle.</param>
        public static bool InCone(Vector2 apex, Vector2 facing, float range, float angleDegrees, Vector2 point)
        {
            var offset = point - apex;
            var distance = offset.Length();
            if (distance > range + Epsilon) return false;
            if (distance < Epsilon) return true; // standing inside the caster
            var forward = facing.LengthSquared() > 1e-8f ? Vector2.Normalize(facing) : Vector2.UnitY;
            var halfAngle = angleDegrees * 0.5 * Math.PI / 180.0;
            return Vector2.Dot(offset / distance, forward) >= Math.Cos(halfAngle) - 1e-6;
        }

        static bool WithinHeight(Vector3 caster, Vector3 target, float reachHeight) =>
            Math.Abs(target.Y - caster.Y) <= reachHeight + Epsilon;
    }
}
