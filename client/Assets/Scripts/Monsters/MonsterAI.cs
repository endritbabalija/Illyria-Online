using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Wuxia.Combat;
using Wuxia.Data;
using Wuxia.World;

namespace Wuxia.Monsters
{
    /// <summary>
    /// Simple hunting-zone monster: wanders near its spawn point, chases the player inside aggro range
    /// (or when hit, calling its pack in), bites in melee, and runs home to full HP once pulled past
    /// its leash. Stands still while stunned, paralysed or knocked down, and can't bite up onto a ledge.
    /// Offline for now; the server takes over these decisions in M4.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Combatant))]
    public class MonsterAI : MonoBehaviour
    {
        enum State { Idle, Chase, Return, Dead }

        static readonly List<MonsterAI> alive = new List<MonsterAI>();
        public static IReadOnlyList<MonsterAI> Alive => alive;

        [SerializeField] string monsterId = "wolf";
        [SerializeField] CombatVisuals visuals;
        [SerializeField] float corpseTime = 2.5f;
        [SerializeField] float packAssistRadius = 7f;
        [SerializeField] float wanderPauseMin = 2f;
        [SerializeField] float wanderPauseMax = 6f;
        [Tooltip("Wander targets and respawn points keep at least this far (centre to centre) from where other monsters stand or are heading, so packmates don't stop inside each other.")]
        [SerializeField] float personalSpace = 1.8f;

        public Combatant Combatant { get; private set; }
        public float PersonalSpace => personalSpace;
        public MonsterData Data => data;
        public string MonsterId => monsterId;
        public float RespawnDelay => data.respawnDelay;
        /// <summary>Online: the server runs this monster; it only follows what the server says.</summary>
        public bool Remote { get; private set; }
        /// <summary>Online: the server's id for this monster ("m12").</summary>
        public string NetId { get; private set; }
        /// <summary>Ground velocity, measured (the agent's when local, from the server's moves when remote).</summary>
        public Vector3 Velocity => Remote ? remoteVelocity : agent.enabled ? agent.velocity : Vector3.zero;

        /// <summary>Fires after every (re)spawn, once the monster is back at full HP and idle.</summary>
        public event Action Spawned;
        /// <summary>Fires when the monster bites (its basic attack), just before the hit resolves.</summary>
        public event Action Attacked;

        NavMeshAgent agent;
        Collider body;
        MonsterData data;
        MonsterSpawner spawner;
        Vector3 home;
        State state;
        Combatant target;
        float nextAttackTime;
        float nextRepathTime;
        float nextWanderTime;
        float despawnAt;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<Collider>();
            Combatant = GetComponent<Combatant>();
            data = GameData.Combat.GetMonster(monsterId);
            Combatant.Init(data.name, data.level, data.maxHp, data.defense, data.dodge, data.attack, data.resist);
            agent.stoppingDistance = data.attack.range * 0.8f;
        }

        void OnEnable()
        {
            Combatant.Damaged += OnDamaged;
            Combatant.Died += OnDied;
        }

        void OnDisable()
        {
            Combatant.Damaged -= OnDamaged;
            Combatant.Died -= OnDied;
            alive.Remove(this);
        }

        /// <summary>(Re)spawns at a point on the NavMesh, full HP, idle.</summary>
        public void Spawn(MonsterSpawner owner, Vector3 position)
        {
            spawner = owner;
            home = position;
            gameObject.SetActive(true);
            agent.enabled = true;
            agent.Warp(position);
            body.enabled = true;
            Combatant.RestoreFull();
            visuals.ResetPose();
            target = null;
            EnterIdle();
            if (!alive.Contains(this)) alive.Add(this);
            Spawned?.Invoke();
        }

        void Update()
        {
            if (Remote)
            {
                UpdateRemote();
                return;
            }
            var disabled = state != State.Dead && Combatant.IsDisabled;
            if (agent.isOnNavMesh && agent.isStopped != disabled) agent.isStopped = disabled;
            if (disabled) return;

            switch (state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.Chase: UpdateChase(); break;
                case State.Return: UpdateReturn(); break;
                case State.Dead:
                    if (Time.time >= despawnAt)
                    {
                        gameObject.SetActive(false);
                        spawner.OnDespawned(this);
                    }
                    break;
            }
        }

        void UpdateIdle()
        {
            var player = ZoneSession.Instance.Player;
            if (player != null && !player.IsDead && FlatDistance(player.transform.position) <= data.aggroRange)
            {
                StartChase(player);
                return;
            }

            if (Time.time < nextWanderTime || agent.pathPending || agent.remainingDistance > agent.stoppingDistance) return;
            nextWanderTime = Time.time + UnityEngine.Random.Range(wanderPauseMin, wanderPauseMax);
            // A few tries for a free spot; if the pack crowds every one, stay put until the next pause.
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var offset = UnityEngine.Random.insideUnitCircle * data.wanderRadius;
                if (NavMesh.SamplePosition(home + new Vector3(offset.x, 0f, offset.y), out var hit, 2f, NavMesh.AllAreas)
                    && IsFree(hit.position, personalSpace, this))
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        /// <summary>
        /// True if no other living monster stands, or is walking to, within <paramref name="space"/>
        /// of <paramref name="point"/> on the ground plane.
        /// </summary>
        public static bool IsFree(Vector3 point, float space, MonsterAI except = null)
        {
            var spaceSqr = space * space;
            foreach (var other in alive)
            {
                if (other == except) continue;
                if (FlatSqrDistance(other.transform.position, point) < spaceSqr) return false;
                var heading = other.agent.hasPath || other.agent.pathPending;
                if (heading && FlatSqrDistance(other.agent.destination, point) < spaceSqr) return false;
            }
            return true;
        }

        static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        void UpdateChase()
        {
            if (target == null || target.IsDead || (transform.position - home).magnitude > data.leashRange)
            {
                StartReturn();
                return;
            }

            if (InReach(target.transform.position))
            {
                if (agent.hasPath) agent.ResetPath();
                FaceTowards(target.transform.position);
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + data.attack.interval;
                    visuals.PlayAttack();
                    Attacked?.Invoke();
                    Combatant.BasicAttack(target);
                }
            }
            else if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + 0.2f;
                agent.SetDestination(target.transform.position);
            }
        }

        void UpdateReturn()
        {
            if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.2f) return;
            Combatant.RestoreFull();
            EnterIdle();
        }

        void EnterIdle()
        {
            state = State.Idle;
            agent.speed = data.wanderSpeed;
            nextWanderTime = Time.time + UnityEngine.Random.Range(0f, wanderPauseMax);
        }

        void StartChase(Combatant who)
        {
            if (state == State.Chase && target == who) return;
            state = State.Chase;
            target = who;
            agent.speed = data.chaseSpeed;
            nextRepathTime = 0f;
            // A short wind-up so a wolf doesn't bite the instant it reaches you.
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.4f);
        }

        void StartReturn()
        {
            state = State.Return;
            target = null;
            agent.speed = data.chaseSpeed;
            agent.SetDestination(home);
        }

        /// <summary>Called by the spawner when a packmate nearby is attacked.</summary>
        public void Assist(Combatant attacker, Vector3 packmatePosition)
        {
            if (state != State.Idle || attacker == null || attacker.IsDead) return;
            if (FlatDistance(packmatePosition) <= packAssistRadius) StartChase(attacker);
        }

        void OnDamaged(AttackResult result)
        {
            if (Remote) return; // the server decides who it chases
            var attacker = Combatant.LastAttacker;
            if (Combatant.IsDead || state != State.Idle || attacker == null) return;
            StartChase(attacker);
            spawner.AlertPack(this, attacker);
        }

        void OnDied(Combatant killer)
        {
            if (Remote)
            {
                // The server counts the kill and says when the corpse goes (DespawnRemote).
                state = State.Dead;
                alive.Remove(this);
                target = null;
                body.enabled = false;
                visuals.SetDowned();
                remoteVelocity = Vector3.zero;
                return;
            }
            state = State.Dead;
            alive.Remove(this);
            target = null;
            agent.ResetPath();
            agent.enabled = false;
            body.enabled = false;
            visuals.SetDowned();
            despawnAt = Time.time + corpseTime;
            if (killer != null && killer.Team == Team.Player) ZoneSession.Instance.RegisterKill(Combatant);
        }

        // ---- Online: driven by the server -------------------------------------------------------

        Vector3 remoteTarget;
        Quaternion remoteRotation;
        Vector3 remoteVelocity;
        [Tooltip("Online: how quickly a remote monster closes on the position the server last sent (per second).")]
        [SerializeField] float remoteSmoothing = 12f;
        [Tooltip("Online: beyond this distance from the server's position, snap instead of sliding.")]
        [SerializeField] float remoteSnapDistance = 6f;

        /// <summary>Online: (re)appears where the server put it, at the server's HP, idle.</summary>
        public void SpawnRemote(string netId, Vector3 position, float yaw, int hp, int maxHp)
        {
            Remote = true;
            NetId = netId;
            home = position;
            gameObject.SetActive(true);
            agent.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            remoteTarget = position;
            remoteRotation = transform.rotation;
            remoteVelocity = Vector3.zero;
            body.enabled = true;
            Combatant.RestoreFull();
            Combatant.SetHp(hp, maxHp);
            visuals.ResetPose();
            target = null;
            state = State.Idle;
            if (!alive.Contains(this)) alive.Add(this);
            Spawned?.Invoke();
        }

        /// <summary>Online: the server's latest position and facing for this monster.</summary>
        public void SetRemoteState(Vector3 position, float yaw)
        {
            remoteTarget = position;
            remoteRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>Online: it bit someone (the hit itself arrives as an event).</summary>
        public void PlayRemoteAttack(Vector3 victim)
        {
            if (state == State.Dead) return;
            var d = victim - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) remoteRotation = transform.rotation = Quaternion.LookRotation(d);
            visuals.PlayAttack();
            Attacked?.Invoke();
        }

        /// <summary>Online: the corpse is gone.</summary>
        public void DespawnRemote()
        {
            alive.Remove(this);
            gameObject.SetActive(false);
        }

        void UpdateRemote()
        {
            var dt = Time.deltaTime;
            if (state == State.Dead || dt <= 0f) return;
            var before = transform.position;
            var to = remoteTarget;
            transform.position = (to - before).sqrMagnitude > remoteSnapDistance * remoteSnapDistance
                ? to
                : Vector3.Lerp(before, to, 1f - Mathf.Exp(-remoteSmoothing * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, remoteRotation, 1f - Mathf.Exp(-remoteSmoothing * dt));
            var v = (transform.position - before) / dt;
            v.y = 0f;
            remoteVelocity = v;
        }

        void FaceTowards(Vector3 point)
        {
            var d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), agent.angularSpeed * Time.deltaTime);
        }

        bool InReach(Vector3 point) =>
            FlatDistance(point) <= data.attack.range && Mathf.Abs(point.y - transform.position.y) <= data.attack.reachHeight;

        float FlatDistance(Vector3 point)
        {
            var d = point - transform.position;
            d.y = 0f;
            return d.magnitude;
        }
    }
}
