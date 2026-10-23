using UnityEngine;
using UnityEngine.Rendering;

namespace Wuxia.Player
{
    /// <summary>
    /// Dithers away the scatter (trees, rocks and bushes on the Wuxia/Foliage shader) between the camera and the
    /// player, and any right in front of the lens. Scatter is merged into one mesh per cell, so the fade is per pixel:
    /// this sets a capsule from the camera to the player's chest as shader globals, and the shader clips the fragments
    /// inside it through a screen-space dither (see Art/Shaders/OccluderFade.hlsl). The clip keyword is only on while
    /// a trunk or rock stands near that line (clipping shaders cost more on phone GPUs), easing in and out.
    /// </summary>
    [DefaultExecutionOrder(110)] // after ThirdPersonCamera has placed the camera
    [RequireComponent(typeof(ThirdPersonCamera))]
    public class CameraOccluderFade : MonoBehaviour
    {
        [Tooltip("Height of the capsule's player end above the player's feet.")]
        [SerializeField] float chestHeight = 1.3f;
        [Tooltip("Scatter within this distance of the camera-to-chest line is cleared (metres).")]
        [SerializeField] float radius = 2f;
        [Tooltip("Width of the dithered rim just inside the radius, where scatter is partly cleared (metres).")]
        [SerializeField] float edgeSoftness = 0.5f;
        [Tooltip("Over this last stretch before the chest the fade eases out, so scatter beside and behind the player stays solid (metres).")]
        [SerializeField] float endSoftness = 1.5f;
        [Tooltip("Scatter this close to the camera is cleared in any direction (metres).")]
        [SerializeField] float nearRadius = 2f;
        [Tooltip("Width of the dithered rim around the near radius (metres).")]
        [SerializeField] float nearSoftness = 0.75f;
        [Tooltip("Share of the scatter inside the capsule that's cleared (1: all of it).")]
        [Range(0f, 1f)]
        [SerializeField] float maxFade = 1f;
        [Tooltip("Seconds to fade in once something is near the line.")]
        [SerializeField] float fadeInTime = 0.3f;
        [Tooltip("Seconds to fade back out once the line is clear.")]
        [SerializeField] float fadeOutTime = 0.5f;
        [Tooltip("Colliders that switch the fade on when one is within radius + crown margin of the line (tree trunks and rocks are on Obstacle).")]
        [SerializeField] LayerMask occluders = 1 << 11;
        [Tooltip("Extra reach for that check, about the widest crown's radius: a trunk can stand this far outside the capsule while its crown is in it (metres).")]
        [SerializeField] float crownMargin = 8f;

        static readonly int StartId = Shader.PropertyToID("_OccluderFadeStart");
        static readonly int AxisId = Shader.PropertyToID("_OccluderFadeAxis");
        static readonly int ShapeId = Shader.PropertyToID("_OccluderFadeShape");
        static readonly int NearId = Shader.PropertyToID("_OccluderFadeNear");

        ThirdPersonCamera follow;
        GlobalKeyword keyword;
        bool keywordOn;
        float weight;

        void Awake() => follow = GetComponent<ThirdPersonCamera>();

        void OnEnable()
        {
            keyword = GlobalKeyword.Create("_OCCLUDER_FADE");
            Shader.SetKeyword(keyword, false);
        }

        void OnDisable()
        {
            weight = 0f;
            SetKeyword(false);
        }

        void LateUpdate()
        {
            var target = follow.Target;
            if (target == null) return;

            var from = transform.position;
            var to = target.position + Vector3.up * chestHeight;
            var blocked = Physics.CheckCapsule(from, to, radius + crownMargin, occluders, QueryTriggerInteraction.Ignore);
            var time = blocked ? fadeInTime : fadeOutTime;
            weight = Mathf.MoveTowards(weight, blocked ? 1f : 0f, time > 0f ? Time.unscaledDeltaTime / time : 1f);
            SetKeyword(weight > 0f);
            if (!keywordOn) return;

            var axis = to - from;
            var length = axis.magnitude;
            var direction = length > 1e-4f ? axis / length : transform.forward;
            Shader.SetGlobalVector(StartId, new Vector4(from.x, from.y, from.z, Mathf.SmoothStep(0f, maxFade, weight)));
            Shader.SetGlobalVector(AxisId, new Vector4(direction.x, direction.y, direction.z, length));
            Shader.SetGlobalVector(ShapeId, new Vector4(radius, Mathf.Max(0.01f, edgeSoftness), Mathf.Max(0.01f, endSoftness), 0f));
            Shader.SetGlobalVector(NearId, new Vector4(nearRadius, Mathf.Max(0.01f, nearSoftness), 0f, 0f));
        }

        void SetKeyword(bool on)
        {
            if (on == keywordOn) return;
            keywordOn = on;
            Shader.SetKeyword(keyword, on);
        }
    }
}
