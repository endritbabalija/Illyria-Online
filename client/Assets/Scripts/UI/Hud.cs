using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Combat;
using Wuxia.Player;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>
    /// Zone HUD: player HP and Spirit, gold, locked-target frame with its statuses, kill count, death
    /// overlay, Attack, weapon swap, Skills and Bag buttons, and short notices ("Not enough Spirit").
    /// While the Bag, Shop or Skills panel is open, a backdrop dims and blocks every HUD control
    /// except the player and gold frames (which <see cref="PanelFit"/> keeps the panels clear of);
    /// tapping it cancels a bag move, or else closes the panel.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        [SerializeField] PlayerCombat player;
        [SerializeField] PlayerSkills skills;
        [SerializeField] Button attackButton;

        [Header("Player")]
        [SerializeField] Text playerName;
        [SerializeField] RectTransform playerHpFill;
        [SerializeField] Text playerHpText;
        [SerializeField] RectTransform playerSpiritFill;
        [SerializeField] Text playerSpiritText;

        [Header("Target")]
        [SerializeField] GameObject targetFrame;
        [SerializeField] Text targetName;
        [SerializeField] RectTransform targetHpFill;
        [SerializeField] Text targetHpText;
        [SerializeField] Text targetStatus;

        [Header("Zone")]
        [SerializeField] Text zoneText;
        [SerializeField] Text killsText;

        [Header("Weapon and skills")]
        [SerializeField] Button swapButton;
        [SerializeField] Text swapLabel;
        [SerializeField] Button skillsButton;
        [SerializeField] Text skillsLabel;
        [SerializeField] SkillsPanel skillsPanel;

        [Header("Items")]
        [SerializeField] Text goldText;
        [SerializeField] Button bagButton;
        [SerializeField] InventoryPanel inventoryPanel;
        [SerializeField] ShopPanel shopPanel;
        [Tooltip("Full-screen dimmer behind the panels; a Button, drawn above the HUD controls but below the player and gold frames.")]
        [SerializeField] Button panelBackdrop;

        [Header("Notices")]
        [SerializeField] Text noticeText;
        [SerializeField] float noticeDuration = 1.6f;
        [Tooltip("Where notices sit while a panel is open: this far below the top of the safe area, clear of the panels.")]
        [SerializeField] float noticeTopWhilePanelOpen = 46f;

        [Header("Death")]
        [SerializeField] GameObject deathOverlay;
        [SerializeField] Text deathText;

        // Text is only rebuilt when a value changes, to keep UI work (and garbage) per frame near zero.
        int shownHp = -1, shownMaxHp = -1, shownTargetHp = -1, shownKills = -1, shownCountdown = -1;
        int shownSpirit = -1, shownPoints = -1, shownStatusVersion = -1, shownGold = -1, shownItemsVersion = -1;
        string shownName;
        Combatant shownTarget;
        bool targetShown = true;
        float noticeUntil;
        Vector2 noticeAnchor, noticePosition;
        readonly StringBuilder statusBuilder = new StringBuilder();

        void Start()
        {
            attackButton.onClick.AddListener(player.OnAttackButton);
            swapButton.onClick.AddListener(player.SwapWeapon);
            skillsButton.onClick.AddListener(ToggleSkills);
            bagButton.onClick.AddListener(inventoryPanel.Toggle);
            panelBackdrop.onClick.AddListener(OnBackdrop);
            zoneText.text = ZoneSession.Instance.ZoneName;
            deathOverlay.SetActive(false);
            skillsPanel.Close();
            shopPanel.EndSession();
            inventoryPanel.Close();
            panelBackdrop.gameObject.SetActive(false);
            noticeAnchor = noticeText.rectTransform.anchorMin;
            noticePosition = noticeText.rectTransform.anchoredPosition;
            noticeText.text = "";
            OnLoadoutChanged();
        }

        void OnEnable()
        {
            player.LoadoutChanged += OnLoadoutChanged;
            skills.Notice += ShowNotice;
        }

        void OnDisable()
        {
            player.LoadoutChanged -= OnLoadoutChanged;
            skills.Notice -= ShowNotice;
        }

        void ToggleSkills()
        {
            // One panel at a time: the skills panel ends any shopping session.
            if (!skillsPanel.gameObject.activeSelf)
            {
                shopPanel.EndSession();
                inventoryPanel.Close();
            }
            skillsPanel.Toggle();
        }

        // Tapping outside a panel: first cancels a bag move, then closes what's open (the bag in a
        // shop session returns to the shop's catalog, as its Close button does).
        void OnBackdrop()
        {
            if (inventoryPanel.IsMoving)
            {
                inventoryPanel.CancelMove();
                return;
            }
            if (skillsPanel.gameObject.activeSelf) skillsPanel.Close();
            else if (inventoryPanel.IsOpen) inventoryPanel.Close();
            else shopPanel.EndSession();
        }

        // The Swap label names the weapon Swap would pick, which depends on the bag as well as the loadout.
        void OnLoadoutChanged()
        {
            var next = player.Items.FindSwapWeapon();
            swapLabel.text = next != null
                ? $"{player.Weapon.name}\n<size=22>swap to {next.Name}</size>"
                : $"{player.Weapon.name}\n<size=22>no other weapon</size>";
        }

        void ShowNotice(string message)
        {
            noticeText.text = message;
            noticeUntil = Time.time + noticeDuration;
        }

        void Update()
        {
            var anyPanel = skillsPanel.gameObject.activeSelf || inventoryPanel.IsOpen || shopPanel.gameObject.activeSelf;
            if (anyPanel != panelBackdrop.gameObject.activeSelf)
            {
                panelBackdrop.gameObject.SetActive(anyPanel);
                // Notices move to the top edge while a panel is open, so they don't cover its contents.
                var notice = noticeText.rectTransform;
                notice.anchorMin = notice.anchorMax = anyPanel ? new Vector2(0.5f, 1f) : noticeAnchor;
                notice.anchoredPosition = anyPanel ? new Vector2(0f, -noticeTopWhilePanelOpen) : noticePosition;
            }

            var me = player.Combatant;
            if (me.Hp != shownHp || me.MaxHp != shownMaxHp || me.DisplayName != shownName)
            {
                shownHp = me.Hp;
                shownMaxHp = me.MaxHp;
                shownName = me.DisplayName;
                playerName.text = me.DisplayName;
                playerHpText.text = $"{me.Hp} / {me.MaxHp}";
                SetFill(playerHpFill, me.HpFraction);
            }

            var state = skills.State;
            var spirit = Mathf.FloorToInt(state.Spirit);
            if (spirit != shownSpirit)
            {
                shownSpirit = spirit;
                playerSpiritText.text = $"{spirit} / {state.MaxSpirit}";
                SetFill(playerSpiritFill, state.MaxSpirit > 0 ? state.Spirit / state.MaxSpirit : 0f);
            }

            if (state.SkillPoints != shownPoints)
            {
                shownPoints = state.SkillPoints;
                skillsLabel.text = state.SkillPoints > 0 ? $"Skills\n<size=22>{state.SkillPoints} points</size>" : "Skills";
            }

            UpdateTarget();

            var items = player.Items;
            if (items.Version != shownItemsVersion)
            {
                shownItemsVersion = items.Version;
                OnLoadoutChanged();
                if (items.Gold != shownGold)
                {
                    shownGold = items.Gold;
                    goldText.text = items.Gold.ToString();
                }
            }

            var kills = ZoneSession.Instance.Kills;
            if (kills != shownKills)
            {
                shownKills = kills;
                killsText.text = $"Wolves slain: {kills}";
            }

            if (noticeText.text.Length > 0 && Time.time >= noticeUntil) noticeText.text = "";

            if (player.IsDead != deathOverlay.activeSelf) deathOverlay.SetActive(player.IsDead);
            if (player.IsDead)
            {
                var countdown = Mathf.Max(0, Mathf.CeilToInt(player.RespawnAt - Time.time));
                if (countdown != shownCountdown)
                {
                    shownCountdown = countdown;
                    deathText.text = $"You have fallen\n<size=36>Reviving at the Han in {countdown}…</size>";
                }
            }
        }

        void UpdateTarget()
        {
            var target = player.Target;
            if (target != shownTarget || targetShown != (target != null))
            {
                shownTarget = target;
                targetShown = target != null;
                targetFrame.SetActive(targetShown);
                shownTargetHp = -1;
                shownStatusVersion = -1;
                if (targetShown) targetName.text = $"{target.DisplayName}  Lv {target.Level}";
            }
            if (target == null) return;

            if (target.Hp != shownTargetHp)
            {
                shownTargetHp = target.Hp;
                targetHpText.text = $"{target.Hp} / {target.MaxHp}";
                SetFill(targetHpFill, target.HpFraction);
            }

            var statuses = target.Statuses;
            if (statuses.Version != shownStatusVersion)
            {
                shownStatusVersion = statuses.Version;
                statusBuilder.Clear();
                for (var i = 0; i < statuses.Count; i++)
                {
                    if (i > 0) statusBuilder.Append("  ·  ");
                    statusBuilder.Append(statuses.Get(i).name);
                }
                targetStatus.text = statusBuilder.ToString();
            }
        }

        static void SetFill(RectTransform fill, float fraction) =>
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), fill.anchorMax.y);
    }
}
