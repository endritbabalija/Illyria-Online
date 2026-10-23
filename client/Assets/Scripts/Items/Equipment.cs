using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Items
{
    /// <summary>The 6 worn gear slots (weapon, helm, armor, boots, ring, amulet). Changed only through <see cref="PlayerItems"/>.</summary>
    public sealed class Equipment
    {
        public const int SlotCount = 6;

        readonly ItemInstance[] slots = new ItemInstance[SlotCount];

        /// <summary>What is worn in a slot, or null.</summary>
        public ItemInstance Get(EquipSlot slot) => slot == EquipSlot.None ? null : slots[(int)slot];

        public ItemInstance Weapon => slots[(int)EquipSlot.Weapon];

        /// <summary>Every worn item (no empty slots), in slot order.</summary>
        public IEnumerable<ItemInstance> Items
        {
            get
            {
                foreach (var s in slots)
                    if (s != null) yield return s;
            }
        }

        public bool IsEquipped(string itemId)
        {
            foreach (var s in slots)
                if (s != null && s.Id == itemId) return true;
            return false;
        }

        /// <summary>Puts an item (or null) into a slot and returns what was there.</summary>
        internal ItemInstance Set(EquipSlot slot, ItemInstance item)
        {
            var old = slots[(int)slot];
            slots[(int)slot] = item;
            return old;
        }

        internal void Clear() => System.Array.Clear(slots, 0, slots.Length);
    }
}
