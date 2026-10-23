using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.Player
{
    /// <summary>
    /// Whatever drives a held weapon's pose: how far an attack is blended in (0..1), read by
    /// <see cref="WeaponActionGrip"/>. The player's <see cref="PlayerAnimator"/> and other players.
    /// </summary>
    public interface IAttackPose
    {
        float AttackWeight { get; }
    }

    /// <summary>
    /// Drives the character model's Animator. Locomotion comes from how fast the player actually moves
    /// on the ground: a 1D blend from idle (0 m/s) to run, with the run sped up past its authored speed
    /// so the feet keep pace. Measures movement itself, so joystick, auto-approach and dashes all
    /// animate alike.
    /// One-shot actions from <see cref="PlayerActionSet"/> (attacks, forms, hits, death, revive,
    /// potions, leaps) play through <see cref="CharacterActions"/>, fed by the player's events.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour, IAttackPose
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int RunPlaybackId = Animator.StringToHash("RunPlayback");

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
        Combatant combatant;
        CharacterActions play;

        /// <summary>How far an attack or skill form is blended in (0..1); hit, death, leap and potions don't count.</summary>
        public float AttackWeight => play != null ? play.AttackWeight : 0f;

        /// <summary>Ground speed in m/s the run clip matches.</summary>
        public float RunClipSpeed => runClipSpeed;

        void Awake()
        {
            combatant = combat.GetComponent<Combatant>();
            play = CreateActions(animator);
        }

        /// <summary>An action player with this one's clips and tuning for another model (other players).</summary>
        public CharacterActions CreateActions(Animator target) => new CharacterActions(actions, new CharacterActions.Tuning
        {
            attackFill = attackFill,
            minActionSpeed = minActionSpeed,
            maxActionSpeed = maxActionSpeed,
            crossFade = crossFade,
            fadeOut = fadeOut,
            runningSpeed = runningSpeed,
            flinchOverAction = flinchOverAction,
            comboResetIntervals = comboResetIntervals,
        }, target);

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
            if (play == null) play = CreateActions(target);
            else play.SetAnimator(target);
        }

        void OnBasicAttack(Combatant target) => play.PlayBasic(combat.Weapon.id, combat.Stats.Attack.interval);

        void OnFormCast(FormData form) => play.PlayForm(form.id, form.castTime, combat.Weapon.id, combat.Stats.Attack.interval);

        void OnDamaged(AttackResult result)
        {
            if (result.Hit && !combat.IsDead) play.Flinch();
        }

        void OnDied(Combatant killer) => play.Play("death");

        void OnRevived() => play.Play("revive");

        void OnPotionUsed() => play.Play("drink");

        void OnLeapStarted(bool ledge)
        {
            // A dash is over in a blink; the sped-up run reads better than any clip.
            if (ledge) play.Play("leap");
        }

        void OnLeapLanded() => play.Play("leap_land");

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f || animator == null) return;

            var delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;
            // Clamp so a teleport (reviving at the Han) reads as running, not as a huge spike.
            var groundSpeed = Mathf.Min(delta.magnitude / dt, runClipSpeed * maxRunPlayback);

            animator.SetFloat(SpeedId, groundSpeed, damping, dt);
            animator.SetFloat(RunPlaybackId, Mathf.Clamp(groundSpeed / runClipSpeed, 1f, maxRunPlayback));
            play.Update(groundSpeed, dt);
        }
    }
}
