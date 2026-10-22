using UnityEngine;

namespace Wuxia.UI
{
    /// <summary>Fits this RectTransform to Screen.safeArea so HUD stays clear of notches and rounded corners.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int appliedScreen;

        void Update()
        {
            var area = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (area == applied && screen == appliedScreen) return;
            if (screen.x <= 0 || screen.y <= 0) return;
            applied = area;
            appliedScreen = screen;

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            rect.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
