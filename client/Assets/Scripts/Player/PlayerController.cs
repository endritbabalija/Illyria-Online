using System;
using UnityEngine;
using Wuxia.Data;
using Wuxia.Input;

namespace Wuxia.Player
{
    /// <summary>
    /// Camera-relative joystick movement. With no joystick input it runs to an engaged target until in
    /// attack range, or to a tapped NPC (<see cref="RunTo"/>); touching the joystick cancels either. Hands over to <see cref="DrangueLeap"/> while
    /// a leap or dash is moving the player, and stands still while stunned or paralysed.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] TouchControls controls;
        [SerializeField] Transform cameraTransform;
        [SerializeField] PlayerCombat combat;
        [SerializeField] DrangueLeap leap;
        [SerializeField] float gravity = -20f;
        [SerializeField] float deadZone = 0.1f;
        [Tooltip("Auto-approach stops at this fraction of attack range so the first swing lands.")]
        [SerializeField] float approachFraction = 0.85f;

        CharacterController body;
        PlayerData data;
        float verticalSpeed;
        bool runningTo;
        Vector3 runPoint;
        float runStop;
        Action runArrived;

        /// <summary>
        /// Where the player means to go: the joystick direction (camera-relative) while it's held,
        /// otherwise the way the character faces. Unit length, on the ground plane.
        /// </summary>
        public Vector3 AimDirection
        {
            get
            {
                var input = controls.Move;
                if (input.magnitude > deadZone)
                    return Flat(Flat(cameraTransform.forward) * input.y + Flat(cameraTransform.right) * input.x);
                return Flat(transform.forward);
            }
        }

        void Awake() => body = GetComponent<CharacterController>();

        void Start() => data = GameData.Combat.player;

        void Update()
        {
            if (leap.IsMoving) return;

            var dt = Time.deltaTime;
            var move = Vector3.zero;
            var target = combat.Target;
            var canAct = !combat.IsDead && !combat.IsDisabled;

            if (canAct)
            {
                var input = controls.Move;
                if (input.magnitude > deadZone)
                {
                    move = Flat(cameraTransform.forward) * input.y + Flat(cameraTransform.right) * input.x;
                    combat.StopChasing();
                    CancelRunTo();
                }
                else if (runningTo && !combat.Chasing)
                {
                    var to = runPoint - transform.position;
                    to.y = 0f;
                    if (to.magnitude <= runStop)
                    {
                        var arrived = runArrived;
                        CancelRunTo();
                        arrived?.Invoke();
                    }
                    else move = Flat(to);
                }
                else if (combat.Chasing && target != null && !combat.InAttackRange(target, approachFraction))
                {
                    move = Flat(target.transform.position - transform.position);
                }
            }

            if (body.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += gravity * dt;
            body.Move((move * data.moveSpeed + Vector3.up * verticalSpeed) * dt);

            // Face the target while it's in reach (strafing works), otherwise the way we're moving.
            var facing = move;
            if (combat.Chasing || !canAct) CancelRunTo();
            if (canAct && target != null && combat.InAttackRange(target))
                facing = Flat(target.transform.position - transform.position);
            if (facing.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), data.turnSpeed * dt);
        }

        /// <summary>
        /// Runs to <paramref name="point"/> (ground plane) until within <paramref name="stopDistance"/>,
        /// then calls <paramref name="arrived"/>. The joystick, engaging a monster or being disabled cancels it.
        /// </summary>
        public void RunTo(Vector3 point, float stopDistance, Action arrived)
        {
            combat.StopChasing();
            runningTo = true;
            runPoint = point;
            runStop = Mathf.Max(0.1f, stopDistance);
            runArrived = arrived;
        }

        public void CancelRunTo()
        {
            runningTo = false;
            runArrived = null;
        }

        /// <summary>Turns straight to <paramref name="direction"/> (ground plane), e.g. as a skill form fires.</summary>
        public void FaceNow(Vector3 direction)
        {
            direction = Flat(direction);
            if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>Called by the leap when it lands, so gravity starts fresh.</summary>
        public void ResetFall() => verticalSpeed = 0f;

        public void Teleport(Transform point)
        {
            body.enabled = false;
            transform.SetPositionAndRotation(point.position, point.rotation);
            body.enabled = true;
            verticalSpeed = 0f;
        }

        /// <summary>Unit direction on the ground plane.</summary>
        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }
    }
}
