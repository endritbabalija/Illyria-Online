using UnityEngine;
using Wuxia.Data;
using Wuxia.Player;

namespace Wuxia.Net
{
    /// <summary>
    /// Another player in the zone, as the server reports them: the character model sliding towards
    /// each snapshot's position, running or standing by its measured speed, holding their weapon, and
    /// playing the same action clips as our own player (<see cref="CharacterActions"/>) for their
    /// attacks, forms, hits, death, revive and leaps. No combat of its own here; its hits on monsters
    /// arrive as server events.
    /// </summary>
    public class RemotePlayer : MonoBehaviour, IAttackPose
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int RunPlaybackId = Animator.StringToHash("RunPlayback");

        [Tooltip("How quickly the model closes on the position the server last sent (per second).")]
        [SerializeField] float smoothing = 12f;
        [Tooltip("Beyond this distance from the server's position, snap instead of sliding (revives).")]
        [SerializeField] float snapDistance = 8f;
        [Tooltip("Height of a leap's arc onto a ledge, above the higher end, in metres.")]
        [SerializeField] float leapArc = 1.5f;
        [Tooltip("Seconds an attack keeps the model facing its target before snapshots turn it again.")]
        [SerializeField] float faceHold = 0.6f;

        Animator animator;
        CharacterActions actions;
        PlayerAppearance.WeaponRig weapons;
        float runClipSpeed = 5f;
        Vector3 target;
        Quaternion targetRotation;
        Quaternion faceRotation;
        float faceUntil;
        bool dead;
        bool started;

        bool leaping;
        Vector3 leapFrom, leapTo;
        float leapStart, leapDuration;
        bool leapLedge;

        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public string CharacterId { get; private set; }
        /// <summary>Item def id of the weapon in hand.</summary>
        public string WeaponItem { get; private set; }
        /// <summary>The name label over its head (destroyed with it).</summary>
        public GameObject Label { get; set; }

        public float AttackWeight => actions != null ? actions.AttackWeight : 0f;

        /// <param name="look">Our own player's animator and appearance: the action clips, their tuning and the weapon models.</param>
        public void Init(string id, string displayName, string characterId, Vector3 position, float yaw, PlayerAnimator clips, PlayerAppearance look)
        {
            Id = id;
            DisplayName = displayName;
            CharacterId = characterId;
            animator = GetComponentInChildren<Animator>();
            if (animator != null && clips != null)
            {
                actions = clips.CreateActions(animator);
                runClipSpeed = clips.RunClipSpeed;
            }
            if (animator != null && look != null) weapons = look.CreateWeaponRig(animator);
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            target = position;
            targetRotation = transform.rotation;
        }

        public void SetState(Vector3 position, float yaw, bool isDead)
        {
            target = position;
            targetRotation = Quaternion.Euler(0f, yaw, 0f);
            var wasDead = dead;
            dead = isDead;
            if (!started)
            {
                // Someone already lying dead when we arrive: straight to the held pose.
                started = true;
                if (dead && actions != null) actions.Play("death");
                return;
            }
            if (dead == wasDead || actions == null) return;
            actions.Play(dead ? "death" : "revive");
            if (!dead) leaping = false;
        }

        /// <summary>Wields this weapon item (snapshots and gear events); shows its model.</summary>
        public void SetWeapon(string itemDefId)
        {
            if (string.IsNullOrEmpty(itemDefId) || itemDefId == WeaponItem) return;
            WeaponItem = itemDefId;
            if (weapons != null) weapons.Show(GameData.Items.FindItem(itemDefId));
        }

        WeaponData WeaponType()
        {
            var def = string.IsNullOrEmpty(WeaponItem) ? null : GameData.Items.FindItem(WeaponItem);
            if (def == null || string.IsNullOrEmpty(def.weaponType)) return null;
            return GameData.Combat.GetWeapon(def.weaponType);
        }

        /// <summary>A basic attack the server resolved (hit or miss) at a monster.</summary>
        public void PlayAttack(Vector3 at)
        {
            if (dead || actions == null) return;
            var weapon = WeaponType();
            if (weapon == null) return;
            Face(at);
            actions.PlayBasic(weapon.id, weapon.attack.interval);
        }

        /// <summary>A skill form went off.</summary>
        public void PlayForm(FormData form, Vector3? at)
        {
            if (dead || actions == null || form == null) return;
            var weapon = WeaponType();
            if (at.HasValue) Face(at.Value);
            actions.PlayForm(form.id, form.castTime, weapon != null ? weapon.id : null, weapon != null ? weapon.attack.interval : form.castTime);
        }

        public void Flinch()
        {
            if (!dead && actions != null) actions.Flinch();
        }

        /// <summary>A Drangue Leap (onto a ledge) or a dash: fly along an arc, then land.</summary>
        public void Leap(Vector3 from, Vector3 to, float duration, bool ontoLedge)
        {
            if (dead) return;
            leaping = true;
            leapFrom = from;
            leapTo = to;
            leapStart = Time.time;
            leapDuration = Mathf.Max(0.05f, duration);
            leapLedge = ontoLedge;
            target = to;
            var flat = to - from;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f) targetRotation = Quaternion.LookRotation(flat);
            if (ontoLedge && actions != null) actions.Play("leap");
        }

        void Face(Vector3 at)
        {
            var flat = at - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return;
            faceRotation = Quaternion.LookRotation(flat);
            faceUntil = Time.time + faceHold;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var before = transform.position;
            var k = 1f - Mathf.Exp(-smoothing * dt);

            if (leaping)
            {
                var u = Mathf.Clamp01((Time.time - leapStart) / leapDuration);
                var p = Vector3.Lerp(leapFrom, leapTo, u);
                // Clears the higher end by about leapArc.
                if (leapLedge) p.y += 4f * (leapArc + Mathf.Abs(leapTo.y - leapFrom.y) * 0.5f) * u * (1f - u);
                transform.position = p;
                if (u >= 1f)
                {
                    leaping = false;
                    if (leapLedge && actions != null) actions.Play("leap_land");
                }
            }
            else transform.position = (target - before).sqrMagnitude > snapDistance * snapDistance ? target : Vector3.Lerp(before, target, k);

            var facing = Time.time < faceUntil ? faceRotation : targetRotation;
            if (!dead) transform.rotation = Quaternion.Slerp(transform.rotation, facing, k);

            if (animator == null) return;
            var v = (transform.position - before) / dt;
            v.y = 0f;
            var speed = dead || leaping && leapLedge ? 0f : v.magnitude;
            animator.SetFloat(SpeedId, speed, 0.1f, dt);
            animator.SetFloat(RunPlaybackId, Mathf.Clamp(speed / runClipSpeed, 1f, 2f));
            if (actions != null) actions.Update(speed, dt);
        }

        void OnDestroy()
        {
            if (Label != null) Destroy(Label);
        }
    }
}
