using UnityEngine;

namespace Wuxia.UI
{
    /// <summary>
    /// Keeps an overlay-canvas element over a point in the world (an NPC's name). Hidden behind the
    /// camera or beyond <see cref="maxDistance"/>; only moves the element, never rebuilds its text.
    /// </summary>
    [DefaultExecutionOrder(200)] // after the camera has moved this frame
    public class WorldLabel : MonoBehaviour
    {
        [SerializeField] Transform anchor;
        [SerializeField] Vector3 offset = new Vector3(0f, 2.4f, 0f);
        [SerializeField] Camera viewCamera;
        [Tooltip("The element to move and hide; its parent must fill the canvas.")]
        [SerializeField] RectTransform label;
        [SerializeField] float maxDistance = 35f;

        RectTransform layer;
        bool shown = true;

        void Awake()
        {
            if (label != null) layer = (RectTransform)label.parent;
        }

        /// <summary>Sets up a label added from code (a remote player's name).</summary>
        public void Configure(Transform anchorTransform, Camera cam, RectTransform labelRect, Vector3 labelOffset)
        {
            anchor = anchorTransform;
            viewCamera = cam;
            label = labelRect;
            offset = labelOffset;
            layer = (RectTransform)label.parent;
        }

        void LateUpdate()
        {
            if (label == null || anchor == null || viewCamera == null) return;
            var world = anchor.position + offset;
            var screen = viewCamera.WorldToScreenPoint(world);
            var visible = screen.z > 0f && screen.z < maxDistance;
            if (visible != shown)
            {
                shown = visible;
                label.gameObject.SetActive(visible);
            }
            if (!visible) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out var local);
            if ((label.anchoredPosition - local).sqrMagnitude > 0.25f) label.anchoredPosition = local;
        }
    }
}
