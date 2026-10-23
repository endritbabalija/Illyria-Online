using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.UI
{
    /// <summary>
    /// Pooled damage numbers, crits, misses, skill hits, poison ticks and status names that rise and
    /// fade above whoever was hit, plus loot pickups through <see cref="ShowAt"/>.
    /// </summary>
    [DefaultExecutionOrder(200)] // after the camera has moved this frame
    public class FloatingCombatText : MonoBehaviour
    {
        class Entry
        {
            public Text Text;
            public RectTransform Rect;
            public Vector3 World;
            public float Age;
        }

        [Tooltip("Disabled child used as the template for every number.")]
        [SerializeField] Text template;
        [SerializeField] Camera viewCamera;
        [SerializeField] float lifetime = 0.9f;
        [SerializeField] float rise = 1.2f;
        [SerializeField] float critScale = 1.4f;
        [SerializeField] Color damageDealt = Color.white;
        [SerializeField] Color damageTaken = new Color(1f, 0.35f, 0.3f);
        [SerializeField] Color crit = new Color(1f, 0.82f, 0.2f);
        [SerializeField] Color miss = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] Color skill = new Color(0.55f, 0.85f, 1f);
        [SerializeField] Color poisonTick = new Color(0.45f, 0.95f, 0.35f);
        [SerializeField] Color status = new Color(0.85f, 0.6f, 1f);
        [Tooltip("Status names start this far above the damage numbers, in metres.")]
        [SerializeField] float statusLift = 0.5f;

        readonly List<Entry> active = new List<Entry>();
        readonly Stack<Entry> pool = new Stack<Entry>();
        RectTransform layer;
        int baseFontSize;

        void Awake()
        {
            layer = (RectTransform)transform;
            baseFontSize = template.fontSize;
            template.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            Combatant.AnyAttackResolved += Show;
            Combatant.AnyStatusApplied += ShowStatus;
        }

        void OnDisable()
        {
            Combatant.AnyAttackResolved -= Show;
            Combatant.AnyStatusApplied -= ShowStatus;
        }

        void Show(Combatant target, AttackResult result)
        {
            var text = !result.Hit ? "Miss" : result.Crit ? $"{result.Damage}!" : result.Damage.ToString();
            Color color;
            if (!result.Hit) color = miss;
            else if (result.Crit) color = crit;
            else if (result.Kind == HitKind.Poison) color = poisonTick;
            else if (target.Team == Team.Player) color = damageTaken;
            else color = result.Kind == HitKind.Skill ? skill : damageDealt;
            Spawn(target, text, color, result.Crit ? critScale : 1f, 0f);
        }

        void ShowStatus(Combatant target, StatusData data) => Spawn(target, data.name, status, 0.8f, statusLift);

        void Spawn(Combatant target, string text, Color color, float scale, float lift) =>
            ShowAt(target.transform.position + Vector3.up * (target.AimHeight + lift), text, color, scale);

        /// <summary>Floats any text up from a world point (loot pickups: "+7 gold", item names in rarity colour).</summary>
        public void ShowAt(Vector3 world, string text, Color color, float scale = 1f)
        {
            var entry = pool.Count > 0 ? pool.Pop() : Create();
            entry.Text.gameObject.SetActive(true);
            entry.Text.text = text;
            entry.Text.color = color;
            entry.Text.fontSize = Mathf.RoundToInt(baseFontSize * scale);
            var jitter = Random.insideUnitCircle * 0.3f;
            entry.World = world + new Vector3(jitter.x, 0f, jitter.y);
            entry.Age = 0f;
            active.Add(entry);
        }

        Entry Create()
        {
            var text = Instantiate(template, layer);
            return new Entry { Text = text, Rect = text.rectTransform };
        }

        void LateUpdate()
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var entry = active[i];
                entry.Age += Time.deltaTime;
                var k = entry.Age / lifetime;
                if (k >= 1f)
                {
                    entry.Text.gameObject.SetActive(false);
                    pool.Push(entry);
                    active.RemoveAt(i);
                    continue;
                }

                var screen = viewCamera.WorldToScreenPoint(entry.World + Vector3.up * (rise * k));
                entry.Text.enabled = screen.z > 0f;
                if (!entry.Text.enabled) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out var local);
                entry.Rect.anchoredPosition = local;
                var c = entry.Text.color;
                c.a = 1f - k * k;
                entry.Text.color = c;
            }
        }
    }
}
