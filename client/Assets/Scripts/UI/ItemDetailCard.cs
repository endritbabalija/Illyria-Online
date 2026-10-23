using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.Items;
using Wuxia.Player;
using Wuxia.World;

namespace Wuxia.UI
{
    public enum ItemAction { None, Equip, Unequip, Use }

    /// <summary>
    /// The tapped item in the bag panel: name in its rarity colour, kind and slot, requirements (red
    /// when this character can't use it), stats and bonus lines, what changes against the item worn
    /// in that slot, and its buttons: Move (bag items) and Equip / Unequip / Use on the bottom row;
    /// above them Sell / Sell all in a shop session, or the potion quick-slot buttons otherwise.
    /// </summary>
    public class ItemDetailCard : MonoBehaviour
    {
        const float Margin = 16f;
        const float ButtonGap = 8f;
        const float ButtonHeight = 72f;
        const float BodyTop = 92f;

        [SerializeField] RawImage icon;
        [SerializeField] Text nameText;
        [SerializeField] Text bodyText;
        [SerializeField] Text emptyHint;
        [SerializeField] Button primaryButton;
        [SerializeField] Text primaryLabel;
        [SerializeField] Button sellButton;
        [SerializeField] Text sellLabel;
        [SerializeField] Button sellAllButton;
        [SerializeField] Text sellAllLabel;
        [SerializeField] Button moveButton;
        [SerializeField] Text moveLabel;
        [Tooltip("One button per potion quick slot, in slot order.")]
        [SerializeField] Button[] quickSlotButtons = new Button[PlayerItems.QuickSlotCount];
        [SerializeField] Text[] quickSlotLabels = new Text[PlayerItems.QuickSlotCount];

        public Button PrimaryButton => primaryButton;
        public Button SellButton => sellButton;
        public Button SellAllButton => sellAllButton;
        public Button MoveButton => moveButton;
        public Button QuickSlotButton(int index) => quickSlotButtons[index];
        public int QuickSlotButtonCount => quickSlotButtons.Length;
        /// <summary>What the primary button does for the item shown.</summary>
        public ItemAction Action { get; private set; }

        readonly StringBuilder sb = new StringBuilder();
        readonly Button[] row = new Button[2];

        public void ShowNothing(bool selling)
        {
            Action = ItemAction.None;
            nameText.text = "";
            bodyText.text = "";
            emptyHint.gameObject.SetActive(true);
            emptyHint.text = selling ? "Tap an item in your bag to sell it." : "Tap an item to see it.";
            if (icon != null) icon.enabled = false;
            primaryButton.gameObject.SetActive(false);
            sellButton.gameObject.SetActive(false);
            sellAllButton.gameObject.SetActive(false);
            moveButton.gameObject.SetActive(false);
            foreach (var b in quickSlotButtons) b.gameObject.SetActive(false);
        }

        /// <param name="equipped">Shown from a gear slot rather than the bag.</param>
        /// <param name="selling">A shop session is open, so bag items can be sold.</param>
        /// <param name="moving">The item is being moved: only the Move button (now Cancel) shows.</param>
        public void Show(ItemInstance item, bool equipped, PlayerCombat combat, bool selling, ItemModelRegistry icons, bool moving)
        {
            var items = combat.Items;
            var def = item.Def;
            var character = combat.Character;
            emptyHint.gameObject.SetActive(false);

            nameText.text = item.Count > 1 ? $"{item.Name}  ×{item.Count}" : item.Name;
            nameText.color = ItemText.RarityColor(item.Rarity);
            ItemIcons.Apply(icon, icons, def);

            sb.Clear();
            sb.Append("<color=").Append(ItemText.Muted).Append('>');
            if (item.Rarity != null && def.IsGear) sb.Append(item.Rarity.name).Append(' ');
            sb.Append(ItemText.KindName(def));
            if (def.IsWeapon) sb.Append(" · Weapon");
            if (equipped) sb.Append(" · worn");
            sb.Append("</color>\n");

            if (def.IsGear)
            {
                var who = ItemText.CharacterNames(def);
                var charOk = ItemText.CharacterCanUse(def, character);
                var levelOk = def.levelRequired <= items.PlayerLevel;
                sb.Append("Requires ");
                if (who.Length > 0)
                    sb.Append("<color=").Append(charOk ? "#FFFFFF" : ItemText.Bad).Append('>').Append(who).Append("</color> · ");
                else if (!charOk)
                    sb.Append("<color=").Append(ItemText.Bad).Append(">another character</color> · ");
                sb.Append("<color=").Append(levelOk ? "#FFFFFF" : ItemText.Bad).Append(">Level ").Append(def.levelRequired).Append("</color>\n");
                if (!charOk) sb.Append("<color=").Append(ItemText.Bad).Append(">").Append(character.name).Append(" can't use this</color>\n");
                else if (!levelOk) sb.Append("<color=").Append(ItemText.Bad).Append(">Your level is too low</color>\n");

                sb.Append('\n');
                ItemText.AppendStats(sb, def.stats);
                if (item.BonusLines.Count > 0)
                {
                    var hex = ItemText.RarityHex(item.Rarity);
                    foreach (var line in item.BonusLines) sb.Append("<color=").Append(hex).Append('>').Append(line).Append("</color>\n");
                }

                if (!equipped && charOk)
                {
                    var worn = items.Equipment.Get(def.Slot);
                    sb.Append('\n').Append("<color=").Append(ItemText.Muted).Append('>')
                        .Append(worn != null ? "Compared with " + worn.Name : "Slot is empty").Append("</color>\n");
                    ItemText.AppendDelta(sb, item.TotalStats(), worn != null ? worn.TotalStats() : null);
                }
            }
            else if (def.IsUsable)
            {
                sb.Append('\n').Append(ItemText.Summary(def)).Append('\n');
                sb.Append("Cooldown ").Append(def.use.cooldown.ToString("0.#")).Append(" s\n");
            }

            var each = item.SellPriceEach;
            if (!equipped)
            {
                sb.Append('\n').Append("<color=").Append(ItemText.GoldHex).Append(">Sells for ").Append(each).Append(" gold");
                if (item.Count > 1) sb.Append(" each");
                sb.Append("</color>");
            }
            bodyText.text = sb.ToString();

            Action = equipped ? ItemAction.Unequip : def.IsGear ? ItemAction.Equip : def.IsUsable ? ItemAction.Use : ItemAction.None;
            primaryButton.gameObject.SetActive(Action != ItemAction.None && !moving);
            switch (Action)
            {
                case ItemAction.Equip:
                    primaryLabel.text = "Equip";
                    primaryButton.interactable = items.CheckWearable(item) == EquipBlock.None;
                    break;
                case ItemAction.Unequip:
                    primaryLabel.text = def.IsWeapon ? "Unequip\n<size=18>swap weapons instead</size>" : "Unequip";
                    primaryButton.interactable = !def.IsWeapon;
                    break;
                case ItemAction.Use:
                    primaryLabel.text = "Use";
                    primaryButton.interactable = true;
                    break;
            }

            // Worn gear doesn't move between bag slots (Unequip puts it in the bag).
            moveButton.gameObject.SetActive(!equipped);
            moveLabel.text = moving ? "Cancel" : "Move";

            var canSell = selling && !equipped && each > 0 && !moving;
            sellButton.gameObject.SetActive(canSell);
            sellAllButton.gameObject.SetActive(canSell && item.Count > 1);
            if (canSell)
            {
                sellLabel.text = $"Sell  <size=20>{each} gold</size>";
                sellAllLabel.text = $"Sell all  <size=20>{each * item.Count} gold</size>";
            }

            // Potions can go on a quick slot; the slot already holding this one says so.
            var canAssign = def.IsUsable && !equipped && !selling && !moving;
            for (var i = 0; i < quickSlotButtons.Length; i++)
            {
                quickSlotButtons[i].gameObject.SetActive(canAssign);
                if (!canAssign) continue;
                var onSlot = items.QuickSlot(i) == def.id;
                quickSlotButtons[i].interactable = !onSlot;
                quickSlotLabels[i].text = onSlot ? $"On slot {i + 1}" : $"Set slot {i + 1}";
            }

            // Bottom row: Move and the primary action; the row above: selling or the quick slots.
            var bottom = LayRow(0, moveButton, primaryButton);
            var upper = canSell
                ? LayRow(1, sellButton, sellAllButton)
                : LayRow(1, quickSlotButtons[0], quickSlotButtons.Length > 1 ? quickSlotButtons[1] : null);
            var rows = upper ? 2 : bottom ? 1 : 0;
            var body = bodyText.rectTransform;
            body.offsetMin = new Vector2(body.offsetMin.x, Margin + rows * (ButtonHeight + ButtonGap));
            body.offsetMax = new Vector2(body.offsetMax.x, -BodyTop);
        }

        /// <summary>Lays a row's visible buttons side by side across the card; false when none is visible.</summary>
        bool LayRow(int index, Button a, Button b)
        {
            var count = 0;
            if (a != null && a.gameObject.activeSelf) row[count++] = a;
            if (b != null && b.gameObject.activeSelf) row[count++] = b;
            if (count == 0) return false;
            var width = ((RectTransform)transform).rect.width;
            var each = (width - 2f * Margin - (count - 1) * ButtonGap) / count;
            var y = Margin + index * (ButtonHeight + ButtonGap);
            for (var i = 0; i < count; i++)
            {
                var rt = (RectTransform)row[i].transform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.sizeDelta = new Vector2(each, ButtonHeight);
                rt.anchoredPosition = new Vector2(Margin + i * (each + ButtonGap), y);
            }
            return true;
        }
    }
}
