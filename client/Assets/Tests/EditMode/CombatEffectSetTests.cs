using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using Wuxia.Combat;

namespace Wuxia.Tests
{
    /// <summary>The placeholder combat effects cover every skill form, weapon and status in the shared data.</summary>
    public class CombatEffectSetTests
    {
        const string SetPath = "Assets/Art/Vfx/CombatEffects.asset";

        static CombatEffectSet Load()
        {
            var set = AssetDatabase.LoadAssetAtPath<CombatEffectSet>(SetPath);
            Assert.That(set, Is.Not.Null, $"{SetPath} not found");
            return set;
        }

        [Test]
        public void EveryForm_HasAnEffect_ThatShowsSomething()
        {
            var set = Load();
            var known = new HashSet<string>();
            foreach (var book in SkillDataTests.Load().books)
                foreach (var form in book.forms)
                {
                    known.Add(form.id);
                    var fx = set.FindForm(form.id);
                    Assert.That(fx, Is.Not.Null, $"no effect for form '{form.id}'");
                    var shows = fx.bladeTrail || fx.motion != FormMotion.None || (fx.bursts != null && fx.bursts.Length > 0);
                    Assert.That(shows, $"'{form.id}' shows nothing");
                    if (fx.bursts != null)
                        foreach (var b in fx.bursts) Assert.That(b.prefab, Is.Not.Null, $"'{form.id}' has a burst without a prefab");
                    if (fx.motion != FormMotion.None && fx.motion != FormMotion.Arrow)
                        Assert.That(fx.impact, Is.Not.Null, $"'{form.id}' has no impact for its {fx.motion}");
                    if (fx.motion == FormMotion.Volley || fx.motion == FormMotion.Rain || fx.motion == FormMotion.Wave)
                        Assert.That(fx.count, Is.GreaterThan(0), form.id);
                }
            foreach (var fx in set.forms) Assert.That(known.Contains(fx.form), $"effect for unknown form '{fx.form}'");
        }

        [Test]
        public void Weapons_AndStatuses_MatchTheGameData()
        {
            var set = Load();
            var weapons = new HashSet<string>();
            foreach (var w in CombatDataTests.Load().weapons) weapons.Add(w.id);
            foreach (var w in set.weapons) Assert.That(weapons.Contains(w.weapon), $"effect for unknown weapon '{w.weapon}'");
            foreach (var status in SkillDataTests.Load().statuses)
            {
                var fx = set.FindStatus(status.id);
                Assert.That(fx, Is.Not.Null, $"no effect for status '{status.id}'");
                Assert.That(fx.prefab, Is.Not.Null, status.id);
            }
            Assert.That(set.hitFlash, Is.Not.Null, "hit flash");
            Assert.That(set.trailMaterial, Is.Not.Null, "trail material");
            Assert.That(set.arrowMaterial, Is.Not.Null, "arrow material");
        }
    }
}
