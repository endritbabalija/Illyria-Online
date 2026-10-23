using UnityEngine;

namespace Wuxia.Combat
{
    /// <summary>
    /// Greybox stand-in for animations: an attack lunge with a weapon swing, a spin for circle forms,
    /// a squash on hit, a dazed wobble while disabled, and tipping over on death. Moves only the visual
    /// child, never the root that carries the colliders. On an animated character the Animator plays
    /// the attacks instead (<see cref="animatedAttacks"/>); the hit squash stays.
    /// </summary>
    public class CombatVisuals : MonoBehaviour
    {
        [SerializeField] Combatant combatant;
        [SerializeField] Transform visual;
        [Tooltip("Optional; swung through an arc on each attack.")]
        [SerializeField] Transform weapon;
        [Tooltip("Optional off-hand weapon pivot (the left sica); swung through the mirrored arc with the main one.")]
        [SerializeField] Transform offHandWeapon;
        [Tooltip("On for models whose Animator plays attack clips: no lunge, weapon swing or spin.")]
        [SerializeField] bool animatedAttacks;
        [SerializeField] float lungeDistance = 0.3f;
        [SerializeField] float attackDuration = 0.22f;
        [SerializeField] float hitPunch = 0.15f;
        [SerializeField] float hitDuration = 0.15f;
        [Tooltip("Off for animated models whose Animator plays a death clip instead.")]
        [SerializeField] bool tipOverOnDeath = true;
        [SerializeField] float downedDuration = 0.35f;
        [Tooltip("Lift applied when lying on its side so the mesh doesn't sink into the ground.")]
        [SerializeField] float downedLift = 0.3f;
        [Tooltip("Side-to-side tilt in degrees while stunned, paralysed or knocked down.")]
        [SerializeField] float dazedTilt = 10f;

        Vector3 restPosition;
        Vector3 restScale;
        Quaternion restRotation;
        Quaternion weaponRest;
        Quaternion offHandRest;
        float attackTime = -1f;
        float hitTime = -1f;
        float downedTime = -1f;
        float spinTime = -1f;
        float spinDuration;

        void Awake()
        {
            restPosition = visual.localPosition;
            restScale = visual.localScale;
            restRotation = visual.localRotation;
            if (weapon != null) weaponRest = weapon.localRotation;
            if (offHandWeapon != null) offHandRest = offHandWeapon.localRotation;
        }

        void OnEnable() => combatant.Damaged += OnDamaged;
        void OnDisable() => combatant.Damaged -= OnDamaged;

        void OnDamaged(AttackResult result)
        {
            if (result.Hit) hitTime = 0f;
        }

        public void PlayAttack()
        {
            if (!animatedAttacks) attackTime = 0f;
        }

        /// <summary>One full turn over <paramref name="duration"/> seconds, for forms that hit all around.</summary>
        public void PlaySpin(float duration)
        {
            if (animatedAttacks) return;
            spinTime = 0f;
            spinDuration = Mathf.Max(0.1f, duration);
        }

        public void SetDowned()
        {
            if (tipOverOnDeath) downedTime = 0f;
        }

        public void ResetPose()
        {
            attackTime = hitTime = downedTime = spinTime = -1f;
            visual.localPosition = restPosition;
            visual.localScale = restScale;
            visual.localRotation = restRotation;
            if (weapon != null) weapon.localRotation = weaponRest;
            if (offHandWeapon != null) offHandWeapon.localRotation = offHandRest;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            var position = restPosition;
            var scale = restScale;
            var rotation = restRotation;
            var weaponRotation = weaponRest;
            var offHandRotation = offHandRest;

            if (attackTime >= 0f)
            {
                attackTime += dt;
                var k = attackTime / attackDuration;
                if (k >= 1f) attackTime = -1f;
                else
                {
                    position += Vector3.forward * (lungeDistance * Mathf.Sin(k * Mathf.PI));
                    var swing = Quaternion.Euler(Mathf.Lerp(-70f, 80f, k), 0f, 0f);
                    weaponRotation = weaponRest * swing;
                    offHandRotation = offHandRest * swing;
                }
            }

            if (hitTime >= 0f)
            {
                hitTime += dt;
                var k = hitTime / hitDuration;
                if (k >= 1f) hitTime = -1f;
                else scale = Vector3.Scale(restScale, new Vector3(1f + hitPunch * (1f - k), 1f - hitPunch * (1f - k), 1f + hitPunch * (1f - k)));
            }

            if (spinTime >= 0f)
            {
                spinTime += dt;
                var k = spinTime / spinDuration;
                if (k >= 1f) spinTime = -1f;
                else rotation = Quaternion.Euler(0f, 360f * k, 0f) * rotation;
            }

            if (downedTime < 0f && combatant.IsDisabled)
                rotation = restRotation * Quaternion.Euler(0f, 0f, dazedTilt * Mathf.Sin(Time.time * 14f));

            if (downedTime >= 0f)
            {
                downedTime += dt;
                var k = Mathf.Clamp01(downedTime / downedDuration);
                rotation = restRotation * Quaternion.Euler(0f, 0f, 90f * k);
                position += Vector3.up * (downedLift * k);
            }

            visual.localPosition = position;
            visual.localScale = scale;
            visual.localRotation = rotation;
            if (weapon != null) weapon.localRotation = weaponRotation;
            if (offHandWeapon != null) offHandWeapon.localRotation = offHandRotation;
        }
    }
}
