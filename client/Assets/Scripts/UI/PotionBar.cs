using UnityEngine;
using Wuxia.Data;
using Wuxia.Items;
using Wuxia.Player;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>
    /// The 2 potion quick-slot buttons: the potion's icon and name, how many are left, and a cooldown
    /// sweep, in the potion's colour; dimmed when none are left or it's cooling down. Which potion
    /// sits on which slot is chosen from its card in the bag (items.json quickSlots is the start).
    /// Labels only change when the bag or the slots do.
    /// </summary>
    public class PotionBar : MonoBehaviour
    {
        [SerializeField] PlayerCombat combat;
        [Tooltip("Item icons (Assets/Items/ItemModels.asset).")]
        [SerializeField] ItemModelRegistry icons;
        [SerializeField] SkillButtonView[] buttons = new SkillButtonView[PlayerItems.QuickSlotCount];

        const float PotionAlpha = 0.92f;

        readonly ItemDef[] shownDefs = new ItemDef[PlayerItems.QuickSlotCount];
        readonly int[] shownCounts = new int[PlayerItems.QuickSlotCount];
        int shownVersion = -1;

        void Start()
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                var index = i;
                buttons[i].Button.onClick.AddListener(() => combat.UseQuickSlot(index));
            }
        }

        void Update()
        {
            var items = combat.Items;
            if (items.Version != shownVersion)
            {
                shownVersion = items.Version;
                for (var i = 0; i < buttons.Length; i++)
                {
                    var id = items.QuickSlot(i);
                    var def = string.IsNullOrEmpty(id) ? null : items.Data.FindItem(id);
                    var count = items.QuickSlotItemCount(i);
                    if (def == shownDefs[i] && count == shownCounts[i]) continue;
                    if (def != shownDefs[i])
                    {
                        var color = def != null ? ItemText.TileColor(def) : new Color(0.2f, 0.2f, 0.2f);
                        color.a = PotionAlpha;
                        buttons[i].SetColor(color);
                        ItemIcons.Apply(buttons[i].Icon, icons, def);
                    }
                    shownDefs[i] = def;
                    shownCounts[i] = count;
                    buttons[i].SetLabels(def != null ? def.name : "Empty", def != null ? $"×{count}" : "");
                }
            }

            var now = Time.time;
            var canAct = !combat.IsDead;
            for (var i = 0; i < buttons.Length; i++)
            {
                var def = shownDefs[i];
                var left = def != null ? items.CooldownLeft(def.id, now) : 0f;
                var total = def != null && def.use != null ? def.use.cooldown : 0f;
                buttons[i].SetState(total > 0f ? left / total : 0f, left, canAct && def != null && shownCounts[i] > 0 && left <= 0f, false);
            }
        }
    }
}
