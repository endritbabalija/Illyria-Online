using UnityEngine;

namespace Wuxia.Net
{
    /// <summary>
    /// Another player in the zone, as the server reports them: the character model sliding towards
    /// each snapshot's position, running or standing by its measured speed, lying down while dead.
    /// No combat of its own here; its hits on monsters arrive as server events.
    /// </summary>
    public class RemotePlayer : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int RunPlaybackId = Animator.StringToHash("RunPlayback");

        [Tooltip("How quickly the model closes on the position the server last sent (per second).")]
        [SerializeField] float smoothing = 12f;
        [Tooltip("Beyond this distance from the server's position, snap instead of sliding (revives, leaps).")]
        [SerializeField] float snapDistance = 8f;
        [Tooltip("Ground speed in m/s the run clip matches (PlayerAnimator uses the same).")]
        [SerializeField] float runClipSpeed = 5f;

        Animator animator;
        Vector3 target;
        Quaternion targetRotation;
        bool dead;

        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public string CharacterId { get; private set; }
        /// <summary>The name label over its head (destroyed with it).</summary>
        public GameObject Label { get; set; }

        public void Init(string id, string displayName, string characterId, Vector3 position, float yaw)
        {
            Id = id;
            DisplayName = displayName;
            CharacterId = characterId;
            animator = GetComponentInChildren<Animator>();
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            target = position;
            targetRotation = transform.rotation;
        }

        public void SetState(Vector3 position, float yaw, bool isDead)
        {
            target = position;
            targetRotation = Quaternion.Euler(0f, yaw, 0f);
            if (isDead == dead) return;
            dead = isDead;
            // No death clip on the bare model yet: lie it down, as the greybox did.
            transform.rotation = targetRotation * (dead ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var before = transform.position;
            var k = 1f - Mathf.Exp(-smoothing * dt);
            transform.position = (target - before).sqrMagnitude > snapDistance * snapDistance ? target : Vector3.Lerp(before, target, k);
            if (!dead) transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, k);

            if (animator == null) return;
            var v = (transform.position - before) / dt;
            v.y = 0f;
            var speed = dead ? 0f : v.magnitude;
            animator.SetFloat(SpeedId, speed, 0.1f, dt);
            animator.SetFloat(RunPlaybackId, Mathf.Clamp(speed / runClipSpeed, 1f, 2f));
        }

        void OnDestroy()
        {
            if (Label != null) Destroy(Label);
        }
    }
}
