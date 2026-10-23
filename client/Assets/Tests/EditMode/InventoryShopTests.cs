using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wuxia.Data;
using Wuxia.Items;

namespace Wuxia.Tests
{
    /// <summary>Bag capacity and stacking, pickups, the wallet, NPC shop buy/sell, potions and the ledger.</summary>
    public class InventoryShopTests
    {
        const string Shop = "han_keeper";

        static ItemsData data;

        [OneTimeSetUp]
        public void LoadData() => data = ItemDataTests.Load();

        sealed class Vitals : IVitals
        {
            public bool IsDead { get; set; }
            public int Hp { get; set; }
            public int MaxHp { get; set; } = 200;
            public float Spirit { get; set; }
            public int MaxSpirit { get; set; } = 100;
            public void RestoreHp(int amount) => Hp += amount;
            public void RestoreSpirit(float amount) => Spirit += amount;
        }

        static PlayerItems NewPlayer(List<LedgerEntry> ledger = null) => EquipmentTests.NewPlayer("kreshnik", ledger);

        static void FillBagWithGear(PlayerItems items)
        {
            while (!items.Bag.IsFull)
                Assert.That(items.PickUp(LootDrop.OfItem(items.Factory.Create("cloth_hood"))), Is.EqualTo(PickupBlock.None));
        }

        // ---- Inventory ----

        [Test]
        public void Bag_HoldsCapacityItems_ThenRefuses()
        {
            var bag = new Inventory(data.inventory.capacity);
            var factory = new ItemFactory(data);
            for (var i = 0; i < bag.Capacity; i++) Assert.That(bag.TryAdd(factory.Create("cloth_hood")), $"slot {i}");
            Assert.That(bag.IsFull);
            var version = bag.Version;
            Assert.That(bag.TryAdd(factory.Create("cloth_hood")), Is.False);
            Assert.That(bag.TryAdd(factory.Create("hp_potion")), Is.False);
            Assert.That(bag.Version, Is.EqualTo(version), "a refused add changes nothing");
        }

        [Test]
        public void Potions_Stack_UpToStackSize()
        {
            var bag = new Inventory(3);
            var factory = new ItemFactory(data);
            var size = data.GetItem("hp_potion").stackSize;

            Assert.That(bag.TryAdd(factory.Create("hp_potion", size - 2)));
            Assert.That(bag.TryAdd(factory.Create("hp_potion", 5)));
            Assert.That(bag.CountOf("hp_potion"), Is.EqualTo(size + 3));
            Assert.That(bag.FreeSlots, Is.EqualTo(1), "topped up the first stack, then started a second");
            Assert.That(bag.Get(0).Count, Is.EqualTo(size));

            Assert.That(bag.CanAdd(data.GetItem("hp_potion"), (size - 3) + size), Is.True);
            Assert.That(bag.CanAdd(data.GetItem("hp_potion"), (size - 3) + size + 1), Is.False);
            Assert.That(bag.CanAdd(data.GetItem("cloth_hood"), 2), Is.False);
        }

        [Test]
        public void Move_SwapsOrMergesSlots()
        {
            var bag = new Inventory(4);
            var factory = new ItemFactory(data);
            var hood = factory.Create("cloth_hood");
            bag.TryAdd(hood);
            Assert.That(bag.Move(0, 3));
            Assert.That(bag.Get(3), Is.SameAs(hood));
            Assert.That(bag.Get(0), Is.Null);

            bag.TryAdd(factory.Create("hp_potion", 4)); // slot 0
            var vest = factory.Create("padded_vest");
            bag.TryAdd(vest); // slot 1
            Assert.That(bag.Move(1, 3));
            Assert.That(bag.Get(3), Is.SameAs(vest));
            Assert.That(bag.Get(1), Is.SameAs(hood));

            // Moving a potion onto a full stack swaps them; onto a partial stack, it merges.
            var potion = data.GetItem("hp_potion");
            var stacks = new Inventory(2);
            stacks.TryAdd(factory.Create(potion, potion.stackSize)); // slot 0: full
            stacks.TryAdd(factory.Create(potion, 4));                // slot 1: 4
            Assert.That(stacks.Move(1, 0));
            Assert.That(stacks.Get(0).Count, Is.EqualTo(4), "swapped with the full stack");
            Assert.That(stacks.Get(1).Count, Is.EqualTo(potion.stackSize));
            Assert.That(stacks.Move(1, 0));
            Assert.That(stacks.Get(0).Count, Is.EqualTo(potion.stackSize), "merged as much as fits");
            Assert.That(stacks.Get(1).Count, Is.EqualTo(4));
            Assert.That(stacks.CountOf(potion.id), Is.EqualTo(potion.stackSize + 4));
        }

        [Test]
        public void PickUp_WithAFullBag_LeavesTheDrop_ButGoldStillFits()
        {
            var items = NewPlayer();
            FillBagWithGear(items);
            var drop = LootDrop.OfItem(items.Factory.Create("iron_sword"));
            Assert.That(items.PickUp(drop), Is.EqualTo(PickupBlock.BagFull));
            Assert.That(drop.Taken, Is.False, "it stays on the ground");

            var gold = items.Gold;
            var coins = LootDrop.OfGold(7);
            Assert.That(items.PickUp(coins), Is.EqualTo(PickupBlock.None));
            Assert.That(items.Gold, Is.EqualTo(gold + 7));
            Assert.That(items.PickUp(coins), Is.EqualTo(PickupBlock.AlreadyTaken), "a drop can't be taken twice");
            Assert.That(items.Gold, Is.EqualTo(gold + 7));
        }

        [Test]
        public void PotionDrop_TopsUpAStack_EvenWithNoFreeSlot()
        {
            var items = NewPlayer();
            FillBagWithGear(items);
            var before = items.Bag.CountOf("hp_potion");
            Assert.That(items.PickUp(LootDrop.OfItem(items.Factory.Create("hp_potion", 2))), Is.EqualTo(PickupBlock.None));
            Assert.That(items.Bag.CountOf("hp_potion"), Is.EqualTo(before + 2));
        }

        // ---- Wallet ----

        [Test]
        public void Wallet_NeverGoesNegative()
        {
            var wallet = new Wallet(10);
            Assert.That(wallet.TrySpend(11), Is.False);
            Assert.That(wallet.Add(-5), Is.False);
            Assert.That(wallet.TrySpend(-5), Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(10));
            Assert.That(wallet.TrySpend(10));
            Assert.That(wallet.Gold, Is.Zero);
            Assert.That(new Wallet(int.MaxValue).Add(1), Is.False, "no overflow");
        }

        // ---- Shop ----

        [Test]
        public void Buy_TakesGold_AndGivesItems()
        {
            var ledger = new List<LedgerEntry>();
            var items = NewPlayer(ledger);
            ledger.Clear();
            var price = data.GetItem("hp_potion").buyPrice;
            var gold = items.Gold;
            var potions = items.Bag.CountOf("hp_potion");

            Assert.That(items.Buy(Shop, "hp_potion", 3), Is.EqualTo(ShopBlock.None));
            Assert.That(items.Gold, Is.EqualTo(gold - 3 * price));
            Assert.That(items.Bag.CountOf("hp_potion"), Is.EqualTo(potions + 3));
            Assert.That(ledger.Count(e => e.Kind == LedgerKind.GoldOut && e.Gold == 3 * price), Is.EqualTo(1));
            Assert.That(ledger.Count(e => e.Kind == LedgerKind.ItemIn && e.DefId == "hp_potion" && e.Count == 3), Is.EqualTo(1));

            Assert.That(items.Buy(Shop, "cloth_hood"), Is.EqualTo(ShopBlock.None));
            Assert.That(items.Bag.FindFirst("cloth_hood").Rarity.id, Is.EqualTo(ItemsData.CommonRarity));
        }

        [Test]
        public void Buy_WithoutEnoughGold_ChangesNothing()
        {
            var items = NewPlayer();
            var gold = items.Gold;
            var version = items.Bag.Version;
            var tooMany = gold / data.GetItem("padded_vest").buyPrice + 1;
            Assert.That(items.Buy(Shop, "padded_vest", tooMany), Is.EqualTo(ShopBlock.NotEnoughGold));
            Assert.That(items.Gold, Is.EqualTo(gold));
            Assert.That(items.Bag.Version, Is.EqualTo(version));
        }

        [Test]
        public void Buy_WithAFullBag_ChangesNothing()
        {
            var items = NewPlayer();
            FillBagWithGear(items);
            items.AddGold(10000, "test");
            var gold = items.Gold;
            Assert.That(items.Buy(Shop, "padded_vest"), Is.EqualTo(ShopBlock.BagFull));
            Assert.That(items.Buy(Shop, "spirit_potion", data.GetItem("spirit_potion").stackSize), Is.EqualTo(ShopBlock.BagFull),
                "a full stack's worth doesn't fit on top of the starter stack");
            Assert.That(items.Gold, Is.EqualTo(gold));
            Assert.That(items.Bag.CountOf("padded_vest"), Is.Zero);
        }

        [Test]
        public void Buy_OnlyWhatTheShopSells()
        {
            var items = NewPlayer();
            items.AddGold(100000, "test");
            Assert.That(items.Buy(Shop, "iron_sword"), Is.EqualTo(ShopBlock.NotSold));
            Assert.That(items.Buy(Shop, "no_such_item"), Is.EqualTo(ShopBlock.NotSold));
            Assert.That(items.Buy(Shop, "hp_potion", 0), Is.EqualTo(ShopBlock.BadCount));
        }

        [Test]
        public void Buy_MoreThanAStack_SplitsIntoStacks()
        {
            var items = NewPlayer();
            items.AddGold(100000, "test");
            var size = data.GetItem("hp_potion").stackSize;
            var before = items.Bag.CountOf("hp_potion");
            Assert.That(items.Buy(Shop, "hp_potion", size + 1), Is.EqualTo(ShopBlock.None));
            Assert.That(items.Bag.CountOf("hp_potion"), Is.EqualTo(before + size + 1));
        }

        [Test]
        public void Sell_PaysTheSellPrice_TimesRarity_AndNeverWornItems()
        {
            var ledger = new List<LedgerEntry>();
            var items = NewPlayer(ledger);
            var gold = items.Gold;

            Assert.That(items.Sell(items.Equipment.Weapon.Id), Is.EqualTo(ShopBlock.Equipped));
            Assert.That(items.Gold, Is.EqualTo(gold));

            var stack = items.Bag.FindFirst("hp_potion");
            var count = stack.Count;
            Assert.That(items.Sell(stack.Id, count + 1), Is.EqualTo(ShopBlock.NotFound));
            Assert.That(items.Sell(stack.Id, 2), Is.EqualTo(ShopBlock.None));
            Assert.That(items.Gold, Is.EqualTo(gold + 2 * data.GetItem("hp_potion").sellPrice));
            Assert.That(items.Bag.CountOf("hp_potion"), Is.EqualTo(count - 2));

            var rare = data.GetRarity("rare");
            var ring = items.Factory.Create(data.GetItem("copper_ring"), 1, rare, items.Factory.RollBonusLines(data.GetItem("copper_ring"), 2, new System.Random(1)));
            items.PickUp(LootDrop.OfItem(ring));
            gold = items.Gold;
            Assert.That(items.Sell(ring.Id), Is.EqualTo(ShopBlock.None));
            Assert.That(items.Gold, Is.EqualTo(gold + (int)(data.GetItem("copper_ring").sellPrice * rare.sellMultiplier)));
            Assert.That(items.Bag.Find(ring.Id), Is.Null);
            Assert.That(ledger.Any(e => e.Kind == LedgerKind.ItemOut && e.ItemId == ring.Id && e.Reason == LedgerReasons.Sell));
        }

        // ---- Potions ----

        [Test]
        public void HpPotion_Heals_UpToMax_AndUsesOne()
        {
            var items = NewPlayer();
            var heal = data.GetItem("hp_potion").use.hp;
            var vitals = new Vitals { Hp = 10, Spirit = 100 };
            var count = items.Bag.CountOf("hp_potion");

            Assert.That(items.UseQuickSlot(0, 0f, vitals), Is.EqualTo(UseBlock.None));
            Assert.That(vitals.Hp, Is.EqualTo(10 + heal));
            Assert.That(items.Bag.CountOf("hp_potion"), Is.EqualTo(count - 1));

            vitals.Hp = vitals.MaxHp - 5;
            var cd = data.GetItem("hp_potion").use.cooldown;
            Assert.That(items.Use("hp_potion", cd, vitals), Is.EqualTo(UseBlock.None));
            Assert.That(vitals.Hp, Is.EqualTo(vitals.MaxHp), "capped at max");
        }

        [Test]
        public void Potions_HaveACooldown_PerGroup()
        {
            var items = NewPlayer();
            var hp = data.GetItem("hp_potion");
            var spirit = data.GetItem("spirit_potion");
            var vitals = new Vitals { Hp = 1, Spirit = 0 };

            Assert.That(items.Use(hp.id, 100f, vitals), Is.EqualTo(UseBlock.None));
            Assert.That(items.Use(hp.id, 100f + hp.use.cooldown - 0.01f, vitals), Is.EqualTo(UseBlock.Cooldown));
            Assert.That(items.CooldownLeft(hp.id, 100f), Is.EqualTo(hp.use.cooldown).Within(1e-4));

            var shared = hp.use.cooldownGroup == spirit.use.cooldownGroup;
            Assert.That(items.Use(spirit.id, 100f, vitals), Is.EqualTo(shared ? UseBlock.Cooldown : UseBlock.None),
                "potions share a cooldown only when they share a cooldownGroup");
            Assert.That(items.Use(hp.id, 100f + hp.use.cooldown, vitals), Is.EqualTo(UseBlock.None));
        }

        [Test]
        public void SpiritPotion_RestoresSpirit_CappedAtMax()
        {
            var items = NewPlayer();
            var vitals = new Vitals { Hp = 200, Spirit = 90 };
            Assert.That(items.UseQuickSlot(1, 0f, vitals), Is.EqualTo(UseBlock.None));
            Assert.That(vitals.Spirit, Is.EqualTo(100f));
        }

        [Test]
        public void Potions_CantBeUsed_WhenDead_Full_OrOut()
        {
            var items = NewPlayer();
            Assert.That(items.UseQuickSlot(0, 0f, new Vitals { Hp = 0, IsDead = true }), Is.EqualTo(UseBlock.Dead));
            Assert.That(items.UseQuickSlot(0, 0f, new Vitals { Hp = 200, Spirit = 0 }), Is.EqualTo(UseBlock.AlreadyFull),
                "an HP potion at full HP would be wasted");

            var vitals = new Vitals { Hp = 1 };
            var t = 0f;
            while (items.Bag.CountOf("hp_potion") > 0)
            {
                vitals.Hp = 1;
                Assert.That(items.UseQuickSlot(0, t, vitals), Is.EqualTo(UseBlock.None));
                t += 1000f;
            }
            Assert.That(items.UseQuickSlot(0, t, vitals), Is.EqualTo(UseBlock.NoneLeft));
            Assert.That(items.QuickSlotItemCount(0), Is.Zero);
            Assert.That(items.SetQuickSlot(0, "cloth_hood"), Is.False, "only usable items go on quick slots");
        }

        [Test]
        public void AssignQuickSlot_PutsAPotionOnASlot_AndSwapsWhenItWasOnTheOther()
        {
            var items = NewPlayer();
            var defaults = data.inventory.quickSlots;
            Assert.That(items.QuickSlot(0), Is.EqualTo(defaults[0]), "starts from items.json");
            Assert.That(items.QuickSlot(1), Is.EqualTo(defaults[1]));

            var version = items.Version;
            Assert.That(items.AssignQuickSlot(0, defaults[1]), Is.True);
            Assert.That(items.QuickSlot(0), Is.EqualTo(defaults[1]));
            Assert.That(items.QuickSlot(1), Is.EqualTo(defaults[0]), "the slots traded places");
            Assert.That(items.Version, Is.GreaterThan(version), "the potion buttons refresh");

            Assert.That(items.AssignQuickSlot(0, defaults[0]), Is.True);
            Assert.That(items.QuickSlot(0), Is.EqualTo(defaults[0]), "and back");
            Assert.That(items.QuickSlot(1), Is.EqualTo(defaults[1]));

            Assert.That(items.AssignQuickSlot(1, "cloth_hood"), Is.False, "only usable items");
            Assert.That(items.AssignQuickSlot(2, defaults[0]), Is.False, "no third slot");
            Assert.That(items.QuickSlot(1), Is.EqualTo(defaults[1]));

            items.AssignQuickSlot(0, defaults[1]);
            items.ResetToStarterKit("kreshnik");
            Assert.That(items.QuickSlot(0), Is.EqualTo(defaults[0]), "a character reset restores the data defaults");
        }

        [Test]
        public void MovingInTheBag_ChangesNoOwnership_ButRefreshesTheUi()
        {
            var ledger = new List<LedgerEntry>();
            var items = NewPlayer(ledger);
            ledger.Clear();
            var first = items.Bag.Get(0);
            var version = items.Version;
            Assert.That(items.Bag.Move(0, items.Bag.Capacity - 1));
            Assert.That(items.Bag.Get(items.Bag.Capacity - 1), Is.SameAs(first));
            Assert.That(items.Version, Is.GreaterThan(version));
            Assert.That(ledger, Is.Empty, "a move isn't a gold or item change");
        }

        // ---- Ledger ----

        [Test]
        public void EveryGoldAndItemChange_IsInTheLedger()
        {
            var ledger = new List<LedgerEntry>();
            var items = NewPlayer(ledger);
            Assert.That(ledger.Count(e => e.Reason == LedgerReasons.StarterKit && e.Kind == LedgerKind.ItemIn),
                Is.EqualTo(data.GetStarterKit("kreshnik").equip.Length + data.GetStarterKit("kreshnik").bag.Length));

            ledger.Clear();
            items.PickUp(LootDrop.OfGold(5), "Grey Wolf");
            items.Buy(Shop, "spirit_potion");
            items.Sell(items.Bag.FindFirst("worn_bow").Id);
            items.Use("hp_potion", 0f, new Vitals { Hp = 1 });
            Assert.That(ledger.Select(e => e.Kind), Is.EqualTo(new[]
            {
                LedgerKind.GoldIn,                       // loot
                LedgerKind.GoldOut, LedgerKind.ItemIn,   // buy
                LedgerKind.ItemOut, LedgerKind.GoldIn,   // sell
                LedgerKind.ItemOut,                      // potion
            }));
            Assert.That(ledger[0].Reason, Is.EqualTo(LedgerReasons.Loot + ": Grey Wolf"));
            Assert.That(ledger[0].GoldAfter, Is.EqualTo(data.GetStarterKit("kreshnik").gold + 5));
            Assert.That(ledger.Where(e => e.Kind == LedgerKind.ItemIn || e.Kind == LedgerKind.ItemOut).All(e => !string.IsNullOrEmpty(e.ItemId)));
        }
    }
}
