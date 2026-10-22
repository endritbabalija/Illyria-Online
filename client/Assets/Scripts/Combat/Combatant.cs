using System;
using UnityEngine;
using Wuxia.Data;

namespace Wuxia.Combat
{
    public enum Team { Player, Monster }

    /// <summary>HP, defensive stats and basic-attack resolution for anything that can fight.</summary>
    public class Combatant : MonoBehaviour
    {
        static readonly System.Random Rng = new System.Random();

        /// <summary>Fires for every resolved attack, misses included (drives floating combat text).</summary>
        public static event Action<Combatant, AttackResult> AnyAttackResolved;

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
        public AttackData Attack { get; private set; }
        public float LastDamagedTime { get; private set; } = float.NegativeInfinity;
        public Combatant LastAttacker { get; private set; }

        public event Action<AttackResult> Damaged;
        public event Action<Combatant> Died;

        float regenCarry;

        public void Init(string displayName, int level, int maxHp, int defense, float dodge, AttackData attack)
        {
            DisplayName = displayName;
            Level = level;
            MaxHp = maxHp;
            Hp = maxHp;
            Defense = defense;
            Dodge = dodge;
            Attack = attack;
        }

        /// <summary>Rolls this combatant's basic attack against <paramref name="target"/> and applies it.</summary>
        public AttackResult BasicAttack(Combatant target)
        {
            var result = CombatMath.RollBasicAttack(Attack, target.Defense, target.Dodge, Rng);
            target.ReceiveAttack(this, result);
            return result;
        }

        void ReceiveAttack(Combatant attacker, AttackResult result)
        {
            if (IsDead) return;

            LastAttacker = attacker;
            if (result.Hit)
            {
                Hp = Mathf.Max(0, Hp - result.Damage);
                LastDamagedTime = Time.time;
            }

            AnyAttackResolved?.Invoke(this, result);
            Damaged?.Invoke(result);
            if (IsDead) Died?.Invoke(attacker);
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
        }
    }
}
