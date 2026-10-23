using UnityEngine;
using Wuxia.Combat;

namespace Wuxia.Monsters
{
    /// <summary>
    /// Drives a monster model's Animator from its NavMeshAgent and <see cref="MonsterAI"/>: a 1D
    /// locomotion blend on <c>Gait</c> (0 idle, 1 at the monster's wander speed, 2 at its chase speed,
    /// both read from the game data), with walk/gallop playback sped up so the feet keep pace, an
    /// <c>Attack</c> trigger per bite, and <c>Dead</c> on death. Disabled monsters (stunned, paralysed,
    /// knocked down) stand still; a respawn rebinds the Animator back to idle.
    /// </summary>
    [RequireComponent(typeof(MonsterAI))]
    public class MonsterAnimator : MonoBehaviour
    {
        static readonly int GaitId = Animator.StringToHash("Gait");
        static readonly int PlaybackId = Animator.StringToHash("Playback");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int DeadId = Animator.StringToHash("Dead");

        [SerializeField] Animator animator;
        [Tooltip("Ground speed in m/s the walk clip matches at the model's scale.")]
        [SerializeField] float walkClipSpeed = 0.85f;
        [Tooltip("Ground speed in m/s the gallop clip matches at the model's scale.")]
        [SerializeField] float gallopClipSpeed = 3.4f;
        [Tooltip("Locomotion playback never goes faster than this.")]
        [SerializeField] float maxPlayback = 2.5f;
        [Tooltip("Seconds to ease the Gait parameter towards the measured speed.")]
        [SerializeField] float damping = 0.1f;

        MonsterAI ai;
        Combatant combatant;

        void Awake()
        {
            ai = GetComponent<MonsterAI>();
            combatant = GetComponent<Combatant>();
        }

        void OnEnable()
        {
            ai.Spawned += OnSpawned;
            ai.Attacked += OnAttacked;
            combatant.Died += OnDied;
        }

        void OnDisable()
        {
            ai.Spawned -= OnSpawned;
            ai.Attacked -= OnAttacked;
            combatant.Died -= OnDied;
        }

        void OnSpawned()
        {
            animator.Rebind();
            animator.SetBool(DeadId, false);
            animator.SetFloat(GaitId, 0f);
            animator.SetFloat(PlaybackId, 1f);
        }

        void OnAttacked() => animator.SetTrigger(AttackId);

        void OnDied(Combatant killer)
        {
            animator.ResetTrigger(AttackId);
            animator.SetFloat(GaitId, 0f);
            animator.SetFloat(PlaybackId, 1f);
            animator.SetBool(DeadId, true);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f || combatant.IsDead || ai.Data == null) return;

            var speed = 0f;
            if (!combatant.IsDisabled)
            {
                var v = ai.Velocity;
                v.y = 0f;
                speed = v.magnitude;
            }

            var data = ai.Data;
            var gait = speed <= data.wanderSpeed
                ? speed / Mathf.Max(0.01f, data.wanderSpeed)
                : 1f + (speed - data.wanderSpeed) / Mathf.Max(0.01f, data.chaseSpeed - data.wanderSpeed);
            gait = Mathf.Clamp(gait, 0f, 2f);

            var clipSpeed = Mathf.Lerp(walkClipSpeed, gallopClipSpeed, gait - 1f);
            animator.SetFloat(GaitId, gait, damping, dt);
            animator.SetFloat(PlaybackId, Mathf.Clamp(speed / clipSpeed, 1f, maxPlayback));
        }
    }
}
