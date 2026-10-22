using UnityEngine;
using Wuxia.Data;
using Wuxia.Input;

namespace Wuxia.Player
{
    /// <summary>
    /// Camera-relative joystick movement. With no joystick input it runs to an engaged target until in
    /// attack range; touching the joystick cancels that.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] TouchControls controls;
        [SerializeField] Transform cameraTransform;
        [SerializeField] PlayerCombat combat;
        [SerializeField] float gravity = -20f;
        [SerializeField] float deadZone = 0.1f;
        [Tooltip("Auto-approach stops at this fraction of attack range so the first swing lands.")]
        [SerializeField] float approachFraction = 0.85f;

        CharacterController body;
        PlayerData data;
        float verticalSpeed;

        void Awake() => body = GetComponent<CharacterController>();

        void Start() => data = GameData.Combat.player;

        void Update()
        {
            var dt = Time.deltaTime;
            var move = Vector3.zero;
            var target = combat.Target;

            if (!combat.IsDead)
            {
                var input = controls.Move;
                if (input.magnitude > deadZone)
                {
                    move = Flat(cameraTransform.forward) * input.y + Flat(cameraTransform.right) * input.x;
                    combat.StopChasing();
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
            if (!combat.IsDead && target != null && combat.InAttackRange(target))
                facing = Flat(target.transform.position - transform.position);
            if (facing.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), data.turnSpeed * dt);
        }

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
