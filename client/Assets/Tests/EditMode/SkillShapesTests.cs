using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.Tests
{
    /// <summary>
    /// M2 "done when": each form hits exactly the targets its shape covers. Targets are placed just
    /// inside and just outside every edge, and the selected set must match exactly.
    /// </summary>
    public class SkillShapesTests
    {
        const float Reach = 2f;
        static readonly Vector3 Origin = Vector3.Zero;
        static readonly Vector2 North = Vector2.UnitY; // +Z on the ground plane

        static FormData Form(string shape, float range = 0f, float radius = 0f, float angle = 0f) => new FormData
        {
            id = "test", name = "Test", shape = shape, range = range, radius = radius, angle = angle,
            minDamage = 1, maxDamage = 1, status = new StatusApplication(),
        };

        /// <summary>Ground point at <paramref name="distance"/> metres, <paramref name="bearing"/> degrees clockwise from +Z.</summary>
        static Vector3 At(float distance, float bearing, float height = 0f)
        {
            var r = bearing * System.MathF.PI / 180f;
            return new Vector3(System.MathF.Sin(r) * distance, height, System.MathF.Cos(r) * distance);
        }

        static List<int> Select(FormData form, IReadOnlyList<Vector3> targets, int locked = -1, Vector2? facing = null, float reach = Reach)
        {
            var hits = new List<int>();
            SkillShapes.SelectTargets(form, Origin, facing ?? North, targets, locked, reach, hits);
            return hits;
        }

        [Test]
        public void Single_HitsOnlyTheLockedTarget_WhenInRange()
        {
            var form = Form("single", range: 3f);
            var targets = new[] { At(2f, 0f), At(1f, 10f), At(2.5f, 180f) };
            Assert.That(Select(form, targets, locked: 0), Is.EqualTo(new[] { 0 }), "a closer monster beside the target is untouched");
            Assert.That(Select(form, targets, locked: 2), Is.EqualTo(new[] { 2 }), "single ignores facing");
            Assert.That(Select(form, targets), Is.Empty, "no lock, no hit");
        }

        [Test]
        public void Single_RangeEdge()
        {
            var form = Form("single", range: 3f);
            Assert.That(Select(form, new[] { At(3f, 45f) }, locked: 0), Is.EqualTo(new[] { 0 }), "exactly at range");
            Assert.That(Select(form, new[] { At(3.01f, 45f) }, locked: 0), Is.Empty, "just past range");
        }

        [Test]
        public void Cone_HitsExactlyTheWedge()
        {
            var form = Form("cone", range: 5f, angle: 90f); // ±45°
            var targets = new[]
            {
                At(4f, 0f),      // 0 dead ahead: in
                At(4.99f, 44f),  // 1 inside the right edge: in
                At(4.99f, -44f), // 2 inside the left edge: in
                At(4f, 46f),     // 3 just outside the right edge
                At(4f, -46f),    // 4 just outside the left edge
                At(5.02f, 0f),   // 5 just past the tip
                At(2f, 180f),    // 6 behind
                At(0.5f, 90f),   // 7 close but to the side
            };
            Assert.That(Select(form, targets), Is.EquivalentTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void Cone_FollowsFacing()
        {
            var form = Form("cone", range: 5f, angle: 60f);
            var targets = new[] { At(3f, 90f), At(3f, 0f) };
            var east = new Vector2(1f, 0f);
            Assert.That(Select(form, targets, facing: east), Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Circle_HitsAllAroundTheCaster_WithinRadius()
        {
            var form = Form("circle", radius: 4f);
            var targets = new[] { At(3.99f, 0f), At(4f, 120f), At(1f, 240f), At(4.02f, 300f), At(10f, 0f) };
            Assert.That(Select(form, targets), Is.EquivalentTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void CircleAtTarget_IsCentredOnTheLockedTarget()
        {
            var form = Form("circleAtTarget", range: 14f, radius: 4f);
            var centre = At(10f, 0f);
            var targets = new[]
            {
                centre,                              // 0 the target itself
                centre + new Vector3(3.99f, 0f, 0f), // 1 inside the circle
                centre + new Vector3(0f, 0f, -4.02f),// 2 just outside, towards the caster
                At(1f, 0f),                          // 3 next to the caster: not in the circle
            };
            Assert.That(Select(form, targets, locked: 0), Is.EquivalentTo(new[] { 0, 1 }));
        }

        [Test]
        public void CircleAtTarget_NeedsTheTargetInRange()
        {
            var form = Form("circleAtTarget", range: 14f, radius: 4f);
            var targets = new[] { At(14.1f, 0f), At(13f, 0f) };
            Assert.That(Select(form, targets, locked: 0), Is.Empty, "target out of range: nothing is hit, even inside the circle");
            Assert.That(Select(form, targets), Is.Empty, "no lock: nothing is hit");
        }

        [Test]
        public void Targets_TooFarAboveOrBelow_AreOutOfReach()
        {
            var form = Form("circle", radius: 4f);
            var targets = new[] { At(2f, 0f, Reach), At(2f, 90f, -Reach - 0.01f), At(2f, 180f, Reach + 0.01f) };
            Assert.That(Select(form, targets), Is.EqualTo(new[] { 0 }));
        }

        /// <summary>Every form in skills.json: a target just inside its edge is hit, one just outside is not.</summary>
        [Test]
        public void EveryFormInData_RespectsItsOwnEdges()
        {
            var data = SkillDataTests.Load();
            foreach (var book in data.books)
            foreach (var form in book.forms)
            {
                string what = $"{book.id}/{form.id}";
                switch (form.Shape)
                {
                    case SkillShape.Single:
                        Assert.That(Select(form, new[] { At(form.range - 0.01f, 30f) }, locked: 0), Has.Count.EqualTo(1), what);
                        Assert.That(Select(form, new[] { At(form.range + 0.01f, 30f) }, locked: 0), Is.Empty, what);
                        break;
                    case SkillShape.Cone:
                        var edge = form.angle / 2f;
                        Assert.That(Select(form, new[] { At(form.range - 0.01f, edge - 0.5f) }), Has.Count.EqualTo(1), what);
                        Assert.That(Select(form, new[] { At(form.range - 0.01f, edge + 0.5f) }), Is.Empty, what);
                        Assert.That(Select(form, new[] { At(form.range + 0.01f, 0f) }), Is.Empty, what);
                        break;
                    case SkillShape.Circle:
                        Assert.That(Select(form, new[] { At(form.radius - 0.01f, 200f) }), Has.Count.EqualTo(1), what);
                        Assert.That(Select(form, new[] { At(form.radius + 0.01f, 200f) }), Is.Empty, what);
                        break;
                    case SkillShape.CircleAtTarget:
                        var centre = At(form.range - 0.01f, 0f);
                        var inside = centre + new Vector3(form.radius - 0.01f, 0f, 0f);
                        var outside = centre + new Vector3(form.radius + 0.01f, 0f, 0f);
                        Assert.That(Select(form, new[] { centre, inside, outside }, locked: 0), Is.EquivalentTo(new[] { 0, 1 }), what);
                        break;
                }
            }
        }
    }
}
