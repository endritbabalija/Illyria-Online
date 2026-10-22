using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Combat;

namespace Wuxia.UI
{
    /// <summary>Pooled damage numbers, crits and misses that rise and fade above whoever was attacked.</summary>
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

        void OnEnable() => Combatant.AnyAttackResolved += Show;
        void OnDisable() => Combatant.AnyAttackResolved -= Show;

        void Show(Combatant target, AttackResult result)
        {
            var entry = pool.Count > 0 ? pool.Pop() : Create();
            entry.Text.gameObject.SetActive(true);
            entry.Text.text = !result.Hit ? "Miss" : result.Crit ? $"{result.Damage}!" : result.Damage.ToString();
            var color = !result.Hit ? miss : result.Crit ? crit : target.Team == Team.Player ? damageTaken : damageDealt;
            entry.Text.color = color;
            entry.Text.fontSize = result.Crit ? Mathf.RoundToInt(baseFontSize * critScale) : baseFontSize;
            var jitter = Random.insideUnitCircle * 0.3f;
            entry.World = target.transform.position + new Vector3(jitter.x, target.AimHeight, jitter.y);
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
