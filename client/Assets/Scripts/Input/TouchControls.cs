using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Wuxia.Input
{
    /// <summary>
    /// All touch handling outside of UI buttons. A touch that starts on the left side of the screen
    /// becomes the floating joystick; one that starts anywhere else swipes the camera. A short touch
    /// that barely moves also counts as a tap (for target lock). Touches that start on a UI element
    /// are left to the EventSystem.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TouchControls : MonoBehaviour
    {
        enum Role { Ui, Joystick, Look }

        struct Tracked
        {
            public Role Role;
            public Vector2 Start;
            public float MaxTravel;
        }

        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform joystickBase;
        [SerializeField] RectTransform joystickKnob;
        [Tooltip("Fraction of the screen width, from the left, where a touch grabs the joystick.")]
        [SerializeField, Range(0.2f, 0.6f)] float joystickZone = 0.45f;
        [Tooltip("Knob travel in canvas units.")]
        [SerializeField] float joystickRadius = 110f;
        [Tooltip("Max movement, in canvas units, for a touch to still count as a tap.")]
        [SerializeField] float tapMaxTravel = 30f;
        [SerializeField] float tapMaxDuration = 0.35f;
        [Tooltip("Camera rotation in degrees for a swipe the full height of the screen.")]
        [SerializeField] float lookDegreesPerScreen = 220f;

        /// <summary>Joystick direction, magnitude 0–1 (x = right, y = up/forward).</summary>
        public Vector2 Move { get; private set; }
        /// <summary>Camera rotation requested this frame, in degrees (x = yaw, y = pitch).</summary>
        public Vector2 LookDelta { get; private set; }
        /// <summary>Screen position of a tap that didn't start on UI.</summary>
        public event Action<Vector2> Tapped;

        readonly Dictionary<int, Tracked> touches = new Dictionary<int, Tracked>();
        readonly HashSet<int> seenThisFrame = new HashSet<int>();
        readonly List<int> stale = new List<int>();
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        PointerEventData pointer;
        Vector2 joystickRest;
        int joystickTouch = -1;

        void Awake() => joystickRest = joystickBase.anchoredPosition;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNITY_STANDALONE)
            // Lets the plain Game view (and a PC development build) drive touches with the mouse. The
            // Input System skips this while the Device Simulator is open, since the simulator supplies
            // its own touchscreen. Not on phones: a stylus there would count as two touches.
            TouchSimulation.Enable();
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNITY_STANDALONE)
            TouchSimulation.Disable();
#endif
            EnhancedTouchSupport.Disable();
            ReleaseAll();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) ReleaseAll();
        }

        void Update()
        {
            LookDelta = Vector2.zero;
            seenThisFrame.Clear();
            var scale = canvas.scaleFactor;

            foreach (var touch in ETouch.activeTouches)
            {
                var id = touch.touchId;
                var position = touch.screenPosition;
                seenThisFrame.Add(id);

                // First sighting is normally the Began phase, but a very quick tap can already be Ended.
                if (!touches.TryGetValue(id, out var tracked))
                {
                    tracked = new Tracked { Role = Classify(position), Start = position };
                    if (tracked.Role == Role.Joystick)
                    {
                        joystickTouch = id;
                        joystickBase.position = position; // overlay canvas: world space == screen pixels
                    }
                }

                tracked.MaxTravel = Mathf.Max(tracked.MaxTravel, (position - tracked.Start).magnitude / scale);
                var ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;

                if (!ended)
                {
                    if (tracked.Role == Role.Joystick)
                    {
                        var offset = (position - (Vector2)joystickBase.position) / (joystickRadius * scale);
                        Move = Vector2.ClampMagnitude(offset, 1f);
                        joystickKnob.anchoredPosition = Move * joystickRadius;
                    }
                    else if (tracked.Role == Role.Look)
                    {
                        LookDelta += touch.delta / Screen.height * lookDegreesPerScreen;
                    }
                    touches[id] = tracked;
                    continue;
                }

                var isTap = touch.phase == TouchPhase.Ended
                    && tracked.Role != Role.Ui
                    && tracked.MaxTravel <= tapMaxTravel
                    && touch.time - touch.startTime <= tapMaxDuration;
                Release(id);
                if (isTap) Tapped?.Invoke(position);
            }

            // A touch can vanish without an Ended phase (device removed, focus lost); don't leave it stuck.
            stale.Clear();
            foreach (var id in touches.Keys)
                if (!seenThisFrame.Contains(id)) stale.Add(id);
            foreach (var id in stale) Release(id);
        }

        Role Classify(Vector2 position)
        {
            if (IsOverUi(position)) return Role.Ui;
            if (joystickTouch < 0 && position.x < Screen.width * joystickZone) return Role.Joystick;
            return Role.Look;
        }

        bool IsOverUi(Vector2 position)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            if (pointer == null) pointer = new PointerEventData(eventSystem);
            pointer.position = position;
            uiHits.Clear();
            eventSystem.RaycastAll(pointer, uiHits);
            return uiHits.Count > 0;
        }

        void Release(int id)
        {
            touches.Remove(id);
            if (id != joystickTouch) return;
            joystickTouch = -1;
            Move = Vector2.zero;
            joystickBase.anchoredPosition = joystickRest;
            joystickKnob.anchoredPosition = Vector2.zero;
        }

        void ReleaseAll()
        {
            stale.Clear();
            stale.AddRange(touches.Keys);
            foreach (var id in stale) Release(id);
            LookDelta = Vector2.zero;
        }
    }
}
