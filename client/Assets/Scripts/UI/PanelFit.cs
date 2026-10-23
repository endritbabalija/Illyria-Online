using UnityEngine;

namespace Wuxia.UI
{
    /// <summary>
    /// Places a fixed-size panel (Bag, Shop, Skills) in its parent (the safe area) without covering
    /// the HUD frames that stay visible while it's open (player HP/Spirit, gold): either to their
    /// right or below them, whichever lets the panel be larger, scaled down to fit (never up).
    /// Refits when the screen size or safe area changes, not every frame.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PanelFit : MonoBehaviour
    {
        [Tooltip("HUD frames the panel must leave uncovered (same parent as the panel).")]
        [SerializeField] RectTransform[] keepVisible;
        [Tooltip("Space kept free along the parent's edges, in canvas units.")]
        [SerializeField] float margin = 16f;
        [Tooltip("Space between the panel and the frames it leaves uncovered, in canvas units.")]
        [SerializeField] float gap = 16f;

        RectTransform rect;
        RectTransform parent;
        Rect fittedFor;
        static readonly Vector3[] corners = new Vector3[4];

        void Awake()
        {
            rect = (RectTransform)transform;
            parent = (RectTransform)transform.parent;
        }

        void OnEnable() => Fit();

        void LateUpdate()
        {
            if (parent.rect != fittedFor) Fit();
        }

        public void Fit()
        {
            if (rect == null) Awake();
            var area = parent.rect;
            fittedFor = area;

            // The frames' extent in the parent's space.
            var reserved = new Rect();
            var any = false;
            foreach (var frame in keepVisible)
            {
                if (frame == null || !frame.gameObject.activeInHierarchy) continue;
                frame.GetWorldCorners(corners);
                for (var i = 0; i < 4; i++)
                {
                    var p = (Vector2)parent.InverseTransformPoint(corners[i]);
                    if (!any) reserved = new Rect(p, Vector2.zero);
                    else reserved = Rect.MinMaxRect(Mathf.Min(reserved.xMin, p.x), Mathf.Min(reserved.yMin, p.y),
                        Mathf.Max(reserved.xMax, p.x), Mathf.Max(reserved.yMax, p.y));
                    any = true;
                }
            }

            var inner = Rect.MinMaxRect(area.xMin + margin, area.yMin + margin, area.xMax - margin, area.yMax - margin);
            var best = inner;
            if (any)
            {
                var right = Rect.MinMaxRect(Mathf.Max(inner.xMin, reserved.xMax + gap), inner.yMin, inner.xMax, inner.yMax);
                var below = Rect.MinMaxRect(inner.xMin, inner.yMin, inner.xMax, Mathf.Min(inner.yMax, reserved.yMin - gap));
                best = ScaleFor(right) > ScaleFor(below) ? right : below;
            }

            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var scale = ScaleFor(best);
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = best.center - area.center;
        }

        float ScaleFor(Rect space)
        {
            var size = rect.rect.size;
            if (space.width <= 0f || space.height <= 0f || size.x <= 0f || size.y <= 0f) return 0f;
            return Mathf.Min(1f, space.width / size.x, space.height / size.y);
        }
    }
}
