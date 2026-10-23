using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.Items;
using Wuxia.Player;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>
    /// The bag: 30 slots, the 6 gear slots, a stats summary, and a card for the tapped item with
    /// Equip / Unequip / Use, Move, the potion quick-slot buttons, plus Sell while a shop session is
    /// open. Moving is tap-based: Move on the card, then tap the destination bag slot (empty: moves
    /// there; same potion: merges; anything else: swaps). Tapping the item itself, a gear slot, the
    /// panel background or closing cancels. Rebuilds only when the items' version, the stats, the
    /// shop session, the selection or the move change, never every frame.
    /// </summary>
    public class InventoryPanel : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] PlayerCombat combat;
        [Tooltip("Item icons (Assets/Items/ItemModels.asset).")]
        [SerializeField] ItemModelRegistry icons;
        [SerializeField] ShopPanel shop;
        [SerializeField] SkillsPanel skillsPanel;
        [SerializeField] Text titleText;
        [SerializeField] Text goldText;
        [SerializeField] Text bagCountText;
        [Tooltip("Disabled slot used as the template for the bag grid.")]
        [SerializeField] ItemSlotView slotTemplate;
        [Tooltip("Gear slots in EquipSlot order: weapon, helm, armor, boots, ring, amulet.")]
        [SerializeField] ItemSlotView[] gearSlots = new ItemSlotView[Equipment.SlotCount];
        [SerializeField] Text statsLeft;
        [SerializeField] Text statsRight;
        [SerializeField] ItemDetailCard detail;
        [SerializeField] Button closeButton;

        readonly List<ItemSlotView> bagSlots = new List<ItemSlotView>();
        readonly StringBuilder sb = new StringBuilder();
        string selectedId;
        // The item being moved and the bag slot it was in; null / -1 when not moving.
        string movingId;
        int movingFrom = -1;
        int shownVersion = -1;
        bool shownSelling;
        bool statsDirty = true;

        public bool IsOpen => gameObject.activeSelf;
        public bool IsMoving => movingId != null;

        void Awake()
        {
            slotTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(Close);
            for (var i = 0; i < gearSlots.Length; i++)
            {
                var slot = (EquipSlot)i;
                gearSlots[i].SetCaption(ItemText.SlotName(slot));
                gearSlots[i].Button.onClick.AddListener(() =>
                {
                    StopMove();
                    Select(combat.Items.Equipment.Get(slot));
                });
            }
            detail.PrimaryButton.onClick.AddListener(OnPrimary);
            detail.SellButton.onClick.AddListener(() => SellSelected(false));
            detail.SellAllButton.onClick.AddListener(() => SellSelected(true));
            detail.MoveButton.onClick.AddListener(ToggleMove);
            for (var i = 0; i < detail.QuickSlotButtonCount; i++)
            {
                var index = i;
                detail.QuickSlotButton(i).onClick.AddListener(() => AssignQuickSlot(index));
            }
        }

        void OnEnable() => combat.StatsChanged += MarkStats;
        void OnDisable() => combat.StatsChanged -= MarkStats;

        void MarkStats() => statsDirty = true;

        public void Open()
        {
            if (skillsPanel != null) skillsPanel.Close();
            if (shop.InSession) shop.HideCatalog();
            gameObject.SetActive(true);
            shownVersion = -1;
            statsDirty = true;
        }

        /// <summary>Closes the bag; during a shop session the shop's catalog comes back.</summary>
        public void Close()
        {
            if (!gameObject.activeSelf) return;
            StopMove();
            gameObject.SetActive(false);
            if (shop.InSession) shop.ShowCatalog();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        void Update()
        {
            var items = combat.Items;
            if (bagSlots.Count != items.Bag.Capacity) BuildBag();
            if (items.Version == shownVersion && shop.InSession == shownSelling && !statsDirty) return;
            shownVersion = items.Version;
            shownSelling = shop.InSession;
            if (statsDirty)
            {
                statsDirty = false;
                RefreshStats();
            }
            Refresh();
        }

        void BuildBag()
        {
            foreach (var s in bagSlots) Destroy(s.gameObject);
            bagSlots.Clear();
            var capacity = combat.Items.Bag.Capacity;
            for (var i = 0; i < capacity; i++)
            {
                var view = Instantiate(slotTemplate, slotTemplate.transform.parent);
                view.name = "Slot" + (i + 1);
                view.gameObject.SetActive(true);
                var index = i;
                view.Button.onClick.AddListener(() => OnBagSlot(index));
                bagSlots.Add(view);
            }
        }

        void Select(ItemInstance item)
        {
            selectedId = item != null ? item.Id : null;
            Refresh();
        }

        void OnBagSlot(int index)
        {
            var bag = combat.Items.Bag;
            if (!IsMoving)
            {
                Select(bag.Get(index));
                return;
            }
            // The stack may have shifted or gone since Move was pressed (a potion drunk, a sale).
            var from = bag.Get(movingFrom) != null && bag.Get(movingFrom).Id == movingId ? movingFrom : bag.IndexOf(movingId);
            StopMove();
            if (from >= 0 && from != index) bag.Move(from, index);
            // Follow the item to where it went (or the stack it merged into).
            Select(bag.Get(index) ?? bag.Find(selectedId));
        }

        void ToggleMove()
        {
            if (IsMoving)
            {
                StopMove();
                Refresh();
                return;
            }
            var item = Selected();
            var from = item != null ? combat.Items.Bag.IndexOf(item.Id) : -1;
            if (from < 0) return; // worn items don't move
            movingId = item.Id;
            movingFrom = from;
            Refresh();
        }

        /// <summary>Cancels a move in progress (tap outside the bag grid, closing, the backdrop).</summary>
        public void CancelMove()
        {
            if (!IsMoving) return;
            StopMove();
            Refresh();
        }

        void StopMove()
        {
            movingId = null;
            movingFrom = -1;
        }

        // Taps on the panel itself (not on a slot or button) cancel a move.
        public void OnPointerClick(PointerEventData eventData) => CancelMove();

        void AssignQuickSlot(int index)
        {
            var item = Selected();
            if (item == null || !item.Def.IsUsable) return;
            if (combat.Items.AssignQuickSlot(index, item.DefId))
                combat.Tell($"{item.Def.name} on quick slot {index + 1}");
        }

        void Refresh()
        {
            var items = combat.Items;
            var bag = items.Bag;
            var selling = shop.InSession;
            var moving = IsMoving ? bag.Find(movingId) : null;
            if (IsMoving && moving == null) StopMove(); // sold, drunk or otherwise gone
            titleText.text = moving != null
                ? $"Bag   <size=26><color={ItemText.Muted}>tap a slot to move {moving.Name} there</color></size>"
                : selling ? $"Bag   <size=26><color={ItemText.Muted}>selling to {shop.Shop.name}</color></size>" : "Bag";
            goldText.text = $"<color={ItemText.GoldHex}>{items.Gold}</color> gold";
            bagCountText.text = $"{bag.Capacity - bag.FreeSlots} / {bag.Capacity}";

            ItemInstance selected = null;
            var selectedWorn = false;
            for (var i = 0; i < bagSlots.Count; i++)
            {
                var item = bag.Get(i);
                bagSlots[i].Show(item, icons);
                var isSelected = item != null && item.Id == selectedId;
                bagSlots[i].SetSelected(isSelected, item != null && item == moving);
                if (isSelected) selected = item;
            }
            for (var i = 0; i < gearSlots.Length; i++)
            {
                var item = items.Equipment.Get((EquipSlot)i);
                gearSlots[i].Show(item, icons);
                var isSelected = item != null && item.Id == selectedId;
                gearSlots[i].SetSelected(isSelected);
                if (isSelected)
                {
                    selected = item;
                    selectedWorn = true;
                }
            }

            if (selected == null)
            {
                selectedId = null;
                detail.ShowNothing(selling);
            }
            else detail.Show(selected, selectedWorn, combat, selling, icons, moving != null && moving == selected);
        }

        void RefreshStats()
        {
            var s = combat.Stats;
            var a = s.Attack;
            sb.Clear();
            Line("Damage", $"{a.minDamage}–{a.maxDamage}");
            Line("Accuracy", ItemText.Percent(a.accuracy));
            Line("Crit", ItemText.Percent(a.critChance));
            Line("Defense", s.Defense.ToString());
            Line("Dodge", ItemText.Percent(s.Dodge));
            Line("HP", s.MaxHp.ToString());
            statsLeft.text = sb.ToString().TrimEnd('\n');

            sb.Clear();
            Line("Spirit", s.MaxSpirit.ToString());
            Line("Skill damage", "+" + ItemText.Percent(s.SkillDamage));
            Line("Skill defense", s.SkillDefense.ToString());
            Line("Poison res.", ItemText.Percent(s.Resist.poison));
            Line("Paralysis res.", ItemText.Percent(s.Resist.paralysis));
            statsRight.text = sb.ToString().TrimEnd('\n');
        }

        void Line(string label, string value) =>
            sb.Append("<color=").Append(ItemText.Muted).Append('>').Append(label).Append("</color>  ").Append(value).Append('\n');

        void OnPrimary()
        {
            var item = Selected();
            if (item == null) return;
            switch (detail.Action)
            {
                case ItemAction.Equip: combat.Equip(item.Id); break;
                case ItemAction.Unequip: combat.Unequip(item.Def.Slot); break;
                case ItemAction.Use: combat.UseItem(item.DefId); break;
            }
        }

        void SellSelected(bool all)
        {
            var item = Selected();
            if (item == null || !shop.InSession) return;
            combat.Sell(item.Id, all ? item.Count : 1);
        }

        ItemInstance Selected()
        {
            if (string.IsNullOrEmpty(selectedId)) return null;
            var items = combat.Items;
            var item = items.Bag.Find(selectedId);
            if (item != null) return item;
            foreach (var worn in items.Equipment.Items)
                if (worn.Id == selectedId) return worn;
            return null;
        }
    }
}
