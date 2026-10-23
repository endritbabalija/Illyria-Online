using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.Player
{
    /// <summary>
    /// Drives the character model's Animator. Locomotion comes from how fast the player actually moves
    /// on the ground: a 1D blend from idle (0 m/s) to run, with the run sped up past its authored speed
    /// so the feet keep pace. Measures movement itself, so joystick, auto-approach and dashes all
    /// animate alike.
    /// One-shot actions from <see cref="PlayerActionSet"/> play on two action layers with the same
    /// states: the whole body while standing, the upper body while the legs run, cross-faded by how
    /// fast the player moves. Basic attacks cycle through <c>&lt;weapon&gt;_basic_&lt;n&gt;</c>, skill
    /// forms play the state named after the form (else a basic attack), each sped up to fit the attack
    /// interval or cast time. Hits flinch on their own upper-body layer; death holds until the revive.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int RunPlaybackId = Animator.StringToHash("RunPlayback");
        static readonly int ActionSpeedId = Animator.StringToHash("ActionSpeed");

        // Layer order the builder creates on the controller.
        const int FullLayer = 1;
        const int UpperLayer = 2;
        const int FlinchLayer = 3;

        [SerializeField] Animator animator;
        [SerializeField] PlayerActionSet actions;
        [SerializeField] PlayerCombat combat;
        [SerializeField] PlayerSkills skills;
        [SerializeField] DrangueLeap leap;
        [Tooltip("Ground speed in m/s the run clip was authored at (Quaternius Jog_Fwd_Loop: about 5).")]
        [SerializeField] float runClipSpeed = 5f;
        [Tooltip("Seconds to ease the Speed parameter towards the measured speed.")]
        [SerializeField] float damping = 0.08f;
        [Tooltip("Run playback never goes faster than this, e.g. mid-leap.")]
        [SerializeField] float maxRunPlayback = 1.5f;
        [Tooltip("Basic attacks fill this share of the attack interval, leaving a beat before the next.")]
        [SerializeField] float attackFill = 0.85f;
        [Tooltip("Actions never play slower or faster than this to fit their time.")]
        [SerializeField] float minActionSpeed = 0.75f;
        [SerializeField] float maxActionSpeed = 2.2f;
        [Tooltip("Seconds to blend into an action.")]
        [SerializeField] float crossFade = 0.08f;
        [Tooltip("Seconds to blend out of an action back to locomotion.")]
        [SerializeField] float fadeOut = 0.18f;
        [Tooltip("Ground speed in m/s above which actions only move the upper body.")]
        [SerializeField] float runningSpeed = 0.6f;
        [Tooltip("Weight of the flinch over an attack in progress (full weight when idle).")]
        [SerializeField] float flinchOverAction = 0.4f;
        [Tooltip("Basic attacks restart from the first swing after this many attack intervals without one.")]
        [SerializeField] float comboResetIntervals = 2f;

        Vector3 lastPosition;
        float groundSpeed;
        float upperBlend;

        Combatant combatant;
        PlayerActionSet.Action current;
        ActionBody body;      // of the current action, kept while it fades out
        float actionEnd;      // Time.time the current action (with its chain) finishes; +inf while held
        float actionWeight;
        float flinchEnd;
        float flinchLength;
        float flinchWeight;

        string comboWeapon;
        int comboIndex;
        float comboResetAt;

        /// <summary>How far an attack or skill form is blended in (0..1); hit, death, leap and potions don't count.</summary>
        public float AttackWeight => body == ActionBody.Auto ? actionWeight : 0f;

        void Awake() => combatant = combat.GetComponent<Combatant>();

        void OnEnable()
        {
            lastPosition = transform.position;
            combat.Attacked += OnBasicAttack;
            combatant.Damaged += OnDamaged;
            combatant.Died += OnDied;
            combat.Revived += OnRevived;
            combat.PotionUsed += OnPotionUsed;
            skills.FormCast += OnFormCast;
            leap.Started += OnLeapStarted;
            leap.Landed += OnLeapLanded;
        }

        void OnDisable()
        {
            combat.Attacked -= OnBasicAttack;
            combatant.Damaged -= OnDamaged;
            combatant.Died -= OnDied;
            combat.Revived -= OnRevived;
            combat.PotionUsed -= OnPotionUsed;
            skills.FormCast -= OnFormCast;
            leap.Started -= OnLeapStarted;
            leap.Landed -= OnLeapLanded;
        }

        /// <summary>Drives another model's Animator, e.g. after a character switch.</summary>
        public void SetAnimator(Animator target)
        {
            animator = target;
            current = null;
            actionWeight = flinchWeight = 0f;
            comboWeapon = null;
            ApplyWeights();
        }

        void OnBasicAttack()
        {
            var weapon = combat.Weapon.id;
            var interval = combat.Stats.Attack.interval;
            if (weapon != comboWeapon || Time.time >= comboResetAt) comboIndex = 0;
            comboWeapon = weapon;
            comboResetAt = Time.time + interval * comboResetIntervals;

            var action = actions.Find($"{weapon}_basic_{comboIndex}");
            if (action == null && comboIndex > 0) action = actions.Find($"{weapon}_basic_{comboIndex = 0}");
            comboIndex++;
            Play(action, interval * attackFill);
        }

        void OnFormCast(FormData form)
        {
            var action = actions.Find(form.id);
            if (action != null) Play(action, form.castTime);
            else OnBasicAttack();
        }

        void OnDamaged(AttackResult result)
        {
            if (!result.Hit || combat.IsDead) return;
            var hit = actions.Find("hit");
            if (hit == null || hit.resolved == null) return;
            animator.Play(hit.state, FlinchLayer, 0f);
            flinchLength = hit.resolved.length / Mathf.Max(0.01f, hit.speed);
            flinchEnd = Time.time + flinchLength;
        }

        void OnDied(Combatant killer)
        {
            flinchEnd = 0f;
            Play(actions.Find("death"), 0f);
        }

        void OnRevived() => Play(actions.Find("revive"), 0f);

        void OnPotionUsed() => Play(actions.Find("drink"), 0f);

        void OnLeapStarted(bool ledge)
        {
            // A dash is over in a blink; the sped-up run reads better than any clip.
            if (ledge) Play(actions.Find("leap"), 0f);
        }

        void OnLeapLanded() => Play(actions.Find("leap_land"), 0f);

        /// <summary>
        /// Cross-fades both action layers into <paramref name="action"/>. With a duration, the action and
        /// the states chained after it are sped up (or slowed) to fill it; otherwise each plays at its
        /// own speed.
        /// </summary>
        void Play(PlayerActionSet.Action action, float duration)
        {
            if (action == null || animator == null) return;
            var natural = ChainLength(action);
            var multiplier = duration > 0f && natural > 0f
                ? Mathf.Clamp(natural / duration, minActionSpeed, maxActionSpeed)
                : 1f;
            animator.SetFloat(ActionSpeedId, multiplier);
            animator.CrossFadeInFixedTime(action.state, crossFade, FullLayer, 0f);
            animator.CrossFadeInFixedTime(action.state, crossFade, UpperLayer, 0f);
            current = action;
            body = action.body;
            actionEnd = IsHeld(action) ? float.PositiveInfinity : Time.time + natural / multiplier;
        }

        /// <summary>Seconds the action and everything chained after it take at their own speeds.</summary>
        float ChainLength(PlayerActionSet.Action action)
        {
            var total = 0f;
            for (var i = 0; action != null && i < 8; i++)
            {
                if (action.resolved != null) total += action.resolved.length / Mathf.Max(0.01f, action.speed);
                if (action.hold) break;
                action = actions.Find(action.next);
            }
            return total;
        }

        bool IsHeld(PlayerActionSet.Action action)
        {
            for (var i = 0; action != null && i < 8; i++)
            {
                if (action.hold) return true;
                action = actions.Find(action.next);
            }
            return false;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f || animator == null) return;

            var delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;
            // Clamp so a teleport (reviving at the Han) reads as running, not as a huge spike.
            groundSpeed = Mathf.Min(delta.magnitude / dt, runClipSpeed * maxRunPlayback);

            animator.SetFloat(SpeedId, groundSpeed, damping, dt);
            animator.SetFloat(RunPlaybackId, Mathf.Clamp(groundSpeed / runClipSpeed, 1f, maxRunPlayback));

            var active = current != null && Time.time < actionEnd;
            if (!active) current = null;
            actionWeight = Mathf.MoveTowards(actionWeight, active ? 1f : 0f, dt / (active ? crossFade : fadeOut));
            var running = groundSpeed > runningSpeed ? 1f : 0f;
            upperBlend = Mathf.MoveTowards(upperBlend, running, dt / 0.15f);

            // Rise fast, then fade out over the clip's second half.
            var flinchTarget = 0f;
            if (Time.time < flinchEnd)
            {
                var left = (flinchEnd - Time.time) / Mathf.Max(0.01f, flinchLength);
                flinchTarget = Mathf.Clamp01(left * 2f) * (actionWeight > 0.5f ? flinchOverAction : 1f);
            }
            flinchWeight = Mathf.MoveTowards(flinchWeight, flinchTarget, dt / 0.06f);

            ApplyWeights();
        }

        void ApplyWeights()
        {
            if (animator == null || animator.layerCount <= FlinchLayer) return;
            var upper = body == ActionBody.Upper ? 1f : body == ActionBody.Full ? 0f : upperBlend;
            animator.SetLayerWeight(FullLayer, actionWeight * (1f - upper));
            animator.SetLayerWeight(UpperLayer, actionWeight * upper);
            animator.SetLayerWeight(FlinchLayer, flinchWeight);
        }
    }
}
