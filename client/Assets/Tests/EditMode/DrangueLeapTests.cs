using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;
using Wuxia.World;

namespace Wuxia.Tests
{
    /// <summary>
    /// M2 "done when": Drangue Leap lands only on allowed ledges. Pure rule checks, then the landing
    /// search against real colliders, built far from anything else and destroyed after each test.
    /// </summary>
    public class DrangueLeapTests
    {
        /// <summary>Far from any scene geometry that shares the physics world.</summary>
        static readonly Vector3 Site = new Vector3(5000f, 0f, 5000f);
        const int SurfaceMask = 1; // Default layer

        LeapData rules;
        readonly List<GameObject> built = new List<GameObject>();
        readonly List<LeapLedge> ledges = new List<LeapLedge>();

        [SetUp]
        public void SetUp()
        {
            rules = SkillDataTests.Load().drangueLeap;
            ledges.Clear();
            Box("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(200f, 1f, 200f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in built) Object.DestroyImmediate(go);
            built.Clear();
        }

        static System.Numerics.Vector3 V(float x, float y, float z) => new System.Numerics.Vector3(x, y, z);

        GameObject Box(string name, Vector3 centre, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.position = Site + centre;
            go.AddComponent<BoxCollider>().size = size;
            built.Add(go);
            return go;
        }

        LeapLedge Ledge(string name, Vector3 centre, Vector3 size)
        {
            var ledge = Box(name, centre, size).AddComponent<LeapLedge>();
            ledges.Add(ledge);
            return ledge;
        }

        bool Find(Vector3 aim, out LeapLanding landing, Vector3 from = default)
        {
            Physics.SyncTransforms();
            return LeapPlanner.TryFindLanding(Site + from, aim, ledges, rules, SurfaceMask, out landing);
        }

        [Test]
        public void Rules_AcceptOnlyMarkedLedges_WithinLimits()
        {
            var from = V(0f, 0f, 0f);
            Assert.That(LeapRules.Check(rules, from, V(0f, 3f, 8f), true), Is.EqualTo(LeapCheck.Ok));
            Assert.That(LeapRules.Check(rules, from, V(0f, 3f, 8f), false), Is.EqualTo(LeapCheck.NotALedge));
            Assert.That(LeapRules.Check(rules, from, V(0f, 0f, rules.maxDistance), true), Is.EqualTo(LeapCheck.Ok));
            Assert.That(LeapRules.Check(rules, from, V(0f, 0f, rules.maxDistance + 0.01f), true), Is.EqualTo(LeapCheck.TooFar));
            Assert.That(LeapRules.Check(rules, from, V(0f, rules.maxRise, 5f), true), Is.EqualTo(LeapCheck.Ok));
            Assert.That(LeapRules.Check(rules, from, V(0f, rules.maxRise + 0.01f, 5f), true), Is.EqualTo(LeapCheck.TooHigh));
            Assert.That(LeapRules.Check(rules, from, V(0f, -rules.maxDrop - 0.01f, 5f), true), Is.EqualTo(LeapCheck.TooLow));
        }

        [Test]
        public void LandsOnTopOfAMarkedLedge_InsideItsEdges()
        {
            var ledge = Ledge("Ledge", new Vector3(0f, 1.5f, 8f), new Vector3(4f, 3f, 4f));
            Assert.That(Find(Vector3.forward, out var landing));
            Assert.That(landing.Ledge, Is.SameAs(ledge));
            var local = landing.Point - Site;
            Assert.That(local.y, Is.EqualTo(3f).Within(1e-3f), "on the top face");
            Assert.That(local.z, Is.EqualTo(6f + rules.landingInset).Within(1e-3f), "near edge, inset");
            Assert.That(Mathf.Abs(local.x), Is.LessThanOrEqualTo(2f - rules.landingInset + 1e-3f));
        }

        [Test]
        public void IgnoresUnmarkedGeometry_EvenStraightAhead()
        {
            Box("Unmarked rock", new Vector3(0f, 1.5f, 6f), new Vector3(4f, 3f, 4f));
            // A marked ledge exists, but outside the aim wedge (behind the player).
            Ledge("Behind", new Vector3(0f, 1.5f, -6f), new Vector3(4f, 3f, 4f));
            Assert.That(Find(Vector3.forward, out _), Is.False, "no landing: the leap becomes a ground dash");
        }

        [Test]
        public void RejectsLedgesTooFarOrTooHigh()
        {
            Ledge("Too far", new Vector3(0f, 1.5f, rules.maxDistance + rules.landingInset + 3f), new Vector3(4f, 3f, 4f));
            Ledge("Too high", new Vector3(5f, (rules.maxRise + 2f) / 2f, 5f), new Vector3(3f, rules.maxRise + 2f, 3f));
            Assert.That(Find(Vector3.forward, out _), Is.False);
            Assert.That(Find(new Vector3(1f, 0f, 1f), out _), Is.False);
        }

        [Test]
        public void RejectsALedgeWhoseTopIsCovered()
        {
            Ledge("Ledge", new Vector3(0f, 1.5f, 8f), new Vector3(4f, 3f, 4f));
            Box("Crate on top", new Vector3(0f, 3.5f, 8f), new Vector3(4f, 1f, 4f));
            Assert.That(Find(Vector3.forward, out _), Is.False);
        }

        [Test]
        public void PrefersTheLedgeStraightAhead()
        {
            Ledge("Off to the side", new Vector3(5f, 1.5f, 5f), new Vector3(3f, 3f, 3f));
            var ahead = Ledge("Ahead", new Vector3(0f, 1.5f, 9f), new Vector3(3f, 3f, 3f));
            Assert.That(Find(Vector3.forward, out var landing));
            Assert.That(landing.Ledge, Is.SameAs(ahead));
        }

        [Test]
        public void SkipsTheLedgeYouStandOn_AndLeapsToTheNextOne()
        {
            var here = Ledge("Here", new Vector3(0f, 1.5f, 0f), new Vector3(4f, 3f, 4f));
            var next = Ledge("Next", new Vector3(0f, 3f, 9f), new Vector3(4f, 6f, 4f));
            Assert.That(Find(Vector3.forward, out var landing, from: new Vector3(0f, 3f, 0f)));
            Assert.That(landing.Ledge, Is.SameAs(next));
            Assert.That(landing.Ledge, Is.Not.SameAs(here));
        }
    }
}
