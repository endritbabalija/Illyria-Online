using System;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;
using Wuxia.Input;
using Wuxia.Monsters;
using Wuxia.World;

namespace Wuxia.Player
{
    /// <summary>
    /// Target lock, auto-attack, HP regen, death and reviving at the Han.
    /// Tap a monster (or press Attack for the nearest one) to lock on and run in; basic attacks then
    /// fire on their own whenever the locked target is in reach.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] TouchControls controls;
        [SerializeField] Camera viewCamera;
        [SerializeField] PlayerController mover;
        [SerializeField] CombatVisuals visuals;
        [SerializeField] LayerMask monsterMask;
        [Tooltip("Sphere radius for tap picking, in metres; forgiving for fingers.")]
        [SerializeField] float tapRadius = 0.6f;

        public Combatant Combatant { get; private set; }
        public Combatant Target { get; private set; }
        /// <summary>True while auto-approaching the target; joystick input clears it.</summary>
        public bool Chasing { get; private set; }
        public bool IsDead => Combatant.IsDead;
        public float RespawnAt { get; private set; }

        public event Action<Combatant> TargetChanged;

        PlayerData data;
        float nextAttackTime;

        void Awake() => Combatant = GetComponent<Combatant>();

        void Start()
        {
            data = GameData.Combat.player;
            Combatant.Init(data.name, 1, data.maxHp, data.defense, data.dodge, data.attack);
        }

        void OnEnable()
        {
            controls.Tapped += OnTapped;
            Combatant.Died += OnDied;
        }

        void OnDisable()
        {
            controls.Tapped -= OnTapped;
            Combatant.Died -= OnDied;
        }

        void Update()
        {
            if (IsDead)
            {
                if (Time.time >= RespawnAt) Revive();
                return;
            }

            if (Target != null && (Target.IsDead || !Target.isActiveAndEnabled || FlatDistance(Target) > data.loseTargetRange))
                ClearTarget();

            if (Target != null && InAttackRange(Target) && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + data.attack.interval;
                visuals.PlayAttack();
                Combatant.BasicAttack(Target);
            }

            if (Time.time - Combatant.LastDamagedTime >= data.regenDelay)
                Combatant.Regenerate(data.regenPerSecond * Time.deltaTime);
        }

        /// <summary>Attack button: re-engage the current target, or lock the nearest monster in range.</summary>
        public void OnAttackButton()
        {
            if (IsDead) return;
            var target = Target != null ? Target : FindNearestMonster();
            if (target != null) Engage(target);
        }

        public void Engage(Combatant target)
        {
            SetTarget(target);
            Chasing = true;
        }

        public void StopChasing() => Chasing = false;

        public bool InAttackRange(Combatant target, float fraction = 1f) =>
            FlatDistance(target) <= data.attack.range * fraction;

        void OnTapped(Vector2 screenPosition)
        {
            if (IsDead) return;
            var ray = viewCamera.ScreenPointToRay(screenPosition);
            if (!Physics.SphereCast(ray, tapRadius, out var hit, 80f, monsterMask, QueryTriggerInteraction.Ignore)) return;
            var target = hit.collider.GetComponentInParent<Combatant>();
            if (target != null && target.Team == Team.Monster && !target.IsDead) Engage(target);
        }

        Combatant FindNearestMonster()
        {
            Combatant best = null;
            var bestDistance = data.lockRange;
            foreach (var monster in MonsterAI.Alive)
            {
                var distance = FlatDistance(monster.Combatant);
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = monster.Combatant;
            }
            return best;
        }

        void OnDied(Combatant killer)
        {
            ClearTarget();
            RespawnAt = Time.time + data.respawnDelay;
            visuals.SetDowned();
            // The 3% EXP death penalty from the design doc arrives with EXP itself (M3/M7).
            Debug.Log($"[Combat] {Combatant.DisplayName} was defeated by {(killer != null ? killer.DisplayName : "something")}; reviving at the Han in {data.respawnDelay:0}s");
        }

        void Revive()
        {
            mover.Teleport(ZoneSession.Instance.RespawnPoint);
            Combatant.RestoreFull();
            visuals.ResetPose();
            nextAttackTime = 0f;
            Debug.Log($"[Combat] {Combatant.DisplayName} revived at the Han");
        }

        void SetTarget(Combatant target)
        {
            if (Target == target) return;
            Target = target;
            TargetChanged?.Invoke(target);
        }

        void ClearTarget()
        {
            SetTarget(null);
            Chasing = false;
        }

        float FlatDistance(Combatant other)
        {
            var d = other.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }
    }
}
