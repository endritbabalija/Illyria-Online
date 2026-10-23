using UnityEngine;
using UnityEngine.UI;
using Wuxia.Items;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>
    /// One bag or gear slot: the item's icon on a tile in its kind colour, its short name, the stack
    /// count, and a border in the rarity colour. Gear slots also show their slot caption. A gold
    /// mark shows the selected item; a cyan mark (and a faded icon) the item being moved.
    /// </summary>
    public class ItemSlotView : MonoBehaviour
    {
        static readonly Color EmptyBorder = new Color(1f, 1f, 1f, 0.12f);
        static readonly Color EmptyTile = new Color(0f, 0f, 0f, 0.35f);
        static readonly Color SelectedColor = new Color(1f, 0.82f, 0.3f, 1f);
        static readonly Color MovingColor = new Color(0.3f, 0.9f, 1f, 1f);
        const float TileShade = 0.7f;
        const float MovingIconAlpha = 0.45f;

        [SerializeField] Button button;
        [SerializeField] Image border;
        [SerializeField] Image tile;
        [Tooltip("Item icon from the icon atlas in ItemModels.asset; hidden when the item has none.")]
        [SerializeField] RawImage icon;
        [SerializeField] Text nameText;
        [SerializeField] Text countText;
        [Tooltip("Optional caption (Weapon, Helm…) for gear slots.")]
        [SerializeField] Text captionText;
        [SerializeField] Image selectedMark;

        public Button Button => button;
        public ItemInstance Item { get; private set; }

        bool shownSelected, shownMoving;

        public void SetCaption(string caption)
        {
            if (captionText != null) captionText.text = caption;
        }

        public void Show(ItemInstance item, ItemModelRegistry icons)
        {
            Item = item;
            if (item == null)
            {
                border.color = EmptyBorder;
                tile.color = EmptyTile;
                nameText.text = "";
                countText.text = "";
                if (icon != null) icon.enabled = false;
                return;
            }
            border.color = ItemText.RarityColor(item.Rarity);
            var shade = ItemText.TileColor(item.Def) * TileShade;
            shade.a = 1f;
            tile.color = shade;
            // The border carries the rarity; white text stays readable on every tile colour.
            nameText.text = item.Name;
            countText.text = item.Count > 1 ? item.Count.ToString() : "";
            ItemIcons.Apply(icon, icons, item.Def);
        }

        /// <param name="moving">This slot's item is being moved: cyan mark and a faded icon.</param>
        public void SetSelected(bool selected, bool moving = false)
        {
            if (selected == shownSelected && moving == shownMoving) return;
            shownSelected = selected;
            shownMoving = moving;
            if (selectedMark != null)
            {
                selectedMark.gameObject.SetActive(selected || moving);
                selectedMark.color = moving ? MovingColor : SelectedColor;
            }
            if (icon != null) icon.color = new Color(1f, 1f, 1f, moving ? MovingIconAlpha : 1f);
        }
    }
}
