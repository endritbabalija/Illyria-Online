using UnityEngine;
using UnityEngine.UI;
using Wuxia.Combat;
using Wuxia.Player;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>Zone HUD: player HP, locked-target frame, kill count, death overlay, Attack button.</summary>
    public class Hud : MonoBehaviour
    {
        [SerializeField] PlayerCombat player;
        [SerializeField] Button attackButton;

        [Header("Player")]
        [SerializeField] Text playerName;
        [SerializeField] RectTransform playerHpFill;
        [SerializeField] Text playerHpText;

        [Header("Target")]
        [SerializeField] GameObject targetFrame;
        [SerializeField] Text targetName;
        [SerializeField] RectTransform targetHpFill;
        [SerializeField] Text targetHpText;

        [Header("Zone")]
        [SerializeField] Text zoneText;
        [SerializeField] Text killsText;

        [Header("Death")]
        [SerializeField] GameObject deathOverlay;
        [SerializeField] Text deathText;

        // Text is only rebuilt when a value changes, to keep UI work (and garbage) per frame near zero.
        int shownHp = -1, shownMaxHp = -1, shownTargetHp = -1, shownKills = -1, shownCountdown = -1;
        Combatant shownTarget;
        bool targetShown = true;

        void Start()
        {
            attackButton.onClick.AddListener(player.OnAttackButton);
            zoneText.text = ZoneSession.Instance.ZoneName;
            deathOverlay.SetActive(false);
        }

        void Update()
        {
            var me = player.Combatant;
            if (me.Hp != shownHp || me.MaxHp != shownMaxHp)
            {
                shownHp = me.Hp;
                shownMaxHp = me.MaxHp;
                playerName.text = me.DisplayName;
                playerHpText.text = $"{me.Hp} / {me.MaxHp}";
                SetFill(playerHpFill, me.HpFraction);
            }

            var target = player.Target;
            if (target != shownTarget || targetShown != (target != null))
            {
                shownTarget = target;
                targetShown = target != null;
                targetFrame.SetActive(targetShown);
                shownTargetHp = -1;
                if (targetShown) targetName.text = $"{target.DisplayName}  Lv {target.Level}";
            }
            if (target != null && target.Hp != shownTargetHp)
            {
                shownTargetHp = target.Hp;
                targetHpText.text = $"{target.Hp} / {target.MaxHp}";
                SetFill(targetHpFill, target.HpFraction);
            }

            var kills = ZoneSession.Instance.Kills;
            if (kills != shownKills)
            {
                shownKills = kills;
                killsText.text = $"Wolves slain: {kills}";
            }

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

        static void SetFill(RectTransform fill, float fraction) =>
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), fill.anchorMax.y);
    }
}
