using System;
using UnityEngine;
using Wuxia.Data;

namespace Wuxia.Combat
{
    public enum Team { Player, Monster }

    public enum HitKind { Basic, Skill, Poison }

    /// <summary>HP, defensive stats, status effects and hit resolution for anything that can fight.</summary>
    public class Combatant : MonoBehaviour
    {
        static readonly System.Random Rng = new System.Random();

        /// <summary>Fires for every resolved attack, misses included (drives floating combat text).</summary>
        public static event Action<Combatant, AttackResult> AnyAttackResolved;
        /// <summary>Fires when a status lands on someone.</summary>
        public static event Action<Combatant, StatusData> AnyStatusApplied;

        [SerializeField] Team team;
        [Tooltip("Height above the pivot where combat text and the target marker anchor.")]
        [SerializeField] float aimHeight = 1.2f;

        public Team Team => team;
        public float AimHeight => aimHeight;
        public string DisplayName { get; private set; }
        public int Level { get; private set; }
        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public bool IsDead => Hp <= 0;
        public float HpFraction => MaxHp > 0 ? (float)Hp / MaxHp : 0f;
        public int Defense { get; private set; }
        public float Dodge { get; private set; }
        /// <summary>Extra skill-form damage dealt, as a fraction (gear; 0 for monsters).</summary>
        public float SkillDamageBonus { get; private set; }
        /// <summary>Flat reduction of skill-form damage taken, on top of <see cref="Defense"/> (gear; 0 for monsters).</summary>
        public int SkillDefense { get; private set; }
        public ResistData Resist { get; private set; }
        public AttackData Attack { get; private set; }
        public float LastDamagedTime { get; private set; } = float.NegativeInfinity;
        public Combatant LastAttacker { get; private set; }
        public StatusSet Statuses { get; } = new StatusSet();
        /// <summary>Paralysed, stunned or knocked down: can't move, attack or use skills.</summary>
        public bool IsDisabled => Statuses.IsDisabled(Time.time);

        public event Action<AttackResult> Damaged;
        public event Action<Combatant> Died;

        float regenCarry;

        public void Init(string displayName, int level, int maxHp, int defense, float dodge, AttackData attack, ResistData resist)
        {
            DisplayName = displayName;
            Level = level;
            MaxHp = maxHp;
            Hp = maxHp;
            Defense = defense;
            Dodge = dodge;
            Attack = attack;
            Resist = resist ?? new ResistData();
        }

        public void SetDisplayName(string displayName) => DisplayName = displayName;

        /// <summary>Swaps the basic attack, e.g. when the player changes weapon.</summary>
        public void SetAttack(AttackData attack) => Attack = attack;

        /// <summary>
        /// Applies recomputed stats (the player's gear changed). Current HP is kept, capped at the new
        /// max; raising max HP doesn't heal.
        /// </summary>
        public void ApplyStats(int maxHp, int defense, float dodge, ResistData resist, AttackData attack, float skillDamageBonus, int skillDefense)
        {
            MaxHp = Mathf.Max(1, maxHp);
            if (Hp > MaxHp) Hp = MaxHp;
            Defense = defense;
            Dodge = dodge;
            Resist = resist ?? new ResistData();
            Attack = attack;
            SkillDamageBonus = skillDamageBonus;
            SkillDefense = skillDefense;
        }

        /// <summary>Restores HP (a potion), capped at max; returns how much was restored. Does nothing while dead.</summary>
        public int Heal(int amount)
        {
            if (IsDead || amount <= 0) return 0;
            var healed = Mathf.Min(amount, MaxHp - Hp);
            Hp += healed;
            return healed;
        }

        void Update()
        {
            if (IsDead || Statuses.Count == 0) return;
            var damage = Statuses.Update(Time.time, out var source);
            if (damage > 0)
                ApplyHit(source as Combatant, new AttackResult { Hit = true, Damage = damage, Kind = HitKind.Poison });
        }

        /// <summary>Rolls this combatant's basic attack against <paramref name="target"/> and applies it.</summary>
        public AttackResult BasicAttack(Combatant target)
        {
            var result = CombatMath.RollBasicAttack(Attack, target.Defense, target.Dodge, Rng);
            target.ApplyHit(this, result);
            return result;
        }

        /// <summary>
        /// Applies one skill form to <paramref name="target"/>: forms always hit, then roll their status
        /// against the target's resistance.
        /// </summary>
        public AttackResult SkillHit(Combatant target, FormData form, int bookLevel)
        {
            var skills = GameData.Skills;
            var result = new AttackResult
            {
                Hit = true,
                Damage = SkillMath.RollDamage(form, bookLevel, skills.skillPoints.damagePerBookLevel,
                    target.Defense + target.SkillDefense, Rng, SkillDamageBonus),
                Kind = HitKind.Skill,
            };
            target.ApplyHit(this, result);

            var status = form.status;
            if (status.Any && !target.IsDead)
            {
                var data = skills.GetStatus(status.id);
                if (SkillMath.StatusLands(status.chance, target.Resist.Get(data.resist), Rng))
                    target.ApplyStatus(data, status.duration, status.tickDamage, this);
            }
            return result;
        }

        public void ApplyStatus(StatusData data, float duration, int tickDamage, Combatant source)
        {
            if (IsDead) return;
            Statuses.Apply(data, duration, tickDamage, source, Time.time);
            AnyStatusApplied?.Invoke(this, data);
        }

        void ApplyHit(Combatant attacker, AttackResult result)
        {
            if (IsDead) return;

            if (attacker != null) LastAttacker = attacker;
            if (result.Hit)
            {
                Hp = Mathf.Max(0, Hp - result.Damage);
                LastDamagedTime = Time.time;
            }

            AnyAttackResolved?.Invoke(this, result);
            Damaged?.Invoke(result);
            if (IsDead)
            {
                Statuses.Clear();
                Died?.Invoke(attacker);
            }
        }

        /// <summary>Accumulates fractional regen so low per-frame amounts still add up.</summary>
        public void Regenerate(float amount)
        {
            if (IsDead || Hp >= MaxHp) return;
            regenCarry += amount;
            var whole = (int)regenCarry;
            if (whole <= 0) return;
            regenCarry -= whole;
            Hp = Mathf.Min(MaxHp, Hp + whole);
        }

        public void RestoreFull()
        {
            Hp = MaxHp;
            regenCarry = 0f;
            LastAttacker = null;
            LastDamagedTime = float.NegativeInfinity;
            Statuses.Clear();
        }
    }
}
