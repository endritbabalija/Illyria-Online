using UnityEngine;
using Wuxia.Player;

namespace Wuxia.UI
{
    /// <summary>The 4 skill buttons around Attack, plus the Drangue Leap button.</summary>
    public class SkillBar : MonoBehaviour
    {
        [SerializeField] PlayerSkills skills;
        [SerializeField] PlayerCombat combat;
        [SerializeField] DrangueLeap leap;
        [SerializeField] SkillButtonView[] buttons = new SkillButtonView[PlayerSkills.SlotCount];
        [SerializeField] SkillButtonView leapButton;

        // Whether each button last showed its form as unlocked (-1 = not yet), so labels only rebuild on change.
        readonly int[] shownUnlocked = new int[PlayerSkills.SlotCount];

        void Start()
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                var index = i;
                buttons[i].Button.onClick.AddListener(() => skills.PressSlot(index));
            }
            leapButton.Button.onClick.AddListener(leap.OnLeapButton);
            skills.SlotsChanged += Refresh;
            Refresh();
        }

        void OnDestroy() => skills.SlotsChanged -= Refresh;

        void Refresh()
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                var slot = skills.GetSlot(i);
                buttons[i].gameObject.SetActive(!slot.IsEmpty);
                shownUnlocked[i] = -1;
            }
            leapButton.SetLabels("Leap", $"{leap.Rules.spiritCost} Spirit");
            leapButton.gameObject.SetActive(skills.State.Knows(leap.Rules.bookId));
        }

        void Update()
        {
            var state = skills.State;
            var now = Time.time;
            var canAct = !combat.IsDead && !combat.IsDisabled;

            for (var i = 0; i < buttons.Length; i++)
            {
                var slot = skills.GetSlot(i);
                if (slot.IsEmpty) continue;
                var form = slot.Form;
                var unlocked = state.IsUnlocked(slot.Book, form);
                // Subtitle flips from the unlock level to the Spirit cost once points unlock the form.
                if ((unlocked ? 1 : 0) != shownUnlocked[i])
                {
                    shownUnlocked[i] = unlocked ? 1 : 0;
                    buttons[i].SetLabels(form.name, unlocked ? $"{form.spiritCost} Spirit" : $"Book Lv {form.unlockLevel}");
                }
                var left = state.CooldownLeft(form.id, now);
                var usable = canAct && unlocked && left <= 0f && state.Spirit >= form.spiritCost;
                buttons[i].SetState(form.cooldown > 0f ? left / form.cooldown : 0f, left, usable, skills.QueuedSlot == i);
            }

            var rules = leap.Rules;
            var leapLeft = state.CooldownLeft(rules.bookId, now);
            var leapUsable = canAct && !leap.IsMoving && leapLeft <= 0f && state.Spirit >= rules.spiritCost;
            leapButton.SetState(rules.cooldown > 0f ? leapLeft / rules.cooldown : 0f, leapLeft, leapUsable, false);
        }
    }
}
