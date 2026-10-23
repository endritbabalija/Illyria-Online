using System;
using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Items
{
    public enum EquipBlock
    {
        None,
        /// <summary>The item isn't in the bag (or the slot is already empty).</summary>
        NotFound,
        /// <summary>Potions and other items that can't be worn.</summary>
        NotGear,
        /// <summary>Locked to other characters, or a weapon type this character can't wield.</summary>
        WrongCharacter,
        LevelTooLow,
        /// <summary>Unequipping needs a free bag slot.</summary>
        BagFull,
        /// <summary>The weapon slot can't be emptied; equip or swap to another weapon instead.</summary>
        WeaponRequired,
        /// <summary>Swap found no usable weapon of another type in the bag.</summary>
        NoOtherWeapon,
    }

    public enum PickupBlock { None, BagFull, AlreadyTaken }

    public enum ShopBlock
    {
        None,
        /// <summary>This shop doesn't sell the item (or it has no buy price).</summary>
        NotSold,
        BadCount,
        NotEnoughGold,
        BagFull,
        /// <summary>The item to sell isn't in the bag (or there are fewer than asked).</summary>
        NotFound,
        /// <summary>Worn items can't be sold; unequip first.</summary>
        Equipped,
        /// <summary>Gold would overflow.</summary>
        GoldLimit,
    }

    public enum UseBlock
    {
        None,
        Dead,
        /// <summary>None of that item in the bag.</summary>
        NoneLeft,
        NotUsable,
        Cooldown,
        /// <summary>HP (or Spirit) is already full, so drinking would waste it.</summary>
        AlreadyFull,
    }

    public enum LedgerKind { GoldIn, GoldOut, ItemIn, ItemOut }

    /// <summary>One gold or item change. Offline these are logged; from M4 the server writes them to its ledger.</summary>
    public struct LedgerEntry
    {
        public LedgerKind Kind;
        /// <summary>Why: one of <see cref="LedgerReasons"/>, optionally with a detail ("loot: Grey Wolf").</summary>
        public string Reason;
        /// <summary>Gold moved (always positive; <see cref="Kind"/> says which way).</summary>
        public int Gold;
        /// <summary>Wallet balance after the change.</summary>
        public int GoldAfter;
        /// <summary>The item instance's unique id (for a merged potion stack: the incoming instance's id).</summary>
        public string ItemId;
        public string DefId;
        public string ItemName;
        public string Rarity;
        public int Count;

        public override string ToString()
        {
            switch (Kind)
            {
                case LedgerKind.GoldIn: return $"[Gold] +{Gold} ({Reason}) → {GoldAfter}";
                case LedgerKind.GoldOut: return $"[Gold] -{Gold} ({Reason}) → {GoldAfter}";
                case LedgerKind.ItemIn: return $"[Loot] +{Count} {ItemName} [{Rarity}] #{ItemId} ({Reason})";
                default: return $"[Loot] -{Count} {ItemName} [{Rarity}] #{ItemId} ({Reason})";
            }
        }
    }

    public static class LedgerReasons
    {
        public const string Loot = "loot";
        public const string Buy = "shop buy";
        public const string Sell = "shop sell";
        public const string Potion = "potion";
        public const string StarterKit = "starter kit";
        public const string Reset = "character reset";
    }

    /// <summary>Whoever drinks a potion: HP and Spirit to top up. The player implements it in Unity.</summary>
    public interface IVitals
    {
        bool IsDead { get; }
        int Hp { get; }
        int MaxHp { get; }
        float Spirit { get; }
        int MaxSpirit { get; }
        void RestoreHp(int amount);
        void RestoreSpirit(float amount);
    }

    /// <summary>
    /// Everything one character owns and wears: gold (<see cref="Wallet"/>), the bag
    /// (<see cref="Inventory"/>), the 6 gear slots (<see cref="Equipment"/>), the 2 potion quick
    /// slots and potion cooldowns. This is the one place gold and items change: every change raises
    /// <see cref="Ledger"/>, which becomes a server call and ledger row in M4. Pure C#, time passed in.
    /// </summary>
    public sealed class PlayerItems
    {
        public const int QuickSlotCount = 2;

        readonly ItemsData data;
        readonly CombatData combat;
        readonly string[] quickSlots = new string[QuickSlotCount];
        readonly Dictionary<string, float> cooldownEnds = new Dictionary<string, float>();
        // Weapon type → id of the weapon of that type last swapped out, for Swap.
        readonly Dictionary<string, string> lastWeapon = new Dictionary<string, string>();

        public ItemsData Data => data;
        public ItemFactory Factory { get; }
        public string CharacterId { get; private set; }
        /// <summary>Level used for item requirements: items.json offline.playerLevel until levels exist (M7).</summary>
        public int PlayerLevel { get; set; }
        public Wallet Wallet { get; private set; }
        public int Gold => Wallet.Gold;
        public Inventory Bag { get; }
        public Equipment Equipment { get; } = new Equipment();
        /// <summary>Bumped on every change, so UI knows to refresh.</summary>
        public int Version { get; private set; }

        /// <summary>Anything changed: gold, bag, gear, quick slots or a potion cooldown started.</summary>
        public event Action Changed;
        /// <summary>A gear slot's item changed (stats need recomputing; the weapon slot also changes the model).</summary>
        public event Action<EquipSlot> EquipmentChanged;
        /// <summary>Every gold and item change, for the log now and the server ledger later.</summary>
        public event Action<LedgerEntry> Ledger;
        /// <summary>A potion was drunk: (item, HP restored, Spirit restored).</summary>
        public event Action<ItemDef, int, int> PotionUsed;

        /// <param name="ledger">Subscribed to <see cref="Ledger"/> before the starter kit is handed out, so it sees those entries too.</param>
        public PlayerItems(ItemsData data, CombatData combat, ItemFactory factory, string characterId, Action<LedgerEntry> ledger = null)
        {
            this.data = data;
            this.combat = combat;
            Factory = factory;
            if (ledger != null) Ledger += ledger;
            PlayerLevel = data.offline.playerLevel;
            Bag = new Inventory(data.inventory.capacity);
            Bag.Changed += Bump;
            Wallet = new Wallet();
            ResetToStarterKit(characterId);
        }

        // ---- Stats ----------------------------------------------------------------------------

        public CharacterStats ComputeStats() => StatCalculator.Compute(combat, data, CharacterId, Equipment);

        // ---- Gear -----------------------------------------------------------------------------

        /// <summary>Whether this character could wear <paramref name="item"/> (slot, character and level), ignoring where it is.</summary>
        public EquipBlock CheckWearable(ItemInstance item)
        {
            if (item == null) return EquipBlock.NotFound;
            var def = item.Def;
            if (!def.IsGear) return EquipBlock.NotGear;
            if (!def.AllowsCharacter(CharacterId)) return EquipBlock.WrongCharacter;
            if (def.IsWeapon && Array.IndexOf(combat.GetCharacter(CharacterId).weapons, def.weaponType) < 0)
                return EquipBlock.WrongCharacter;
            if (def.levelRequired > PlayerLevel) return EquipBlock.LevelTooLow;
            return EquipBlock.None;
        }

        /// <summary>Wears an item from the bag; whatever was in its slot takes the item's bag slot.</summary>
        public EquipBlock Equip(string itemId)
        {
            var index = Bag.IndexOf(itemId);
            if (index < 0) return EquipBlock.NotFound;
            var item = Bag.Get(index);
            var block = CheckWearable(item);
            if (block != EquipBlock.None) return block;

            var slot = item.Def.Slot;
            var old = Equipment.Set(slot, item);
            Bag.Replace(index, old);
            if (old != null && old.Def.IsWeapon) lastWeapon[old.Def.weaponType] = old.Id;
            EquipmentChanged?.Invoke(slot);
            Bump();
            return EquipBlock.None;
        }

        /// <summary>Takes a worn item off into the first free bag slot. The weapon slot can't be emptied.</summary>
        public EquipBlock Unequip(EquipSlot slot)
        {
            var item = Equipment.Get(slot);
            if (item == null) return EquipBlock.NotFound;
            if (slot == EquipSlot.Weapon) return EquipBlock.WeaponRequired;
            if (!Bag.CanAdd(item.Def, item.Count)) return EquipBlock.BagFull;
            Equipment.Set(slot, null);
            Bag.TryAdd(item);
            EquipmentChanged?.Invoke(slot);
            Bump();
            return EquipBlock.None;
        }

        /// <summary>
        /// The weapon Swap would pick: the character's next weapon type (in combat.json order) that has
        /// a usable weapon in the bag; of that type, the one last swapped out if it's still there,
        /// else the one with the highest average damage. Null if there is none.
        /// </summary>
        public ItemInstance FindSwapWeapon()
        {
            var types = combat.GetCharacter(CharacterId).weapons;
            var current = Equipment.Weapon;
            var start = current != null ? Array.IndexOf(types, current.Def.weaponType) : -1;
            for (var step = 1; step <= types.Length; step++)
            {
                var type = types[((start + step) % types.Length + types.Length) % types.Length];
                if (current != null && type == current.Def.weaponType) continue;

                ItemInstance best = null;
                var bestScore = float.MinValue;
                foreach (var item in Bag.Items)
                {
                    if (!item.Def.IsWeapon || item.Def.weaponType != type || CheckWearable(item) != EquipBlock.None) continue;
                    if (lastWeapon.TryGetValue(type, out var lastId) && lastId == item.Id) return item;
                    var score = AverageDamage(item);
                    if (score > bestScore)
                    {
                        best = item;
                        bestScore = score;
                    }
                }
                if (best != null) return best;
            }
            return null;
        }

        /// <summary>Swap button: wields <see cref="FindSwapWeapon"/>; the current weapon goes to the bag.</summary>
        public EquipBlock SwapWeapon()
        {
            var next = FindSwapWeapon();
            return next == null ? EquipBlock.NoOtherWeapon : Equip(next.Id);
        }

        static float AverageDamage(ItemInstance weapon)
        {
            var s = weapon.TotalStats();
            return (s.minDamage + s.maxDamage) * 0.5f;
        }

        // ---- Loot -----------------------------------------------------------------------------

        public PickupBlock CheckPickUp(LootDrop drop)
        {
            if (drop.Taken) return PickupBlock.AlreadyTaken;
            if (drop.IsGold) return Wallet.CanAdd(drop.Gold) ? PickupBlock.None : PickupBlock.BagFull;
            return Bag.CanAdd(drop.Item.Def, drop.Item.Count) ? PickupBlock.None : PickupBlock.BagFull;
        }

        /// <summary>Auto-loot: takes a drop into the wallet or bag. BagFull leaves it on the ground untouched.</summary>
        /// <param name="source">Ledger detail, e.g. the monster's name.</param>
        public PickupBlock PickUp(LootDrop drop, string source = null)
        {
            var block = CheckPickUp(drop);
            if (block != PickupBlock.None) return block;
            var reason = Reason(LedgerReasons.Loot, source);
            if (drop.IsGold)
            {
                Wallet.Add(drop.Gold);
                drop.Taken = true;
                Record(LedgerKind.GoldIn, reason, drop.Gold);
            }
            else
            {
                var item = drop.Item;
                var count = item.Count;
                Bag.TryAdd(item);
                drop.Taken = true;
                Record(LedgerKind.ItemIn, reason, 0, item, count);
            }
            Bump();
            return PickupBlock.None;
        }

        /// <summary>Adds gold from anywhere (quests, dev tools…). False for a negative amount or overflow.</summary>
        public bool AddGold(int amount, string reason)
        {
            if (amount == 0) return true;
            if (!Wallet.Add(amount)) return false;
            Record(LedgerKind.GoldIn, reason, amount);
            Bump();
            return true;
        }

        // ---- Shop -----------------------------------------------------------------------------

        public ShopBlock CheckBuy(ShopData shop, string defId, int count, out int cost)
        {
            cost = 0;
            if (count < 1) return ShopBlock.BadCount;
            var def = data.FindItem(defId);
            if (def == null || def.buyPrice <= 0 || shop.items == null || Array.IndexOf(shop.items, defId) < 0)
                return ShopBlock.NotSold;
            var total = (long)def.buyPrice * count;
            if (total > int.MaxValue) return ShopBlock.BadCount;
            cost = (int)total;
            if (!Wallet.CanSpend(cost)) return ShopBlock.NotEnoughGold;
            if (!Bag.CanAdd(def, count)) return ShopBlock.BagFull;
            return ShopBlock.None;
        }

        /// <summary>Buys <paramref name="count"/> (potions stack; gear makes one item each). Atomic: gold and bag both change or neither does.</summary>
        public ShopBlock Buy(string shopId, string defId, int count = 1)
        {
            var shop = data.GetShop(shopId);
            var block = CheckBuy(shop, defId, count, out var cost);
            if (block != ShopBlock.None) return block;

            var def = data.GetItem(defId);
            var bought = new List<ItemInstance>();
            for (var left = count; left > 0;)
            {
                var n = Math.Min(left, Math.Max(1, def.stackSize));
                bought.Add(Factory.Create(def, n));
                left -= n;
            }
            var counts = bought.ConvertAll(i => i.Count);

            // Both checks passed, but stay atomic anyway: if the items don't go in, the gold comes back.
            if (!Wallet.TrySpend(cost)) return ShopBlock.NotEnoughGold;
            if (!Bag.TryAddAll(bought))
            {
                Wallet.Add(cost);
                return ShopBlock.BagFull;
            }

            var reason = Reason(LedgerReasons.Buy, shop.name);
            Record(LedgerKind.GoldOut, reason, cost);
            for (var i = 0; i < bought.Count; i++) Record(LedgerKind.ItemIn, reason, 0, bought[i], counts[i]);
            Bump();
            return ShopBlock.None;
        }

        /// <summary>Gold an NPC would pay for <paramref name="count"/> of a bag item.</summary>
        public int SellValue(ItemInstance item, int count) => item.SellPriceEach * count;

        public ShopBlock CheckSell(string itemId, int count, out int value)
        {
            value = 0;
            if (count < 1) return ShopBlock.BadCount;
            if (Equipment.IsEquipped(itemId)) return ShopBlock.Equipped;
            var item = Bag.Find(itemId);
            if (item == null || item.Count < count) return ShopBlock.NotFound;
            var total = (long)item.SellPriceEach * count;
            if (total > int.MaxValue || !Wallet.CanAdd((int)total)) return ShopBlock.GoldLimit;
            value = (int)total;
            return ShopBlock.None;
        }

        /// <summary>Sells from the bag to any NPC shop. Worn items can't be sold.</summary>
        public ShopBlock Sell(string itemId, int count = 1)
        {
            var block = CheckSell(itemId, count, out var value);
            if (block != ShopBlock.None) return block;
            Wallet.Add(value);
            var item = Bag.Take(itemId, count);
            Record(LedgerKind.ItemOut, LedgerReasons.Sell, 0, item, count);
            Record(LedgerKind.GoldIn, LedgerReasons.Sell, value);
            Bump();
            return ShopBlock.None;
        }

        // ---- Potions --------------------------------------------------------------------------

        /// <summary>The item id on a quick slot (may be empty or have none left in the bag).</summary>
        public string QuickSlot(int index) => quickSlots[index];

        /// <summary>How many of the quick slot's item are in the bag.</summary>
        public int QuickSlotItemCount(int index) => string.IsNullOrEmpty(quickSlots[index]) ? 0 : Bag.CountOf(quickSlots[index]);

        /// <summary>Puts a usable item (by id) on a quick slot; null or empty clears it.</summary>
        public bool SetQuickSlot(int index, string defId)
        {
            if (index < 0 || index >= QuickSlotCount) return false;
            if (!string.IsNullOrEmpty(defId))
            {
                var def = data.FindItem(defId);
                if (def == null || !def.IsUsable) return false;
            }
            quickSlots[index] = defId;
            Bump();
            return true;
        }

        /// <summary>
        /// Puts a usable item (by id) on a quick slot from the bag UI. If it already sits on another
        /// slot, the two slots trade places, so a potion is never on both. False for anything not usable.
        /// </summary>
        public bool AssignQuickSlot(int index, string defId)
        {
            if (index < 0 || index >= QuickSlotCount || string.IsNullOrEmpty(defId)) return false;
            var def = data.FindItem(defId);
            if (def == null || !def.IsUsable) return false;
            if (quickSlots[index] == defId) return true;
            var other = Array.IndexOf(quickSlots, defId);
            if (other >= 0) quickSlots[other] = quickSlots[index];
            quickSlots[index] = defId;
            Bump();
            return true;
        }

        /// <summary>Seconds until a potion (by item id) can be drunk again.</summary>
        public float CooldownLeft(string defId, float now)
        {
            var def = data.FindItem(defId);
            if (def?.use == null) return 0f;
            return cooldownEnds.TryGetValue(CooldownKey(def), out var end) ? Math.Max(0f, end - now) : 0f;
        }

        public UseBlock CheckUse(ItemDef def, float now, IVitals vitals)
        {
            if (vitals.IsDead) return UseBlock.Dead;
            if (def == null || !def.IsUsable) return UseBlock.NotUsable;
            if (Bag.CountOf(def.id) <= 0) return UseBlock.NoneLeft;
            if (CooldownLeft(def.id, now) > 0f) return UseBlock.Cooldown;
            var hpNeeded = def.use.hp > 0 && vitals.Hp < vitals.MaxHp;
            var spiritNeeded = def.use.spirit > 0 && vitals.Spirit < vitals.MaxSpirit;
            return hpNeeded || spiritNeeded ? UseBlock.None : UseBlock.AlreadyFull;
        }

        public UseBlock UseQuickSlot(int index, float now, IVitals vitals)
        {
            var defId = index >= 0 && index < QuickSlotCount ? quickSlots[index] : null;
            return string.IsNullOrEmpty(defId) ? UseBlock.NotUsable : Use(defId, now, vitals);
        }

        /// <summary>Drinks one of a potion from the bag: restores HP/Spirit up to max and starts its cooldown group.</summary>
        public UseBlock Use(string defId, float now, IVitals vitals)
        {
            var def = data.FindItem(defId);
            var block = CheckUse(def, now, vitals);
            if (block != UseBlock.None) return block;

            var hp = Math.Max(0, Math.Min(def.use.hp, vitals.MaxHp - vitals.Hp));
            var spirit = Math.Max(0, Math.Min(def.use.spirit, (int)Math.Ceiling(vitals.MaxSpirit - vitals.Spirit)));
            var stack = Bag.FindFirst(defId);
            Bag.Take(stack.Id, 1);
            Record(LedgerKind.ItemOut, LedgerReasons.Potion, 0, stack, 1);
            if (hp > 0) vitals.RestoreHp(hp);
            if (spirit > 0) vitals.RestoreSpirit(spirit);
            cooldownEnds[CooldownKey(def)] = now + def.use.cooldown;
            PotionUsed?.Invoke(def, hp, spirit);
            Bump();
            return UseBlock.None;
        }

        static string CooldownKey(ItemDef def) => string.IsNullOrEmpty(def.use.cooldownGroup) ? def.id : def.use.cooldownGroup;

        // ---- Character ------------------------------------------------------------------------

        /// <summary>
        /// Empties everything and hands out the character's starter kit (gold, worn gear, bag items).
        /// Used at start and by the dev "play the other character" button until saves exist (M4).
        /// </summary>
        public void ResetToStarterKit(string characterId)
        {
            combat.GetCharacter(characterId); // throws on an unknown id
            var kit = data.GetStarterKit(characterId);

            foreach (var item in Equipment.Items) Record(LedgerKind.ItemOut, LedgerReasons.Reset, 0, item, item.Count);
            foreach (var item in Bag.Items) Record(LedgerKind.ItemOut, LedgerReasons.Reset, 0, item, item.Count);
            if (Gold > 0)
            {
                var gold = Gold;
                Wallet = new Wallet();
                Record(LedgerKind.GoldOut, LedgerReasons.Reset, gold);
            }
            Equipment.Clear();
            Bag.Clear();
            cooldownEnds.Clear();
            lastWeapon.Clear();
            CharacterId = characterId;

            if (kit.gold > 0) AddGold(kit.gold, LedgerReasons.StarterKit);
            if (kit.equip != null)
                foreach (var id in kit.equip)
                {
                    var item = Factory.Create(id);
                    var block = CheckWearable(item);
                    if (block != EquipBlock.None)
                        throw new InvalidOperationException($"Starter kit for {characterId}: can't wear {id} ({block})");
                    Equipment.Set(item.Def.Slot, item);
                    Record(LedgerKind.ItemIn, LedgerReasons.StarterKit, 0, item, item.Count);
                }
            if (kit.bag != null)
                foreach (var entry in kit.bag)
                {
                    var item = Factory.Create(entry.item, entry.count);
                    var count = item.Count;
                    if (!Bag.TryAdd(item)) throw new InvalidOperationException($"Starter kit for {characterId} doesn't fit the bag");
                    Record(LedgerKind.ItemIn, LedgerReasons.StarterKit, 0, item, count);
                }

            var defaults = data.inventory.quickSlots;
            for (var i = 0; i < QuickSlotCount; i++) quickSlots[i] = i < defaults.Length ? defaults[i] : null;

            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
                if (slot != EquipSlot.None) EquipmentChanged?.Invoke(slot);
            Bump();
        }

        // ---- Online ---------------------------------------------------------------------------

        /// <summary>
        /// Online: replaces everything with the server's copy (its item ids, rolls and counts). No
        /// ledger entries: the server keeps the ledger. Raises <see cref="EquipmentChanged"/> for
        /// every slot whose item changed and potion cooldowns follow the server's.
        /// </summary>
        /// <param name="bag">Bag slots in order; null for an empty slot.</param>
        /// <param name="worn">Worn items by <see cref="EquipSlot"/> index; null for an empty slot.</param>
        /// <param name="potionCooldowns">Seconds left per cooldown group.</param>
        public void ApplyServerState(string characterId, int gold, IList<ItemInstance> bag, IList<ItemInstance> worn,
            IList<string> quickSlotIds, IEnumerable<KeyValuePair<string, float>> potionCooldowns, float now)
        {
            CharacterId = characterId;
            Wallet = new Wallet(Math.Max(0, gold));
            Bag.Clear();
            for (var i = 0; i < bag.Count && i < Bag.Capacity; i++)
                if (bag[i] != null) Bag.Replace(i, bag[i]);

            var changed = new List<EquipSlot>();
            for (var s = 0; s < Equipment.SlotCount; s++)
            {
                var slot = (EquipSlot)s;
                var item = s < worn.Count ? worn[s] : null;
                var old = Equipment.Get(slot);
                if ((old == null ? null : old.Id) != (item == null ? null : item.Id)) changed.Add(slot);
                Equipment.Set(slot, item);
            }

            for (var i = 0; i < QuickSlotCount; i++)
                quickSlots[i] = i < quickSlotIds.Count && !string.IsNullOrEmpty(quickSlotIds[i]) ? quickSlotIds[i] : null;

            cooldownEnds.Clear();
            foreach (var pair in potionCooldowns) cooldownEnds[pair.Key] = now + pair.Value;

            foreach (var slot in changed) EquipmentChanged?.Invoke(slot);
            Bump();
        }

        // ---- Ledger ---------------------------------------------------------------------------

        static string Reason(string reason, string detail) => string.IsNullOrEmpty(detail) ? reason : reason + ": " + detail;

        void Record(LedgerKind kind, string reason, int gold, ItemInstance item = null, int count = 0)
        {
            Ledger?.Invoke(new LedgerEntry
            {
                Kind = kind,
                Reason = reason,
                Gold = gold,
                GoldAfter = Gold,
                ItemId = item?.Id,
                DefId = item?.DefId,
                ItemName = item?.Name,
                Rarity = item?.Rarity?.name,
                Count = count,
            });
        }

        void Bump()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
