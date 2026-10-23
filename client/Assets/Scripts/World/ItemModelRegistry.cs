using System;
using System.Collections.Generic;
using UnityEngine;
using Wuxia.Data;

namespace Wuxia.World
{
    /// <summary>
    /// Maps an item's <c>modelKey</c> (items.json) to the prefab shown in the hand and on the ground.
    /// Items without an entry keep the greybox weapon in hand (by weapon type) and drop as a pouch.
    /// Add a model: make a prefab whose root is the grip (blade/shaft along +Z, flat side facing ±Y,
    /// like the greybox sword under WeaponPivot), then add an entry here with its key.
    /// Also holds the UI item icons: one atlas texture rendered from these models (and greybox
    /// stand-ins for items without one) by Wuxia → Items → Generate Item Icons, keyed like models.
    /// </summary>
    [CreateAssetMenu(menuName = "Wuxia/Item Model Registry", fileName = "ItemModels")]
    public class ItemModelRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("modelKey from items.json, e.g. IronSword.")]
            public string modelKey;
            [Tooltip("Root at the grip; parented to WeaponPivot as is in hand.")]
            public GameObject prefab;
            [Tooltip("Model position on the ground, relative to the drop spot.")]
            public Vector3 groundOffset;
            [Tooltip("Model rotation on the ground (a sword at (90, 0, 0) stands blade-down, stuck in the ground).")]
            public Vector3 groundEuler;
            [Tooltip("Degrees per second the model turns on the ground (0 = still).")]
            public float groundSpin;
            [Tooltip("Height of a slow up-and-down bob on the ground, in metres (0 = still).")]
            public float groundBob;
        }

        /// <summary>One icon in <see cref="IconAtlas"/>: its key (modelKey, else item id; "Gold", "Pouch") and UV rect.</summary>
        [Serializable]
        public class IconEntry
        {
            public string key;
            public Rect uv;
        }

        [SerializeField] Entry[] items = new Entry[0];
        [Tooltip("Ground model for a gold drop.")]
        [SerializeField] Entry gold = new Entry();
        [Tooltip("Ground model for an item with no entry of its own.")]
        [SerializeField] Entry pouch = new Entry();

        [Header("Icons (generated)")]
        [Tooltip("Written by Wuxia → Items → Generate Item Icons; don't edit by hand.")]
        [SerializeField] Texture2D iconAtlas;
        [SerializeField] IconEntry[] icons = new IconEntry[0];

        Dictionary<string, Rect> iconLookup;

        public Entry Gold => gold;
        public Entry Pouch => pouch;
        public Texture2D IconAtlas => iconAtlas;
        public IReadOnlyList<Entry> Items => items;

        /// <summary>The key an item's icon (and model) is stored under: its modelKey, else its id.</summary>
        public static string IconKey(ItemDef def) => !string.IsNullOrEmpty(def.modelKey) ? def.modelKey : def.id;

        /// <summary>The atlas UV rect of an icon by key; false when there is no such icon.</summary>
        public bool TryGetIcon(string key, out Rect uv)
        {
            uv = default;
            if (iconAtlas == null || string.IsNullOrEmpty(key)) return false;
            if (iconLookup == null)
            {
                iconLookup = new Dictionary<string, Rect>();
                foreach (var icon in icons)
                    if (icon != null && !string.IsNullOrEmpty(icon.key)) iconLookup[icon.key] = icon.uv;
            }
            return iconLookup.TryGetValue(key, out uv);
        }

        /// <summary>An item's icon, falling back to the pouch icon; false only when no icons were generated.</summary>
        public bool TryGetIcon(ItemDef def, out Rect uv)
        {
            if (def != null && TryGetIcon(IconKey(def), out uv)) return true;
            return TryGetIcon(pouch.modelKey, out uv);
        }

#if UNITY_EDITOR
        /// <summary>Editor only: replaces the generated icon atlas and its entries.</summary>
        public void SetIcons(Texture2D atlas, IconEntry[] entries)
        {
            iconAtlas = atlas;
            icons = entries ?? new IconEntry[0];
            iconLookup = null;
        }
#endif

        void OnValidate() => iconLookup = null;

        /// <summary>The entry for a model key, or null when the item has no model of its own.</summary>
        public Entry Find(string modelKey)
        {
            if (string.IsNullOrEmpty(modelKey)) return null;
            foreach (var e in items)
                if (e != null && e.prefab != null && e.modelKey == modelKey) return e;
            return null;
        }

        /// <summary>The prefab for a model key, or null.</summary>
        public GameObject FindPrefab(string modelKey)
        {
            var e = Find(modelKey);
            return e != null ? e.prefab : null;
        }
    }
}
