using UnityEngine;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>One item in an NPC shop: icon on a tile, name, a stat summary, price and Buy buttons (×1, and ×5 for potions).</summary>
    public class ShopRow : MonoBehaviour
    {
        public const int BulkCount = 5;

        [SerializeField] Image tile;
        [SerializeField] RawImage icon;
        [SerializeField] Text nameText;
        [SerializeField] Text detailText;
        [SerializeField] Text priceText;
        [SerializeField] Button buyButton;
        [SerializeField] Button buyBulkButton;
        [SerializeField] Text buyBulkLabel;

        public Button BuyButton => buyButton;
        public Button BuyBulkButton => buyBulkButton;
        public ItemDef Def { get; private set; }

        public void Bind(ItemDef def, CharacterData character, ItemModelRegistry icons)
        {
            Def = def;
            var shade = ItemText.TileColor(def) * 0.7f;
            shade.a = 1f;
            tile.color = shade;
            ItemIcons.Apply(icon, icons, def);
            nameText.text = def.name;
            var summary = ItemText.Summary(def);
            if (def.IsGear && !ItemText.CharacterCanUse(def, character))
                summary = $"<color={ItemText.Bad}>{ItemText.CharacterNames(def)} only</color>   {summary}";
            detailText.text = summary;
            buyBulkButton.gameObject.SetActive(def.IsStackable);
            buyBulkLabel.text = $"×{BulkCount}";
        }

        /// <summary>Price in gold, red when you can't afford one.</summary>
        public void Refresh(int gold)
        {
            priceText.text = $"<color={(gold >= Def.buyPrice ? ItemText.GoldHex : ItemText.Bad)}>{Def.buyPrice} gold</color>";
        }
    }
}
