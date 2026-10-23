using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wuxia.Data;

namespace Wuxia.Tests
{
    /// <summary>Sanity checks on the shared skills.json.</summary>
    public class SkillDataTests
    {
        const string SkillsJsonPath = "Packages/com.wuxia.gamedata/skills.json";

        public static SkillData Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(SkillsJsonPath);
            Assert.That(asset, Is.Not.Null, $"{SkillsJsonPath} not found");
            return SkillData.FromJson(asset.text);
        }

        [Test]
        public void BookLevelCosts_MatchTheDesignDoc()
        {
            var costs = Load().skillPoints.bookLevelCosts;
            Assert.That(costs, Is.EqualTo(new[] { 1, 1, 1, 2, 3, 4, 6, 9, 14, 21, 31, 47 }));
            Assert.That(costs.Sum(), Is.EqualTo(140), "140 points to max a starter book");
        }

        [Test]
        public void Forms_AreValid()
        {
            var data = Load();
            var ids = new HashSet<string>();
            foreach (var book in data.books)
            {
                Assert.That(ids.Add("book:" + book.id), $"duplicate book id '{book.id}'");
                foreach (var form in book.forms)
                {
                    var what = $"{book.id}/{form.id}";
                    Assert.That(ids.Add("form:" + form.id), $"duplicate form id '{form.id}'");
                    Assert.DoesNotThrow(() => FormData.ParseShape(form.shape), what);
                    Assert.That(form.unlockLevel, Is.InRange(1, data.MaxBookLevel), what);
                    Assert.That(form.spiritCost, Is.GreaterThan(0), what);
                    Assert.That(form.cooldown, Is.GreaterThan(0f), what);
                    Assert.That(form.castTime, Is.InRange(0.05f, form.cooldown), what);
                    Assert.That(form.minDamage, Is.GreaterThan(0), what);
                    Assert.That(form.maxDamage, Is.GreaterThanOrEqualTo(form.minDamage), what);
                    switch (form.Shape)
                    {
                        case SkillShape.Single: Assert.That(form.range, Is.GreaterThan(0f), what); break;
                        case SkillShape.Cone:
                            Assert.That(form.range, Is.GreaterThan(0f), what);
                            Assert.That(form.angle, Is.InRange(1f, 360f), what);
                            break;
                        case SkillShape.Circle: Assert.That(form.radius, Is.GreaterThan(0f), what); break;
                        case SkillShape.CircleAtTarget:
                            Assert.That(form.range, Is.GreaterThan(0f), what);
                            Assert.That(form.radius, Is.GreaterThan(0f), what);
                            break;
                    }
                    if (form.status.Any)
                    {
                        Assert.DoesNotThrow(() => data.GetStatus(form.status.id), what);
                        Assert.That(form.status.chance, Is.InRange(0.01f, 1f), what);
                        Assert.That(form.status.duration, Is.GreaterThan(0f), what);
                    }
                }
            }
        }

        [Test]
        public void EveryWeapon_HasABook_WithSingleConeAndCircleForms()
        {
            var skills = Load();
            var combat = CombatDataTests.Load();
            foreach (var weapon in combat.weapons)
            {
                var forms = skills.books.Where(b => b.weapon == weapon.id && b.requiredLevel <= 10).SelectMany(b => b.forms).ToList();
                Assert.That(forms.Count, Is.EqualTo(3), $"{weapon.id}: three forms in its weapon book");
                Assert.That(forms.Any(f => f.Shape == SkillShape.Single), $"{weapon.id}: a single-target form");
                Assert.That(forms.Any(f => f.Shape == SkillShape.Cone || f.Shape == SkillShape.CircleAtTarget), $"{weapon.id}: an area form");
                Assert.That(forms.Any(f => f.Shape == SkillShape.Circle || f.Shape == SkillShape.CircleAtTarget), $"{weapon.id}: a circle form");
                var slots = skills.books.Where(b => b.weapon == weapon.id).Sum(b => b.forms.Length);
                Assert.That(slots, Is.LessThanOrEqualTo(4), $"{weapon.id}: forms must fit the 4 skill buttons");
            }
        }

        [Test]
        public void Statuses_PoisonParalysisAndStun_Exist()
        {
            var data = Load();
            Assert.That(data.GetStatus("poison").tickInterval, Is.GreaterThan(0f));
            Assert.That(data.GetStatus("paralysis").disables);
            Assert.That(data.GetStatus("stun").disables);
            foreach (var s in data.statuses) Assert.DoesNotThrow(() => new ResistData().Get(s.resist), s.id);
        }

        [Test]
        public void DrangueLeap_IsABook_WithSaneLimits()
        {
            var data = Load();
            var leap = data.drangueLeap;
            Assert.That(data.GetBook(leap.bookId).requiredLevel, Is.EqualTo(20));
            Assert.That(leap.maxDistance, Is.GreaterThan(leap.dashDistance));
            Assert.That(leap.maxRise, Is.GreaterThan(0f));
            Assert.That(leap.aimAngle, Is.InRange(1f, 360f));
            Assert.That(leap.leapDuration, Is.GreaterThan(0f));
            Assert.That(leap.dashDuration, Is.GreaterThan(0f));
        }
    }
}
