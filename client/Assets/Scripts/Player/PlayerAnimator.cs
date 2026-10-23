using UnityEngine;

namespace Wuxia.Player
{
    /// <summary>
    /// Drives the character model's Animator from how fast the player actually moves on the ground:
    /// a 1D blend from idle (0 m/s) to run, with the run sped up past its authored speed so the feet
    /// keep pace. Measures movement itself, so joystick, auto-approach and dashes all animate alike.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int RunPlaybackId = Animator.StringToHash("RunPlayback");

        [SerializeField] Animator animator;
        [Tooltip("Ground speed in m/s the run clip was authored at (Quaternius Jog_Fwd_Loop: about 5).")]
        [SerializeField] float runClipSpeed = 5f;
        [Tooltip("Seconds to ease the Speed parameter towards the measured speed.")]
        [SerializeField] float damping = 0.08f;
        [Tooltip("Run playback never goes faster than this, e.g. mid-leap.")]
        [SerializeField] float maxRunPlayback = 1.5f;

        Vector3 lastPosition;

        void OnEnable() => lastPosition = transform.position;

        /// <summary>Drives another model's Animator, e.g. after a character switch.</summary>
        public void SetAnimator(Animator target) => animator = target;

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;

            var delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;
            // Clamp so a teleport (reviving at the Han) reads as running, not as a huge spike.
            var speed = Mathf.Min(delta.magnitude / dt, runClipSpeed * maxRunPlayback);

            animator.SetFloat(SpeedId, speed, damping, dt);
            animator.SetFloat(RunPlaybackId, Mathf.Clamp(speed / runClipSpeed, 1f, maxRunPlayback));
        }
    }
}
