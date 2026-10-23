using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wuxia.World;

namespace Wuxia.Tests
{
    /// <summary>Zone areas: which one the player stands in (box on the ground plane, priority on overlap) and the level text.</summary>
    public class ZoneAreaTests
    {
        readonly List<GameObject> built = new List<GameObject>();
        readonly List<ZoneArea> areas = new List<ZoneArea>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in built) Object.DestroyImmediate(go);
            built.Clear();
            areas.Clear();
        }

        ZoneArea Area(string name, int min, int max, Vector3 centre, Vector2 size, int priority, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(centre, Quaternion.Euler(0f, yaw, 0f));
            built.Add(go);
            var area = go.AddComponent<ZoneArea>();
            area.Configure(name, min, max, size, priority);
            areas.Add(area);
            return area;
        }

        [Test]
        public void At_PicksTheHigherPriorityWhereAreasOverlap()
        {
            var forest = Area("Oak Forest", 5, 8, Vector3.zero, new Vector2(100f, 60f), 0);
            var spring = Area("Dried Spring", 5, 8, new Vector3(-20f, 0f, 5f), new Vector2(20f, 20f), 1);

            Assert.AreSame(spring, ZoneArea.At(new Vector3(-20f, 3f, 5f), areas));
            Assert.AreSame(forest, ZoneArea.At(new Vector3(20f, 0f, 5f), areas));
            Assert.IsNull(ZoneArea.At(new Vector3(0f, 0f, 31f), areas));
        }

        [Test]
        public void Contains_IsABoxOnTheGroundPlane_EdgesInclusive_IgnoringHeight()
        {
            var area = Area("Meadow", 1, 4, new Vector3(10f, 0f, 10f), new Vector2(20f, 10f), 0);

            Assert.IsTrue(area.Contains(new Vector3(20f, 50f, 15f)));
            Assert.IsFalse(area.Contains(new Vector3(20.1f, 0f, 10f)));
            Assert.IsFalse(area.Contains(new Vector3(10f, 0f, 15.1f)));
        }

        [Test]
        public void Contains_FollowsTheAreasYRotation()
        {
            var area = Area("Bandit Camp", 8, 11, Vector3.zero, new Vector2(40f, 4f), 0, 90f);

            Assert.IsTrue(area.Contains(new Vector3(0f, 0f, 19f)));
            Assert.IsFalse(area.Contains(new Vector3(19f, 0f, 0f)));
        }

        [Test]
        public void LevelText_ShowsTheBand_ASingleLevel_OrNothingForSafeSpots()
        {
            Assert.AreEqual("Lv 11–12", Area("Wolf Den", 11, 12, Vector3.zero, Vector2.one, 0).LevelText);
            Assert.AreEqual("Lv 3", Area("Ridge", 3, 3, Vector3.zero, Vector2.one, 0).LevelText);
            Assert.AreEqual("", Area("The Han", 0, 0, Vector3.zero, Vector2.one, 0).LevelText);
        }
    }
}
