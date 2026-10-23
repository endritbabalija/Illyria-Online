using UnityEngine;
using UnityEngine.UI;

namespace Wuxia.UI
{
    /// <summary>
    /// One round skill button: name, a subtitle (Spirit cost, or the book level that unlocks it), a
    /// radial cooldown sweep with seconds left, dimmed when it can't be used and ringed when queued.
    /// Potion quick slots use the same button with an item icon and their potion's colour.
    /// </summary>
    public class SkillButtonView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Text label;
        [SerializeField] Text subtitle;
        [Tooltip("Optional item icon (potion quick slots).")]
        [SerializeField] RawImage icon;
        [Tooltip("Radial-filled image drawn over the button while on cooldown.")]
        [SerializeField] Image cooldownFill;
        [SerializeField] Text cooldownText;
        [Tooltip("Shown while this form is queued to fire next.")]
        [SerializeField] GameObject queuedRing;
        [SerializeField] float dimAlpha = 0.4f;

        public Button Button => button;
        public RawImage Icon => icon;

        Color baseColor;
        int shownSeconds = -1;
        bool shownUsable = true;
        bool shownQueued;
        float shownFill = -1f;

        void Awake()
        {
            baseColor = background.color;
            queuedRing.SetActive(false);
            cooldownText.text = "";
            cooldownFill.fillAmount = 0f;
        }

        public void SetLabels(string name, string sub)
        {
            label.text = name;
            subtitle.text = sub;
        }

        /// <summary>Changes the button's base colour (kept dimmed while it can't be used).</summary>
        public void SetColor(Color color)
        {
            baseColor = color;
            var c = color;
            if (!shownUsable) c.a *= dimAlpha;
            background.color = c;
        }

        /// <param name="cooldownFraction">Share of the cooldown still to run, 0–1.</param>
        public void SetState(float cooldownFraction, float secondsLeft, bool usable, bool queued)
        {
            if (Mathf.Abs(cooldownFraction - shownFill) > 0.005f || (cooldownFraction == 0f && shownFill != 0f))
            {
                shownFill = cooldownFraction;
                cooldownFill.fillAmount = cooldownFraction;
            }

            var seconds = secondsLeft > 0f ? Mathf.CeilToInt(secondsLeft) : 0;
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                cooldownText.text = seconds > 0 ? seconds.ToString() : "";
            }

            if (usable != shownUsable)
            {
                shownUsable = usable;
                var c = baseColor;
                if (!usable) c.a *= dimAlpha;
                background.color = c;
                label.color = usable ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                if (icon != null) icon.color = usable ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }

            if (queued != shownQueued)
            {
                shownQueued = queued;
                queuedRing.SetActive(queued);
            }
        }
    }
}
