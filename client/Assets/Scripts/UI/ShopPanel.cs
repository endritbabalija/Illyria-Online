using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.Items;
using Wuxia.Player;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>
    /// An NPC shop's catalog from items.json, with prices and Buy buttons, and a Sell button that
    /// opens the bag in sell mode. The shopping session lasts until you close the shop, walk away
    /// from the <see cref="Shopkeeper"/> or die; the catalog is hidden (not ended) while the bag is open.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] PlayerCombat combat;
        [Tooltip("Item icons (Assets/Items/ItemModels.asset).")]
        [SerializeField] ItemModelRegistry icons;
        [SerializeField] InventoryPanel inventory;
        [SerializeField] SkillsPanel skillsPanel;
        [SerializeField] Text titleText;
        [SerializeField] Text goldText;
        [Tooltip("Disabled row used as the template for every item on sale.")]
        [SerializeField] ShopRow rowTemplate;
        [SerializeField] Button closeButton;
        [SerializeField] Button sellButton;

        readonly List<ShopRow> rows = new List<ShopRow>();
        ShopData builtShop;
        string builtCharacter;
        int shownVersion = -1;

        /// <summary>The NPC whose shop is open, or null when no session is open.</summary>
        public Shopkeeper Keeper { get; private set; }
        public ShopData Shop => Keeper != null ? Keeper.Shop : null;
        public bool InSession => Keeper != null;

        void Awake()
        {
            rowTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(EndSession);
            sellButton.onClick.AddListener(() => inventory.Open());
        }

        /// <summary>Starts a session with <paramref name="keeper"/> and shows the catalog.</summary>
        public void Open(Shopkeeper keeper)
        {
            if (combat.IsDead) return;
            var fresh = Keeper != keeper;
            Keeper = keeper;
            if (skillsPanel != null) skillsPanel.Close();
            if (inventory.IsOpen) inventory.Close();
            ShowCatalog();
            if (fresh) Debug.Log($"[Shop] Trading with {keeper.Shop.name}");
        }

        public void ShowCatalog()
        {
            if (!InSession) return;
            gameObject.SetActive(true);
            shownVersion = -1;
        }

        /// <summary>Hides the catalog but keeps the session (the bag is open for selling).</summary>
        public void HideCatalog() => gameObject.SetActive(false);

        /// <summary>Ends the session: the catalog closes and the bag (if open) leaves sell mode.</summary>
        public void EndSession()
        {
            if (Keeper == null && !gameObject.activeSelf) return;
            Keeper = null;
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!InSession)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Shop != builtShop || combat.Character.id != builtCharacter) Rebuild();
            var items = combat.Items;
            if (items.Version == shownVersion) return;
            shownVersion = items.Version;
            goldText.text = $"<color={ItemText.GoldHex}>{items.Gold}</color> gold";
            foreach (var row in rows) row.Refresh(items.Gold);
        }

        void Rebuild()
        {
            builtShop = Shop;
            builtCharacter = combat.Character.id;
            shownVersion = -1;
            titleText.text = builtShop.name;
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();
            foreach (var id in builtShop.items)
            {
                var def = GameData.Items.FindItem(id);
                if (def == null || def.buyPrice <= 0) continue;
                var row = Instantiate(rowTemplate, rowTemplate.transform.parent);
                row.name = def.name;
                row.gameObject.SetActive(true);
                row.Bind(def, combat.Character, icons);
                var defId = def.id;
                row.BuyButton.onClick.AddListener(() => Buy(defId, 1));
                row.BuyBulkButton.onClick.AddListener(() => Buy(defId, ShopRow.BulkCount));
                rows.Add(row);
            }
        }

        void Buy(string defId, int count)
        {
            if (!InSession) return;
            if (combat.Buy(builtShop.id, defId, count) == ShopBlock.None)
                combat.Tell($"Bought {GameData.Items.GetItem(defId).name}{(count > 1 ? " ×" + count : "")}");
        }
    }
}
