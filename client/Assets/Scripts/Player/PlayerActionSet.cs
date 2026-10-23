using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.Player
{
    /// <summary>Which Animator layer an action plays on.</summary>
    public enum ActionBody
    {
        /// <summary>Whole body when standing, upper body while the legs run (attacks, skill forms).</summary>
        Auto,
        /// <summary>Always the whole body (death, getting up, the leap).</summary>
        Full,
        /// <summary>Always the upper body only (drinking a potion).</summary>
        Upper,
        /// <summary>The flinch layer: upper body, blended over whatever else plays (hit reactions).</summary>
        Flinch,
    }

    /// <summary>
    /// The player's one-shot animations: attacks per weapon, skill forms, hit, death, revive, leap and
    /// potion. Presentation only, so it lives with the client, not in the shared game data.
    /// <see cref="imports"/> says how each source clip is cut out of its FBX; <see cref="actions"/> are
    /// the Animator states made from them. <c>Wuxia → Animation → Build Player Actions</c> applies the
    /// imports and regenerates the action layers of the characters' controller from this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Wuxia/Player Action Set")]
    public class PlayerActionSet : ScriptableObject
    {
        [Serializable]
        public class ClipImport
        {
            [Tooltip("Name of the clip cut out of the source; actions refer to it by this name.")]
            public string name;
            [Tooltip("Project path of the FBX (Mixamo, without skin, imported as Humanoid).")]
            public string source;
            [Tooltip("Seconds into the source take.")]
            public float start;
            public float end;
            [Tooltip("Mirror left/right, e.g. Mixamo's archers hold the bow in the left hand; ours holds it in the right.")]
            public bool mirror;
            [Tooltip("Strip the clip's travel across the ground (lunges, combos that step forward) instead of keeping it in the pose, so it never snaps back.")]
            public bool inPlace;
        }

        [Serializable]
        public class Action
        {
            [Tooltip("Animator state name. Code finds actions by name: '<weapon>_basic_<n>' (cycled), a skill form id, or hit, death, revive, drink, leap, leap_land.")]
            public string state;
            [Tooltip("Clip name: one of the imports above, or a clip of the extra sources.")]
            public string clip;
            [Tooltip("Optional state that follows when this one ends (the bow: release, then draw the next arrow).")]
            public string next;
            public ActionBody body;
            [Tooltip("Playback speed when the game doesn't ask for a duration (hit, death, leap, potion).")]
            public float speed = 1f;
            [Tooltip("Stays on its last frame (death) or loops (in the air) until another action plays.")]
            public bool hold;
            [Tooltip("Filled by the builder from the clip name.")]
            public AnimationClip resolved;
        }

        public ClipImport[] imports;
        [Tooltip("Other FBX files to look clips up in by name (the Quaternius library).")]
        public string[] extraSources;
        public Action[] actions;

        Dictionary<string, Action> byState;

        public Action Find(string state)
        {
            if (byState == null)
            {
                byState = new Dictionary<string, Action>();
                foreach (var a in actions) byState[a.state] = a;
            }
            return state != null && byState.TryGetValue(state, out var action) ? action : null;
        }

        void OnValidate() => byState = null;
    }
}
