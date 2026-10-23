using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;
using Wuxia.World;

namespace Wuxia.Player
{
    /// <summary>
    /// The Drangue Leap button: leaps in an arc onto the best marked <see cref="LeapLedge"/> in the
    /// aim direction (joystick, or facing), or dashes along the ground when none is in reach. Costs
    /// Spirit and has a cooldown. The landing is checked again with <see cref="LeapRules"/> as it
    /// happens, the same check the server makes from M4.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DrangueLeap : MonoBehaviour
    {
        enum Phase { None, Leap, Dash }

        [SerializeField] PlayerCombat combat;
        [SerializeField] PlayerSkills skills;
        [SerializeField] PlayerController mover;
        [Tooltip("World geometry that ledges are made of or that can cover them; leave out Player and Monster.")]
        [SerializeField] LayerMask surfaceMask = 1;

        /// <summary>True while a leap or dash is moving the player; normal movement waits.</summary>
        public bool IsMoving => phase != Phase.None;
        /// <summary>War banners arrive in M8; the leap is disabled while carrying one.</summary>
        public bool CarryingBanner { get; set; }
        public LeapData Rules => GameData.Skills.drangueLeap;

        CharacterController body;
        Phase phase;
        Vector3 start;
        Vector3 landingPoint;
        Vector3 dashDirection;
        LeapLedge ledge;
        float progress;

        void Awake() => body = GetComponent<CharacterController>();

        public void OnLeapButton()
        {
            if (combat.IsDead || IsMoving) return;
            if (combat.IsDisabled)
            {
                skills.Tell("You can't act right now");
                return;
            }
            if (CarryingBanner)
            {
                skills.Tell("You can't leap while carrying a banner");
                return;
            }

            switch (skills.State.CheckLeap(Time.time))
            {
                case CastBlock.NotLearned: skills.Tell("You haven't learned Drangue Leap"); return;
                case CastBlock.Cooldown: skills.Tell("Drangue Leap isn't ready"); return;
                case CastBlock.NoSpirit: skills.Tell("Not enough Spirit"); return;
            }
            if (!body.isGrounded)
            {
                skills.Tell("You need firm footing to leap");
                return;
            }

            var aim = mover.AimDirection;
            skills.State.CommitLeap(Time.time);
            combat.StopChasing();
            start = transform.position;
            progress = 0f;

            if (LeapPlanner.TryFindLanding(start, aim, LeapLedge.All, Rules, surfaceMask, out var landing))
            {
                phase = Phase.Leap;
                ledge = landing.Ledge;
                landingPoint = landing.Point;
                body.enabled = false;
                mover.FaceNow(landingPoint - start);
            }
            else
            {
                phase = Phase.Dash;
                dashDirection = aim;
                mover.FaceNow(aim);
            }
        }

        void Update()
        {
            if (phase == Phase.Leap) UpdateLeap();
            else if (phase == Phase.Dash) UpdateDash();
        }

        void UpdateLeap()
        {
            progress = Mathf.Min(1f, progress + Time.deltaTime / Rules.leapDuration);
            // A parabola on top of the straight line, peaking arcHeight above the higher end.
            var rise = landingPoint.y - start.y;
            var peak = Rules.arcHeight + Mathf.Max(0f, rise) * 0.5f;
            transform.position = Vector3.Lerp(start, landingPoint, progress)
                + Vector3.up * (4f * peak * progress * (1f - progress));
            if (progress >= 1f) Land();
        }

        void UpdateDash()
        {
            progress = Mathf.Min(1f, progress + Time.deltaTime / Rules.dashDuration);
            var speed = Rules.dashDistance / Rules.dashDuration;
            // Moving through the CharacterController keeps the dash on the ground and out of walls.
            body.Move((dashDirection * speed + Vector3.down * 2f) * Time.deltaTime);
            if (progress >= 1f) Finish();
        }

        void Land()
        {
            var check = LeapRules.Check(Rules, start.ToNumerics(), landingPoint.ToNumerics(),
                LeapPlanner.IsClearLedgeTop(ledge, landingPoint, surfaceMask));
            // A hair above the top so the CharacterController starts clear of the ledge's collider.
            transform.position = check == LeapCheck.Ok ? landingPoint + Vector3.up * 0.05f : start;
            body.enabled = true;
            if (check == LeapCheck.Ok) Debug.Log($"[Leap] Landed on {ledge.name}");
            else Debug.LogWarning($"[Leap] Landing on {ledge.name} refused ({check}); back to take-off");
            Finish();
        }

        void Finish()
        {
            phase = Phase.None;
            ledge = null;
            mover.ResetFall();
        }
    }
}
