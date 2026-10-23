using System;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.Data;
using Wuxia.Input;
using Wuxia.Items;
using Wuxia.Monsters;
using Wuxia.Net;
using Wuxia.World;

namespace Wuxia.Player
{
    /// <summary>
    /// Character and weapon, target lock, auto-attack, HP regen, death and reviving at the Han.
    /// Tap a monster (or press Attack for the nearest one) to lock on and run in; basic attacks then
    /// fire on their own whenever the locked target is in reach.
    /// Also hosts the character's items (<see cref="Items"/>: gold, bag, gear, potions) and fights with
    /// the stats computed from them (<see cref="Stats"/>), recomputed whenever gear changes.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    [DefaultExecutionOrder(-60)] // set up before PlayerSkills and the HUD read the character
    public class PlayerCombat : MonoBehaviour, IVitals
    {
        [SerializeField] TouchControls controls;
        [SerializeField] Camera viewCamera;
        [SerializeField] PlayerController mover;
        [SerializeField] CombatVisuals visuals;
        [SerializeField] LayerMask monsterMask;
        [Tooltip("NPCs you can tap to talk to or trade with (the Npc layer).")]
        [SerializeField] LayerMask interactMask;
        [Tooltip("Sphere radius for tap picking, in metres; forgiving for fingers.")]
        [SerializeField] float tapRadius = 0.6f;
        [Tooltip("Character from combat.json to play offline (kreshnik or zanas_chosen). Online, the character select picks it.")]
        [SerializeField] string characterId = "kreshnik";

        public Combatant Combatant { get; private set; }
        public PlayerController Mover => mover;
        public Combatant Target { get; private set; }
        /// <summary>True while auto-approaching the target; joystick input clears it.</summary>
        public bool Chasing { get; private set; }
        public bool IsDead => Combatant.IsDead;
        /// <summary>Stunned, paralysed or knocked down.</summary>
        public bool IsDisabled => Combatant.IsDisabled;
        public float RespawnAt { get; private set; }
        public CharacterData Character { get; private set; }
        /// <summary>The wielded weapon's type from combat.json (range, interval, skill books). Damage comes from <see cref="Stats"/>.</summary>
        public WeaponData Weapon { get; private set; }
        /// <summary>Gold, bag, the 6 gear slots, potion quick slots: the one place items and gold change.</summary>
        public PlayerItems Items { get; private set; }
        /// <summary>Final stats from base + gear; <see cref="CharacterStats.Attack"/> is the basic attack in use.</summary>
        public CharacterStats Stats { get; private set; }

        public event Action<Combatant> TargetChanged;
        /// <summary>Character, weapon type or wielded weapon item changed.</summary>
        public event Action LoadoutChanged;
        /// <summary><see cref="Stats"/> was recomputed (gear changed).</summary>
        public event Action StatsChanged;
        public event Action Revived;
        /// <summary>A basic attack swung or shot at the target (before its result is known).</summary>
        public event Action Attacked;
        /// <summary>A potion was drunk, from a quick slot or the bag.</summary>
        public event Action PotionUsed;

        PlayerData data;
        PlayerSkills skills;
        float nextAttackTime;
        bool resetting;

        void Awake()
        {
            // ZoneSession runs first (execution order -200) and has loaded GameData by now.
            Combatant = GetComponent<Combatant>();
            skills = GetComponent<PlayerSkills>();
            data = GameData.Combat.player;
            Character = GameData.Combat.GetCharacter(characterId);
            Items = new PlayerItems(GameData.Items, GameData.Combat, new ItemFactory(GameData.Items), characterId, OnLedger);
            Items.EquipmentChanged += OnEquipmentChanged;
            Stats = Items.ComputeStats();
            Weapon = GameData.Combat.GetWeapon(Stats.WeaponType);
            Combatant.Init(Character.name, 1, Stats.MaxHp, Stats.Defense, Stats.Dodge, Stats.Attack, Stats.Resist);
            ApplyStats();
            LogLoadout();
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
            // Online the server decides attacks, regen and the revive; their results arrive through Net.NetZone.
            var online = NetZone.Online;
            if (IsDead)
            {
                if (!online && Time.time >= RespawnAt) Revive();
                return;
            }

            if (Target != null && (Target.IsDead || !Target.isActiveAndEnabled || FlatDistance(Target) > data.loseTargetRange))
                ClearTarget();
            if (online) return;

            if (Target != null && !IsDisabled && InAttackRange(Target) && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + Stats.Attack.interval;
                visuals.PlayAttack();
                Attacked?.Invoke();
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

        /// <summary>
        /// Swap button: wields the best (or last used) weapon of the character's other type from the
        /// bag (<see cref="PlayerItems.FindSwapWeapon"/>): new basic attack and skill buttons.
        /// </summary>
        public void SwapWeapon()
        {
            if (IsDead) return;
            if (NetZone.Online)
            {
                NetZone.Instance.SendAction(Json.Object("a", "swap"));
                return;
            }
            var result = Items.SwapWeapon();
            if (result != EquipBlock.None) Tell(Describe(result));
        }

        /// <summary>Wears an item from the bag (inventory panel). Shows a notice when it can't.</summary>
        public EquipBlock Equip(string itemId)
        {
            if (NetZone.Online) return Send(EquipBlock.None, "a", "equip", "item", itemId);
            var result = Items.Equip(itemId);
            if (result != EquipBlock.None) Tell(Describe(result));
            return result;
        }

        /// <summary>Takes off a worn item into the bag. Shows a notice when it can't.</summary>
        public EquipBlock Unequip(EquipSlot slot)
        {
            if (NetZone.Online) return Send(EquipBlock.None, "a", "unequip", "slot", slot.ToString().ToLowerInvariant());
            var result = Items.Unequip(slot);
            if (result != EquipBlock.None && result != EquipBlock.NotFound) Tell(Describe(result));
            return result;
        }

        /// <summary>Potion button: drinks the quick slot's potion. Shows a notice when it can't.</summary>
        public UseBlock UseQuickSlot(int index)
        {
            if (NetZone.Online)
            {
                NetZone.Instance.SendAction(Json.Object("a", "quickUse", "slot", index), r => { if (r.ok) PotionUsed?.Invoke(); });
                return UseBlock.None;
            }
            var result = Items.UseQuickSlot(index, Time.time, this);
            if (result == UseBlock.None) PotionUsed?.Invoke();
            else Tell(Describe(result, Items.Data.FindItem(Items.QuickSlot(index) ?? "")));
            return result;
        }

        /// <summary>Drinks a potion from the bag (inventory panel). Shows a notice when it can't.</summary>
        public UseBlock UseItem(string defId)
        {
            if (NetZone.Online)
            {
                NetZone.Instance.SendAction(Json.Object("a", "use", "item", defId), r => { if (r.ok) PotionUsed?.Invoke(); });
                return UseBlock.None;
            }
            var result = Items.Use(defId, Time.time, this);
            if (result == UseBlock.None) PotionUsed?.Invoke();
            else Tell(Describe(result, Items.Data.FindItem(defId)));
            return result;
        }

        /// <summary>Buys from an NPC shop. Shows the reason (not enough gold, bag full) when it can't.</summary>
        public ShopBlock Buy(string shopId, string defId, int count)
        {
            if (NetZone.Online) return Send(ShopBlock.None, "a", "buy", "shop", shopId, "item", defId, "count", count);
            var result = Items.Buy(shopId, defId, count);
            if (result != ShopBlock.None) Tell(Wuxia.UI.ItemText.Describe(result));
            return result;
        }

        /// <summary>Sells from the bag to an NPC shop. Shows the reason when it can't.</summary>
        public ShopBlock Sell(string itemId, int count)
        {
            if (NetZone.Online) return Send(ShopBlock.None, "a", "sell", "item", itemId, "count", count);
            var result = Items.Sell(itemId, count);
            if (result != ShopBlock.None) Tell(Wuxia.UI.ItemText.Describe(result));
            return result;
        }

        /// <summary>
        /// Moves a bag item to another slot (merging potions or swapping), from the inventory panel.
        /// False when nothing moved; online the server's answer comes back as a new bag.
        /// </summary>
        public bool MoveItem(int from, int to)
        {
            if (NetZone.Online)
            {
                NetZone.Instance.SendAction(Json.Object("a", "moveItem", "from", from, "to", to));
                return true;
            }
            return Items.Bag.Move(from, to);
        }

        /// <summary>Puts a potion on a quick slot (the other slot trades places if it had it).</summary>
        public bool AssignQuickSlot(int index, string defId)
        {
            if (NetZone.Online)
            {
                NetZone.Instance.SendAction(Json.Object("a", "quickSlot", "slot", index, "item", defId));
                return true;
            }
            return Items.AssignQuickSlot(index, defId);
        }

        /// <summary>Online item actions answer later; a refusal shows as a notice (the server's wording).</summary>
        T Send<T>(T optimistic, params object[] message)
        {
            NetZone.Instance.SendAction(Json.Object(message));
            return optimistic;
        }

        /// <summary>
        /// Play as another character. Offline (test helper): with their starter kit. Online: rejoins
        /// the zone as that character (which must exist; the character select creates them), with
        /// their saved progress.
        /// </summary>
        public void SetCharacter(string id)
        {
            if (NetZone.Online)
            {
                NetZone.Instance.SwitchCharacter(id);
                return;
            }
            Character = GameData.Combat.GetCharacter(id);
            characterId = id;
            resetting = true;
            try { Items.ResetToStarterKit(id); }
            finally { resetting = false; }
            Combatant.SetDisplayName(Character.name);
            RefreshStats(true);
        }

        /// <summary>Online: the server says we play this character; its items and skills follow in the same message.</summary>
        public void ApplyServerCharacter(string id, string displayName)
        {
            Character = GameData.Combat.GetCharacter(id);
            characterId = id;
            Combatant.SetDisplayName(string.IsNullOrEmpty(displayName) ? Character.name : displayName);
        }

        /// <summary>Online: the server resolved one of our basic attacks; play the swing (the hit lands on the target separately).</summary>
        public void PlayServerAttack(Combatant target)
        {
            if (IsDead) return;
            if (target != null) mover.FaceNow(target.transform.position - transform.position);
            visuals.PlayAttack();
            Attacked?.Invoke();
        }

        /// <summary>Online: the server revived us at the zone's respawn point.</summary>
        public void ServerRevive(Vector3 position)
        {
            mover.Teleport(position, Quaternion.identity);
            Combatant.RestoreFull();
            visuals.ResetPose();
            nextAttackTime = 0f;
            Revived?.Invoke();
            Debug.Log($"[Combat] {Combatant.DisplayName} revived at the Han");
        }

        void OnEquipmentChanged(EquipSlot slot)
        {
            if (!resetting) RefreshStats(slot == EquipSlot.Weapon);
        }

        void RefreshStats(bool weaponChanged)
        {
            Stats = Items.ComputeStats();
            var weapon = GameData.Combat.GetWeapon(Stats.WeaponType);
            var typeChanged = weapon != Weapon;
            Weapon = weapon;
            ApplyStats();
            // A short draw time so swapping can't be used to skip the attack interval.
            if (weaponChanged)
            {
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.4f);
                LogLoadout();
            }
            StatsChanged?.Invoke();
            if (weaponChanged || typeChanged) LoadoutChanged?.Invoke();
        }

        void ApplyStats() =>
            Combatant.ApplyStats(Stats.MaxHp, Stats.Defense, Stats.Dodge, Stats.Resist, Stats.Attack, Stats.SkillDamage, Stats.SkillDefense);

        void OnLedger(LedgerEntry entry) => Debug.Log(entry.ToString());

        void LogLoadout()
        {
            var a = Stats.Attack;
            var weapon = Items.Equipment.Weapon;
            Debug.Log($"[Items] {Character.name} wields {(weapon != null ? weapon.ToString() : "no weapon")}: basic attack {a.minDamage}–{a.maxDamage}, " +
                      $"HP {Stats.MaxHp}, Spirit {Stats.MaxSpirit}, defense {Stats.Defense}; {Items.Gold} gold");
        }

        /// <summary>Shows a short notice on the HUD ("Your bag is full").</summary>
        public void Tell(string message)
        {
            if (skills != null) skills.Tell(message);
        }

        static string Describe(EquipBlock block)
        {
            switch (block)
            {
                case EquipBlock.NotGear: return "That can't be worn";
                case EquipBlock.WrongCharacter: return "Your character can't use that";
                case EquipBlock.LevelTooLow: return "Your level is too low";
                case EquipBlock.BagFull: return "Your bag is full";
                case EquipBlock.WeaponRequired: return "You always hold a weapon; equip another instead";
                case EquipBlock.NoOtherWeapon: return "No other weapon in your bag";
                case EquipBlock.NotFound: return "That item isn't in your bag";
                default: return "";
            }
        }

        static string Describe(UseBlock block, ItemDef item)
        {
            var name = item != null ? item.name : "potion";
            switch (block)
            {
                case UseBlock.Dead: return "You can't do that now";
                case UseBlock.NoneLeft: return $"No {name} left";
                case UseBlock.NotUsable: return "Nothing to use there";
                case UseBlock.Cooldown: return $"{name} isn't ready";
                case UseBlock.AlreadyFull:
                    return item != null && item.use != null && item.use.hp > 0 ? "HP is already full" : "Spirit is already full";
                default: return "";
            }
        }

        // Potions top up HP on the Combatant and Spirit on PlayerSkills' state.
        int IVitals.Hp => Combatant.Hp;
        int IVitals.MaxHp => Combatant.MaxHp;
        float IVitals.Spirit => skills != null && skills.State != null ? skills.State.Spirit : 0f;
        int IVitals.MaxSpirit => skills != null && skills.State != null ? skills.State.MaxSpirit : 0;
        void IVitals.RestoreHp(int amount) => Combatant.Heal(amount);

        void IVitals.RestoreSpirit(float amount)
        {
            if (skills != null && skills.State != null) skills.State.AddSpirit(amount);
        }

        public void Engage(Combatant target)
        {
            SetTarget(target);
            Chasing = true;
            if (NetZone.Online) NetZone.Instance.SendTarget(target);
        }

        public void StopChasing() => Chasing = false;

        /// <summary>Holds off basic attacks until <paramref name="time"/>, e.g. while a skill form plays.</summary>
        public void DelayBasicAttack(float time) => nextAttackTime = Mathf.Max(nextAttackTime, time);

        public bool InAttackRange(Combatant target, float fraction = 1f) =>
            FlatDistance(target) <= Weapon.attack.range * fraction
            && Mathf.Abs(target.transform.position.y - transform.position.y) <= Weapon.attack.reachHeight;

        public Combatant FindNearestMonster()
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

        void OnTapped(Vector2 screenPosition)
        {
            if (IsDead) return;
            var ray = viewCamera.ScreenPointToRay(screenPosition);
            if (Physics.SphereCast(ray, tapRadius, out var hit, 80f, monsterMask, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<Combatant>();
                if (target != null && target.Team == Team.Monster && !target.IsDead) Engage(target);
                return;
            }
            // No monster under the finger: maybe an NPC (the Han Keeper's shop).
            if (Physics.SphereCast(ray, tapRadius, out hit, 80f, interactMask, QueryTriggerInteraction.Collide))
            {
                var npc = hit.collider.GetComponentInParent<Shopkeeper>();
                if (npc != null) npc.Interact(this);
            }
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
            Revived?.Invoke();
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
