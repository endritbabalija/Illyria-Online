using System;
using UnityEngine;

namespace Wuxia.Combat
{
    /// <summary>Where a form's burst goes off.</summary>
    public enum BurstAt
    {
        /// <summary>On the ground under the caster, turned to the way the form faces.</summary>
        Caster,
        /// <summary>On the ground under the locked target (the caster if there is none).</summary>
        Target,
    }

    /// <summary>What travels out of a form after its bursts.</summary>
    public enum FormMotion
    {
        None,
        /// <summary>One arrow at the locked target, homing; its impact plays where it lands.</summary>
        Arrow,
        /// <summary><see cref="CombatEffectSet.FormEffect.count"/> arrows fanned across the form's cone, out to its range.</summary>
        Volley,
        /// <summary><see cref="CombatEffectSet.FormEffect.count"/> arrows falling at random spots inside the form's circle at the target.</summary>
        Rain,
        /// <summary>Impacts marching out across the form's cone in rows, nearest first.</summary>
        Wave,
    }

    /// <summary>
    /// Placeholder visual effects for combat: what each skill form, weapon and status shows, mostly
    /// Cartoon FX Remaster prefabs recoloured per form. Presentation only, so it lives with the client,
    /// not in the shared game data; <see cref="CombatEffects"/> plays it.
    /// </summary>
    [CreateAssetMenu(menuName = "Wuxia/Combat Effect Set")]
    public class CombatEffectSet : ScriptableObject
    {
        [Serializable]
        public class Burst
        {
            [Tooltip("One-shot particle prefab (its lights, camera shake and self-destroy are stripped when pooled).")]
            public GameObject prefab;
            public BurstAt at;
            [Tooltip("Seconds after the cast, to meet the animation's swing.")]
            public float delay;
            [Tooltip("Metres above the ground point.")]
            public float height;
            [Tooltip("Rotation relative to the form's facing (x 90 lays a vertical effect flat).")]
            public Vector3 euler;
            public float scale = 1f;
            [Tooltip("Also multiply the scale by the form's radius (circles) or range (cones).")]
            public bool scaleToArea;
            [Tooltip("Recolour towards this; alpha is how far (0 keeps the prefab's own colours).")]
            public Color tint;
        }

        [Serializable]
        public class FormEffect
        {
            [Tooltip("Skill form id from skills.json.")]
            public string form;
            [Tooltip("The form's colour: blade trails, arrow trails and hit flashes.")]
            public Color color = Color.white;
            [Tooltip("Ribbon behind the blade(s) while the form plays.")]
            public bool bladeTrail;
            public Burst[] bursts;
            public FormMotion motion;
            [Tooltip("Arrows in a volley or rain; rows in a wave.")]
            public int count = 1;
            [Tooltip("Where each arrow or wave impact lands.")]
            public GameObject impact;
            public float impactScale = 1f;
            [Tooltip("Recolour of the impact; alpha is how far.")]
            public Color impactTint;
        }

        [Serializable]
        public class WeaponEffect
        {
            [Tooltip("Weapon type id from combat.json.")]
            public string weapon;
            [Tooltip("Grip to blade tip along the weapon pivot's +Z, in metres; 0 for no blade trail.")]
            public float bladeLength;
            [Tooltip("Paired weapons (the sicas): the off-hand blade trails too.")]
            public bool offHand;
            [Tooltip("Basic attacks shoot an arrow at the target.")]
            public bool arrows;
            [Tooltip("Arrow trail of basic attacks.")]
            public Color color = Color.white;
        }

        [Serializable]
        public class StatusEffect
        {
            [Tooltip("Status id from skills.json.")]
            public string status;
            public GameObject prefab;
            public float scale = 1f;
            public Color tint;
        }

        public FormEffect[] forms;
        public WeaponEffect[] weapons;
        [Tooltip("Plays once on a target as a status lands.")]
        public StatusEffect[] statuses;

        [Header("Hits")]
        [Tooltip("Small burst on whoever is hit, recoloured per kind of hit.")]
        public GameObject hitFlash;
        public float hitFlashScale = 0.35f;
        public float critFlashScale = 0.55f;
        public Color basicHit = new Color(1f, 1f, 1f, 0.6f);
        public Color critHit = new Color(1f, 0.8f, 0.2f, 1f);
        public Color takenHit = new Color(1f, 0.3f, 0.25f, 1f);
        public Color poisonHit = new Color(0.45f, 0.95f, 0.35f, 1f);
        [Tooltip("Skill hits by anyone without a known form.")]
        public Color skillHit = new Color(0.55f, 0.85f, 1f, 1f);

        [Header("Shared")]
        [Tooltip("Additive soft ribbon for blade and arrow trails.")]
        public Material trailMaterial;
        public Material arrowMaterial;

        public FormEffect FindForm(string id)
        {
            if (forms != null) foreach (var f in forms) if (f.form == id) return f;
            return null;
        }

        public WeaponEffect FindWeapon(string id)
        {
            if (weapons != null) foreach (var w in weapons) if (w.weapon == id) return w;
            return null;
        }

        public StatusEffect FindStatus(string id)
        {
            if (statuses != null) foreach (var s in statuses) if (s.status == id) return s;
            return null;
        }
    }
}
