using UnityEngine;

namespace Wuxia.Player
{
    /// <summary>
    /// Plays one-shot actions from a <see cref="PlayerActionSet"/> on a character model's Animator:
    /// two action layers with the same states, the whole body while standing and the upper body while
    /// the legs run (cross-faded by ground speed), plus a flinch layer. Basic attacks cycle through
    /// <c>&lt;weapon&gt;_basic_&lt;n&gt;</c>, skill forms play the state named after the form (else a
    /// basic attack), each sped up to fit the attack interval or cast time; held actions (death) last
    /// until the next one. Shared by the player (<see cref="PlayerAnimator"/>) and other players.
    /// </summary>
    public class CharacterActions
    {
        static readonly int ActionSpeedId = Animator.StringToHash("ActionSpeed");

        // Layer order the builder creates on the controller.
        const int FullLayer = 1;
        const int UpperLayer = 2;
        const int FlinchLayer = 3;

        /// <summary>Tuning, the same for every character (PlayerAnimator's inspector fields).</summary>
        public struct Tuning
        {
            public float attackFill, minActionSpeed, maxActionSpeed, crossFade, fadeOut, runningSpeed, flinchOverAction, comboResetIntervals;
        }

        readonly PlayerActionSet actions;
        readonly Tuning tuning;
        Animator animator;

        PlayerActionSet.Action current;
        ActionBody body;      // of the current action, kept while it fades out
        float actionEnd;      // Time.time the current action (with its chain) finishes; +inf while held
        float actionWeight;
        float upperBlend;
        float flinchEnd;
        float flinchLength;
        float flinchWeight;

        string comboWeapon;
        int comboIndex;
        float comboResetAt;

        public CharacterActions(PlayerActionSet actions, Tuning tuning, Animator animator)
        {
            this.actions = actions;
            this.tuning = tuning;
            this.animator = animator;
        }

        /// <summary>How far an attack or skill form is blended in (0..1); hit, death, leap and potions don't count.</summary>
        public float AttackWeight => body == ActionBody.Auto ? actionWeight : 0f;

        /// <summary>Drives another model's Animator, e.g. after a character switch.</summary>
        public void SetAnimator(Animator target)
        {
            animator = target;
            current = null;
            actionWeight = flinchWeight = 0f;
            comboWeapon = null;
            ApplyWeights();
        }

        /// <summary>The next swing of this weapon's basic-attack cycle, filling most of the attack interval.</summary>
        public void PlayBasic(string weaponId, float interval)
        {
            if (weaponId != comboWeapon || Time.time >= comboResetAt) comboIndex = 0;
            comboWeapon = weaponId;
            comboResetAt = Time.time + interval * tuning.comboResetIntervals;

            var action = actions.Find($"{weaponId}_basic_{comboIndex}");
            if (action == null && comboIndex > 0) action = actions.Find($"{weaponId}_basic_{comboIndex = 0}");
            comboIndex++;
            Play(action, interval * tuning.attackFill);
        }

        /// <summary>A skill form's own state, fitted to its cast time; a basic attack if it has none.</summary>
        public void PlayForm(string formId, float castTime, string weaponId, float interval)
        {
            var action = actions.Find(formId);
            if (action != null) Play(action, castTime);
            else PlayBasic(weaponId, interval);
        }

        /// <summary>A short upper-body flinch over whatever plays.</summary>
        public void Flinch()
        {
            var hit = actions.Find("hit");
            if (hit == null || hit.resolved == null || animator == null) return;
            animator.Play(hit.state, FlinchLayer, 0f);
            flinchLength = hit.resolved.length / Mathf.Max(0.01f, hit.speed);
            flinchEnd = Time.time + flinchLength;
        }

        /// <summary>An action at its own speed: death (held), revive, drink, leap, leap_land.</summary>
        public void Play(string state)
        {
            if (state == "death") flinchEnd = 0f;
            Play(actions.Find(state), 0f);
        }

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
                ? Mathf.Clamp(natural / duration, tuning.minActionSpeed, tuning.maxActionSpeed)
                : 1f;
            animator.SetFloat(ActionSpeedId, multiplier);
            animator.CrossFadeInFixedTime(action.state, tuning.crossFade, FullLayer, 0f);
            animator.CrossFadeInFixedTime(action.state, tuning.crossFade, UpperLayer, 0f);
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

        /// <summary>Eases the layer weights; call once a frame with the model's ground speed.</summary>
        public void Update(float groundSpeed, float dt)
        {
            if (dt <= 0f || animator == null) return;
            var active = current != null && Time.time < actionEnd;
            if (!active) current = null;
            actionWeight = Mathf.MoveTowards(actionWeight, active ? 1f : 0f, dt / (active ? tuning.crossFade : tuning.fadeOut));
            var running = groundSpeed > tuning.runningSpeed ? 1f : 0f;
            upperBlend = Mathf.MoveTowards(upperBlend, running, dt / 0.15f);

            // Rise fast, then fade out over the clip's second half.
            var flinchTarget = 0f;
            if (Time.time < flinchEnd)
            {
                var left = (flinchEnd - Time.time) / Mathf.Max(0.01f, flinchLength);
                flinchTarget = Mathf.Clamp01(left * 2f) * (actionWeight > 0.5f ? tuning.flinchOverAction : 1f);
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
