using UnityEngine;

namespace Wuxia.Combat
{
    /// <summary>
    /// Greybox stand-in for animations: an attack lunge with a weapon swing, a squash on hit, and
    /// tipping over on death. Moves only the visual child, never the root that carries the colliders.
    /// </summary>
    public class CombatVisuals : MonoBehaviour
    {
        [SerializeField] Combatant combatant;
        [SerializeField] Transform visual;
        [Tooltip("Optional; swung through an arc on each attack.")]
        [SerializeField] Transform weapon;
        [SerializeField] float lungeDistance = 0.3f;
        [SerializeField] float attackDuration = 0.22f;
        [SerializeField] float hitPunch = 0.15f;
        [SerializeField] float hitDuration = 0.15f;
        [SerializeField] float downedDuration = 0.35f;
        [Tooltip("Lift applied when lying on its side so the mesh doesn't sink into the ground.")]
        [SerializeField] float downedLift = 0.3f;

        Vector3 restPosition;
        Vector3 restScale;
        Quaternion restRotation;
        Quaternion weaponRest;
        float attackTime = -1f;
        float hitTime = -1f;
        float downedTime = -1f;

        void Awake()
        {
            restPosition = visual.localPosition;
            restScale = visual.localScale;
            restRotation = visual.localRotation;
            if (weapon != null) weaponRest = weapon.localRotation;
        }

        void OnEnable() => combatant.Damaged += OnDamaged;
        void OnDisable() => combatant.Damaged -= OnDamaged;

        void OnDamaged(AttackResult result)
        {
            if (result.Hit) hitTime = 0f;
        }

        public void PlayAttack() => attackTime = 0f;

        public void SetDowned() => downedTime = 0f;

        public void ResetPose()
        {
            attackTime = hitTime = downedTime = -1f;
            visual.localPosition = restPosition;
            visual.localScale = restScale;
            visual.localRotation = restRotation;
            if (weapon != null) weapon.localRotation = weaponRest;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            var position = restPosition;
            var scale = restScale;
            var rotation = restRotation;
            var weaponRotation = weaponRest;

            if (attackTime >= 0f)
            {
                attackTime += dt;
                var k = attackTime / attackDuration;
                if (k >= 1f) attackTime = -1f;
                else
                {
                    position += Vector3.forward * (lungeDistance * Mathf.Sin(k * Mathf.PI));
                    weaponRotation = weaponRest * Quaternion.Euler(Mathf.Lerp(-70f, 80f, k), 0f, 0f);
                }
            }

            if (hitTime >= 0f)
            {
                hitTime += dt;
                var k = hitTime / hitDuration;
                if (k >= 1f) hitTime = -1f;
                else scale = Vector3.Scale(restScale, new Vector3(1f + hitPunch * (1f - k), 1f - hitPunch * (1f - k), 1f + hitPunch * (1f - k)));
            }

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
        }
    }
}
