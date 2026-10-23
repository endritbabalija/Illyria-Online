using System;
using System.Collections.Generic;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;
using Wuxia.Monsters;
using Wuxia.World;

namespace Wuxia.Player
{
    /// <summary>A skill button: one form from a learned book. Empty when the weapon has fewer forms.</summary>
    public struct SkillSlot
    {
        public BookData Book;
        public FormData Form;
        public bool IsEmpty => Form == null;
    }

    /// <summary>
    /// Skill forms on the 4 skill buttons: Spirit, cooldowns, book levels and skill points (via
    /// <see cref="SkillState"/>), casting with one queued form, and shape targeting through
    /// <see cref="SkillShapes"/>. The buttons show the current weapon's books, in book then form order.
    /// </summary>
    [RequireComponent(typeof(PlayerCombat))]
    [DefaultExecutionOrder(-50)] // State exists before the HUD's Start reads it
    public class PlayerSkills : MonoBehaviour
    {
        public const int SlotCount = 4;

        [SerializeField] PlayerController mover;
        [SerializeField] CombatVisuals visuals;
        [SerializeField] SkillFx fx;
        [Tooltip("A queued form that still can't fire after this many seconds is dropped.")]
        [SerializeField] float queueTimeout = 3f;

        public SkillState State { get; private set; }
        public bool IsCasting => Time.time < castEnd;
        /// <summary>Slot waiting to fire once the current form ends or the target is in reach; -1 for none.</summary>
        public int QueuedSlot => queued;

        /// <summary>Why a button press did nothing, worded for the player ("Not enough Spirit").</summary>
        public event Action<string> Notice;
        /// <summary>The buttons now hold different forms (weapon, character or books changed).</summary>
        public event Action SlotsChanged;
        /// <summary>A form was cast (Spirit spent, about to hit); the character animates it.</summary>
        public event Action<FormData> FormCast;

        readonly SkillSlot[] slots = new SkillSlot[SlotCount];
        readonly List<Combatant> candidates = new List<Combatant>();
        readonly List<System.Numerics.Vector3> positions = new List<System.Numerics.Vector3>();
        readonly List<int> hits = new List<int>();
        PlayerCombat combat;
        string characterId;
        float castEnd;
        int queued = -1;
        float queuedAt;
        bool queuedEngaged;

        void Awake() => combat = GetComponent<PlayerCombat>();

        void Start()
        {
            ResetForCharacter();
            ZoneSession.Instance.KillsChanged += OnKill;
        }

        void OnDestroy()
        {
            if (ZoneSession.Instance != null) ZoneSession.Instance.KillsChanged -= OnKill;
        }

        void OnEnable()
        {
            combat.LoadoutChanged += OnLoadoutChanged;
            combat.StatsChanged += OnStatsChanged;
            combat.Revived += OnRevived;
        }

        void OnDisable()
        {
            combat.LoadoutChanged -= OnLoadoutChanged;
            combat.StatsChanged -= OnStatsChanged;
            combat.Revived -= OnRevived;
        }

        void Update()
        {
            if (combat.IsDead)
            {
                queued = -1;
                return;
            }
            State.Regenerate(Time.deltaTime);
            ProcessQueue();
        }

        public SkillSlot GetSlot(int index) => slots[index];

        /// <summary>Shows a short message to the player (through <see cref="Notice"/>).</summary>
        public void Tell(string message) => Notice?.Invoke(message);

        public void PressSlot(int index)
        {
            if (combat.IsDead) return;
            var slot = slots[index];
            if (slot.IsEmpty) return;
            if (combat.IsDisabled)
            {
                Notice?.Invoke("You can't act right now");
                return;
            }

            var block = State.CheckCast(slot.Book, slot.Form, Time.time);
            if (block != CastBlock.None)
            {
                Notice?.Invoke(Describe(block, slot));
                return;
            }

            // Pressing while a form plays replaces whatever was queued.
            queued = index;
            queuedAt = Time.time;
            queuedEngaged = false;
            ProcessQueue();
        }

        /// <summary>Spends skill points on the next level of a book.</summary>
        public bool LevelUpBook(string bookId)
        {
            var book = GameData.Skills.GetBook(bookId);
            switch (State.CheckLevelUp(bookId, out var cost))
            {
                case LevelUpBlock.MaxLevel: Notice?.Invoke($"{book.name} is at its highest level"); return false;
                case LevelUpBlock.NotEnoughPoints: Notice?.Invoke($"Needs {cost} skill points"); return false;
                case LevelUpBlock.NotLearned: Notice?.Invoke($"You haven't learned {book.name}"); return false;
            }
            State.LevelUp(bookId);
            Debug.Log($"[Skill] {book.name} raised to level {State.BookLevel(bookId)} for {cost} point(s); {State.SkillPoints} left");
            return true;
        }

        /// <summary>Reading a skill book teaches it (items arrive in M3).</summary>
        public void Learn(string bookId)
        {
            if (State.Learn(bookId)) RebuildSlots();
        }

        void ProcessQueue()
        {
            if (queued < 0 || IsCasting || combat.IsDisabled) return;
            if (Time.time - queuedAt > queueTimeout)
            {
                queued = -1;
                return;
            }

            var slot = slots[queued];
            // The form that just played may have used the Spirit this one needed.
            var block = slot.IsEmpty ? CastBlock.NotLearned : State.CheckCast(slot.Book, slot.Form, Time.time);
            if (block != CastBlock.None)
            {
                if (!slot.IsEmpty) Notice?.Invoke(Describe(block, slot));
                queued = -1;
                return;
            }

            var target = combat.Target;
            if (slot.Form.NeedsTarget)
            {
                if (!queuedEngaged)
                {
                    if (target == null) target = combat.FindNearestMonster();
                    if (target == null)
                    {
                        Notice?.Invoke("No target nearby");
                        queued = -1;
                        return;
                    }
                    combat.Engage(target);
                    queuedEngaged = true;
                }

                if (target == null || !InReach(slot.Form, target))
                {
                    // Running in; touching the joystick (or losing the target) cancels the form.
                    if (target == null || !combat.Chasing) queued = -1;
                    return;
                }
            }

            queued = -1;
            Cast(slot, target);
        }

        void Cast(SkillSlot slot, Combatant target)
        {
            var form = slot.Form;
            var now = Time.time;
            State.CommitCast(form, now);
            castEnd = now + form.castTime;
            combat.DelayBasicAttack(castEnd);

            var origin = transform.position;
            var facing = target != null ? target.transform.position - origin : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = transform.forward;
            mover.FaceNow(facing);
            if (form.Shape == SkillShape.Circle) visuals.PlaySpin(form.castTime);
            else visuals.PlayAttack();
            FormCast?.Invoke(form);

            // Copy first: monsters leave MonsterAI.Alive as they die from this very hit.
            candidates.Clear();
            positions.Clear();
            var lockedIndex = -1;
            foreach (var monster in MonsterAI.Alive)
            {
                if (monster.Combatant == target) lockedIndex = candidates.Count;
                candidates.Add(monster.Combatant);
                positions.Add(monster.transform.position.ToNumerics());
            }
            SkillShapes.SelectTargets(form, origin.ToNumerics(), facing.ToFlat(), positions, lockedIndex,
                combat.Weapon.attack.reachHeight, hits);

            var level = State.BookLevel(slot.Book.id);
            foreach (var i in hits) combat.Combatant.SkillHit(candidates[i], form, level);

            fx.Play(form, origin, facing, target != null ? target.transform.position : origin);
            Debug.Log($"[Skill] {form.name} ({slot.Book.name} Lv {level}) hit {hits.Count} target(s)");
        }

        bool InReach(FormData form, Combatant target) =>
            SkillShapes.InReach(form, transform.position.ToNumerics(), target.transform.position.ToNumerics(),
                combat.Weapon.attack.reachHeight);

        void ResetForCharacter()
        {
            var skills = GameData.Skills;
            var player = GameData.Combat.player;
            var character = combat.Character;
            characterId = character.id;
            // Max Spirit includes gear (PlayerCombat.Stats); regen stays per character.
            State = new SkillState(skills, combat.Stats.MaxSpirit, player.spiritRegenPerSecond);

            if (skills.dev.learnAllBooks)
                foreach (var book in skills.books)
                    if (string.IsNullOrEmpty(book.weapon) || Array.IndexOf(character.weapons, book.weapon) >= 0)
                        State.Learn(book.id);
            State.AddSkillPoints(skills.dev.startingSkillPoints);

            castEnd = 0f;
            queued = -1;
            RebuildSlots();
        }

        void RebuildSlots()
        {
            Array.Clear(slots, 0, slots.Length);
            var next = 0;
            foreach (var book in GameData.Skills.books)
            {
                if (book.weapon != combat.Weapon.id || !State.Knows(book.id)) continue;
                foreach (var form in book.forms)
                    if (next < SlotCount) slots[next++] = new SkillSlot { Book = book, Form = form };
            }
            queued = -1;
            SlotsChanged?.Invoke();
        }

        void OnLoadoutChanged()
        {
            if (combat.Character.id != characterId) ResetForCharacter();
            else RebuildSlots();
        }

        void OnStatsChanged()
        {
            if (State != null) State.SetMaxSpirit(combat.Stats.MaxSpirit);
        }

        void OnRevived() => State.RestoreSpirit();

        void OnKill(int totalKills)
        {
            if (!State.AddKill()) return;
            Notice?.Invoke("Skill point earned");
            Debug.Log($"[Skill] Skill point earned ({State.SkillPoints} unspent)");
        }

        static string Describe(CastBlock block, SkillSlot slot)
        {
            switch (block)
            {
                case CastBlock.NotLearned: return $"You haven't learned {slot.Book.name}";
                case CastBlock.Locked: return $"{slot.Form.name} unlocks at {slot.Book.name} level {slot.Form.unlockLevel}";
                case CastBlock.Cooldown: return $"{slot.Form.name} isn't ready";
                case CastBlock.NoSpirit: return "Not enough Spirit";
                default: return "";
            }
        }
    }
}
