using UnityEngine;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>Shows an item's icon from the atlas in <see cref="ItemModelRegistry"/> on a RawImage.</summary>
    public static class ItemIcons
    {
        /// <summary>Points <paramref name="image"/> at the item's icon (the pouch icon if it has none); hides it when no icons exist.</summary>
        public static bool Apply(RawImage image, ItemModelRegistry icons, ItemDef def)
        {
            if (image == null) return false;
            if (def == null || icons == null || !icons.TryGetIcon(def, out var uv))
            {
                image.enabled = false;
                return false;
            }
            if (image.texture != icons.IconAtlas) image.texture = icons.IconAtlas;
            image.uvRect = uv;
            image.enabled = true;
            return true;
        }
    }
}
