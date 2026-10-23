using System;
using UnityEngine;

namespace Wuxia.Data
{
    // Mirrors shared/gamedata/skills.json. Field names match the JSON keys exactly so the Nakama
    // server can read the same file in M4.

    public enum SkillShape
    {
        /// <summary>The locked target only.</summary>
        Single,
        /// <summary>A wedge in front of the caster: <c>range</c> long, <c>angle</c> degrees wide.</summary>
        Cone,
        /// <summary>A circle of <c>radius</c> around the caster.</summary>
        Circle,
        /// <summary>A circle of <c>radius</c> around the locked target, which must be within <c>range</c>.</summary>
        CircleAtTarget,
    }

    [Serializable]
    public class SkillPointData
    {
        /// <summary>Kills that fill the skill-point meter once.</summary>
        public int killsPerPoint;
        /// <summary>Points to reach each book level: entry 0 buys level 1, entry 11 buys level 12.</summary>
        public int[] bookLevelCosts;
        /// <summary>Extra skill damage per book level above 1 (0.08 = +8%).</summary>
        public float damagePerBookLevel;
    }

    [Serializable]
    public class StatusData
    {
        public string id;
        /// <summary>Shown over the target and in its frame, e.g. "Stunned".</summary>
        public string name;
        /// <summary>Can't move, attack or use skills while it lasts.</summary>
        public bool disables;
        /// <summary>Seconds between damage ticks; 0 for statuses that don't deal damage.</summary>
        public float tickInterval;
        /// <summary>Which resistance (poison, paralysis, stun) lowers the chance of it landing.</summary>
        public string resist;
    }

    /// <summary>A status a form may apply to everything it hits. An empty <c>id</c> means none.</summary>
    [Serializable]
    public class StatusApplication
    {
        public string id;
        public float chance;
        public float duration;
        public int tickDamage;

        public bool Any => !string.IsNullOrEmpty(id);
    }

    [Serializable]
    public class FormData
    {
        public string id;
        public string name;
        /// <summary>Book level at which this form unlocks.</summary>
        public int unlockLevel;
        /// <summary>single, cone, circle or circleAtTarget; see <see cref="Shape"/>.</summary>
        public string shape;
        /// <summary>Reach to the target (single, circleAtTarget) or the cone's length, in metres.</summary>
        public float range;
        /// <summary>Circle radius in metres.</summary>
        public float radius;
        /// <summary>Full cone width in degrees.</summary>
        public float angle;
        public int spiritCost;
        public float cooldown;
        /// <summary>How long the form plays; one more form can be queued meanwhile.</summary>
        public float castTime;
        public int minDamage;
        public int maxDamage;
        public StatusApplication status;

        public SkillShape Shape => ParseShape(shape);

        /// <summary>Single and circle-at-target forms need a locked target within range.</summary>
        public bool NeedsTarget => Shape == SkillShape.Single || Shape == SkillShape.CircleAtTarget;

        public static SkillShape ParseShape(string value)
        {
            switch (value)
            {
                case "single": return SkillShape.Single;
                case "cone": return SkillShape.Cone;
                case "circle": return SkillShape.Circle;
                case "circleAtTarget": return SkillShape.CircleAtTarget;
                default: throw new FormatException($"Unknown skill shape '{value}'");
            }
        }
    }

    [Serializable]
    public class BookData
    {
        public string id;
        public string name;
        /// <summary>Weapon whose skill buttons show this book's forms; empty for books like Drangue Leap.</summary>
        public string weapon;
        /// <summary>Character level needed to read the book (enforced once levels exist, M7).</summary>
        public int requiredLevel;
        public FormData[] forms;
    }

    [Serializable]
    public class LeapData
    {
        /// <summary>The book that teaches the leap.</summary>
        public string bookId;
        public int spiritCost;
        public float cooldown;
        /// <summary>Furthest landing spot, measured on the ground plane.</summary>
        public float maxDistance;
        /// <summary>Highest a landing spot may be above the take-off point.</summary>
        public float maxRise;
        /// <summary>Lowest a landing spot may be below the take-off point.</summary>
        public float maxDrop;
        /// <summary>Full width, in degrees, of the wedge around the aim direction searched for ledges.</summary>
        public float aimAngle;
        /// <summary>Landing spots keep this far from a ledge's edges.</summary>
        public float landingInset;
        public float leapDuration;
        /// <summary>How far the arc rises above the higher of take-off and landing.</summary>
        public float arcHeight;
        /// <summary>With no ledge in reach the leap becomes a ground dash this long.</summary>
        public float dashDistance;
        public float dashDuration;
    }

    /// <summary>Offline test settings until books drop as items (M3) and progress is saved (M4).</summary>
    [Serializable]
    public class SkillDevData
    {
        /// <summary>Start knowing every book your character can use, as if read.</summary>
        public bool learnAllBooks;
        public int startingSkillPoints;
    }

    [Serializable]
    public class SkillData
    {
        public int version;
        public SkillPointData skillPoints;
        public StatusData[] statuses;
        public BookData[] books;
        public LeapData drangueLeap;
        public SkillDevData dev;

        public BookData GetBook(string id)
        {
            foreach (var b in books)
                if (b.id == id) return b;
            throw new ArgumentException($"No book '{id}' in skill data");
        }

        /// <summary>The form with this id in any book, or null.</summary>
        public FormData FindForm(string id)
        {
            foreach (var b in books)
                foreach (var f in b.forms)
                    if (f.id == id) return f;
            return null;
        }

        public StatusData GetStatus(string id)
        {
            foreach (var s in statuses)
                if (s.id == id) return s;
            throw new ArgumentException($"No status '{id}' in skill data");
        }

        /// <summary>Points needed to raise a book from <paramref name="level"/> to the next; -1 at max level.</summary>
        public int LevelUpCost(int level)
        {
            var costs = skillPoints.bookLevelCosts;
            return level >= 0 && level < costs.Length ? costs[level] : -1;
        }

        public int MaxBookLevel => skillPoints.bookLevelCosts.Length;

        public static SkillData FromJson(string json)
        {
            var data = JsonUtility.FromJson<SkillData>(json);
            if (data?.skillPoints?.bookLevelCosts == null || data.statuses == null || data.books == null || data.drangueLeap == null)
                throw new FormatException("skills.json is missing 'skillPoints', 'statuses', 'books' or 'drangueLeap'");
            if (data.dev == null) data.dev = new SkillDevData();
            return data;
        }
    }
}
