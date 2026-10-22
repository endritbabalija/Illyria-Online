using UnityEngine;
using Wuxia.Input;

namespace Wuxia.Player
{
    /// <summary>Orbit camera behind the player; swipe to turn, pulls in when something is in the way.</summary>
    [DefaultExecutionOrder(100)]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] TouchControls controls;
        [SerializeField] float distance = 8f;
        [SerializeField] float pivotHeight = 1.6f;
        [SerializeField] float pitch = 28f;
        [SerializeField] float minPitch = 8f;
        [SerializeField] float maxPitch = 65f;
        [Tooltip("Geometry the camera shouldn't clip through (leave out the player and monsters).")]
        [SerializeField] LayerMask obstacles = 1;
        [SerializeField] float collisionRadius = 0.3f;

        float yaw;

        void Start() => yaw = target.eulerAngles.y;

        void LateUpdate()
        {
            var look = controls.LookDelta;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var pivot = target.position + Vector3.up * pivotHeight;
            var back = rotation * Vector3.back;
            var reach = distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, distance, obstacles, QueryTriggerInteraction.Ignore))
                reach = Mathf.Max(0.5f, hit.distance);

            transform.SetPositionAndRotation(pivot + back * reach, rotation);
        }
    }
}
