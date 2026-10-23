using System;
using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Items
{
    /// <summary>
    /// The bag: a fixed number of slots, each empty or holding one item (or one potion stack).
    /// Adding is all-or-nothing, so a drop that doesn't fit stays on the ground whole. Pure C#.
    /// Change items through <see cref="PlayerItems"/> so every change reaches the ledger; only
    /// <see cref="Move"/> is safe to call directly (it changes no ownership).
    /// </summary>
    public sealed class Inventory
    {
        readonly ItemInstance[] slots;

        public int Capacity => slots.Length;
        /// <summary>Bumped on every change, so UI knows to refresh.</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public Inventory(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            slots = new ItemInstance[capacity];
        }

        /// <summary>The item in a bag slot, or null when the slot is empty.</summary>
        public ItemInstance Get(int index) => slots[index];

        public int FreeSlots
        {
            get
            {
                var free = 0;
                foreach (var s in slots)
                    if (s == null) free++;
                return free;
            }
        }

        public bool IsFull => FreeSlots == 0;

        /// <summary>Every item in bag order (no empty slots).</summary>
        public IEnumerable<ItemInstance> Items
        {
            get
            {
                foreach (var s in slots)
                    if (s != null) yield return s;
            }
        }

        public int IndexOf(string itemId)
        {
            for (var i = 0; i < slots.Length; i++)
                if (slots[i] != null && slots[i].Id == itemId) return i;
            return -1;
        }

        public ItemInstance Find(string itemId)
        {
            var i = IndexOf(itemId);
            return i >= 0 ? slots[i] : null;
        }

        /// <summary>How many of an item definition the bag holds, summed over stacks.</summary>
        public int CountOf(string defId)
        {
            var count = 0;
            foreach (var s in slots)
                if (s != null && s.DefId == defId) count += s.Count;
            return count;
        }

        /// <summary>The first stack of a definition, or null.</summary>
        public ItemInstance FindFirst(string defId)
        {
            foreach (var s in slots)
                if (s != null && s.DefId == defId) return s;
            return null;
        }

        /// <summary>Whether <paramref name="count"/> more of <paramref name="def"/> would fit (topping up stacks first).</summary>
        public bool CanAdd(ItemDef def, int count)
        {
            if (count <= 0) return false;
            var stackSize = Math.Max(1, def.stackSize);
            var room = 0L;
            foreach (var s in slots)
            {
                if (s == null) room += stackSize;
                else if (stackSize > 1 && CanStack(s, def)) room += stackSize - s.Count;
            }
            return room >= count;
        }

        /// <summary>
        /// Adds one item: a potion stack tops up existing stacks of the same item first, then takes
        /// an empty slot. All or nothing: false (and no change) when it doesn't fit. After a true
        /// return the instance may have been merged away (its <see cref="ItemInstance.Count"/> is 0).
        /// </summary>
        public bool TryAdd(ItemInstance item)
        {
            if (item == null || item.Count <= 0 || !CanAdd(item.Def, item.Count)) return false;
            AddUnchecked(item);
            Bump();
            return true;
        }

        /// <summary>Adds several items, all or none.</summary>
        public bool TryAddAll(IList<ItemInstance> items)
        {
            var savedSlots = (ItemInstance[])slots.Clone();
            var savedCounts = new Dictionary<ItemInstance, int>();
            foreach (var s in slots)
                if (s != null) savedCounts[s] = s.Count;
            foreach (var item in items)
                if (item != null) savedCounts[item] = item.Count;

            foreach (var item in items)
            {
                if (item != null && item.Count > 0 && CanAdd(item.Def, item.Count))
                {
                    AddUnchecked(item);
                    continue;
                }
                Array.Copy(savedSlots, slots, slots.Length);
                foreach (var pair in savedCounts) pair.Key.Count = pair.Value;
                return false;
            }
            Bump();
            return true;
        }

        /// <summary>Takes <paramref name="count"/> from an item's stack (or the whole gear item); null if it isn't there or the stack is too small.</summary>
        internal ItemInstance Take(string itemId, int count)
        {
            var index = IndexOf(itemId);
            if (index < 0 || count <= 0 || slots[index].Count < count) return null;
            var item = slots[index];
            item.Count -= count;
            if (item.Count == 0) slots[index] = null;
            Bump();
            return item;
        }

        /// <summary>Puts <paramref name="item"/> (may be null) into a slot and returns what was there.</summary>
        internal ItemInstance Replace(int index, ItemInstance item)
        {
            var old = slots[index];
            slots[index] = item;
            Bump();
            return old;
        }

        internal void Clear()
        {
            Array.Clear(slots, 0, slots.Length);
            Bump();
        }

        /// <summary>
        /// Moves the item in <paramref name="from"/> to <paramref name="to"/>: into an empty slot,
        /// onto a stack of the same potion (as much as fits), or swapping places with what is there.
        /// </summary>
        public bool Move(int from, int to)
        {
            if (from < 0 || from >= slots.Length || to < 0 || to >= slots.Length || from == to) return false;
            var a = slots[from];
            if (a == null) return false;
            var b = slots[to];
            if (b != null && b.Def.stackSize > 1 && CanStack(b, a.Def) && b.Count < b.Def.stackSize)
            {
                var moved = Math.Min(a.Count, b.Def.stackSize - b.Count);
                b.Count += moved;
                a.Count -= moved;
                if (a.Count == 0) slots[from] = null;
            }
            else
            {
                slots[to] = a;
                slots[from] = b;
            }
            Bump();
            return true;
        }

        void AddUnchecked(ItemInstance item)
        {
            var stackSize = Math.Max(1, item.Def.stackSize);
            if (stackSize > 1)
            {
                for (var i = 0; i < slots.Length && item.Count > 0; i++)
                {
                    var s = slots[i];
                    if (s == null || s == item || !CanStack(s, item.Def) || s.Count >= stackSize) continue;
                    var moved = Math.Min(item.Count, stackSize - s.Count);
                    s.Count += moved;
                    item.Count -= moved;
                }
            }
            if (item.Count <= 0) return;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) continue;
                slots[i] = item;
                return;
            }
            throw new InvalidOperationException("Inventory.AddUnchecked ran out of room after CanAdd passed");
        }

        // Stackables are always Common with no lines, so the same definition is enough.
        static bool CanStack(ItemInstance stack, ItemDef def) => stack.Def.id == def.id;

        void Bump()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
