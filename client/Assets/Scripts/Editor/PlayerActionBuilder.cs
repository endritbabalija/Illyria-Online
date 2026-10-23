using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Wuxia.Player;

namespace Wuxia.EditorTools
{
    /// <summary>
    /// Builds the player's one-shot animations from PlayerActions.asset (<see cref="PlayerActionSet"/>):
    /// cuts each import out of its FBX (Humanoid, trimmed, mirrored, in place where asked), points every
    /// action at its clip, and regenerates the action layers of the characters' shared controller. The
    /// Base Layer (locomotion) is left alone; the ActionFull, ActionUpper (upper-body mask) and Flinch
    /// layers are rebuilt from scratch, in that order, which <see cref="PlayerAnimator"/> relies on.
    /// Menu: Wuxia → Animation → Build Player Actions. From MCP: Wuxia.EditorTools.PlayerActionBuilder.Build().
    /// </summary>
    public static class PlayerActionBuilder
    {
        public const string SetPath = "Assets/Characters/Animations/PlayerActions.asset";
        const string ControllerPath = "Assets/Characters/Kreshnik/Kreshnik.controller";
        const string MaskPath = "Assets/Characters/Animations/UpperBody.mask";
        const string SpeedParameter = "ActionSpeed";
        static readonly string[] Layers = { "ActionFull", "ActionUpper", "Flinch" };

        [MenuItem("Wuxia/Animation/Build Player Actions")]
        static void BuildFromMenu() => Debug.Log(Build());

        public static string Build()
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayerActionSet>(SetPath);
            if (set == null) return $"No action set at {SetPath}";
            var log = new StringBuilder();

            foreach (var group in set.imports.GroupBy(i => i.source))
                ApplyImports(group.Key, group.ToList(), log);

            var clips = new Dictionary<string, AnimationClip>();
            foreach (var path in set.imports.Select(i => i.source).Concat(set.extraSources ?? new string[0]).Distinct())
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    if (!clip.name.StartsWith("__preview")) clips[clip.name] = clip;

            foreach (var action in set.actions)
            {
                clips.TryGetValue(action.clip ?? "", out action.resolved);
                if (action.resolved == null) log.AppendLine($"Missing clip '{action.clip}' for {action.state}");
                if (!string.IsNullOrEmpty(action.next) && set.Find(action.next) == null)
                    log.AppendLine($"{action.state}: next state '{action.next}' doesn't exist");
            }
            EditorUtility.SetDirty(set);

            BuildController(set, UpperBodyMask());
            AssetDatabase.SaveAssets();
            log.AppendLine($"Built {set.actions.Length} actions from {set.imports.Length} imported clips into {ControllerPath}");
            return log.ToString();
        }

        static void ApplyImports(string path, List<PlayerActionSet.ClipImport> imports, StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                log.AppendLine($"No model at {path}");
                return;
            }
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;

            var take = importer.importedTakeInfos.FirstOrDefault();
            if (string.IsNullOrEmpty(take.name))
            {
                // A first import of a new file: read its take before cutting clips out of it.
                importer.SaveAndReimport();
                take = importer.importedTakeInfos.FirstOrDefault();
            }
            var fps = take.sampleRate > 0f ? take.sampleRate : 30f;

            importer.clipAnimations = imports.Select(i => new ModelImporterClipAnimation
            {
                name = i.name,
                takeName = take.name,
                firstFrame = Mathf.Round(i.start * fps),
                lastFrame = Mathf.Round(Mathf.Min(i.end, take.stopTime) * fps),
                mirror = i.mirror,
                loopTime = false,
                // Turning stays in the pose (spin attacks), so does the height (falling, crouching).
                lockRootRotation = true,
                keepOriginalOrientation = true,
                lockRootHeightY = true,
                heightFromFeet = true,
                // Travel either stays in the pose or is stripped (applyRootMotion is off).
                lockRootPositionXZ = !i.inPlace,
                keepOriginalPositionXZ = true,
            }).ToArray();
            importer.SaveAndReimport();
        }

        static AvatarMask UpperBodyMask()
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MaskPath);
            }
            for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
            {
                var upper = part == AvatarMaskBodyPart.Body || part == AvatarMaskBodyPart.Head
                    || part == AvatarMaskBodyPart.LeftArm || part == AvatarMaskBodyPart.RightArm
                    || part == AvatarMaskBodyPart.LeftFingers || part == AvatarMaskBodyPart.RightFingers
                    || part == AvatarMaskBodyPart.LeftHandIK || part == AvatarMaskBodyPart.RightHandIK;
                mask.SetHumanoidBodyPartActive(part, upper);
            }
            EditorUtility.SetDirty(mask);
            return mask;
        }

        static void BuildController(PlayerActionSet set, AvatarMask upperBody)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller.parameters.All(p => p.name != SpeedParameter))
                controller.AddParameter(new AnimatorControllerParameter
                {
                    name = SpeedParameter, type = AnimatorControllerParameterType.Float, defaultFloat = 1f,
                });

            for (var i = controller.layers.Length - 1; i >= 1; i--)
                if (Layers.Contains(controller.layers[i].name)) controller.RemoveLayer(i);

            var flinch = set.actions.Where(a => a.body == ActionBody.Flinch).ToList();
            var actions = set.actions.Where(a => a.body != ActionBody.Flinch).ToList();
            AddLayer(controller, Layers[0], null, actions);
            AddLayer(controller, Layers[1], upperBody, actions);
            AddLayer(controller, Layers[2], upperBody, flinch);
            EditorUtility.SetDirty(controller);
        }

        static void AddLayer(AnimatorController controller, string name, AvatarMask mask, List<PlayerActionSet.Action> actions)
        {
            var machine = new AnimatorStateMachine { name = name, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(machine, controller);
            controller.AddLayer(new AnimatorControllerLayer
            {
                name = name,
                stateMachine = machine,
                avatarMask = mask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                // PlayerAnimator sets the weight while an action plays.
                defaultWeight = 0f,
            });

            // Nothing plays until code cross-fades into an action; a finished action rests on its last
            // frame while the layer weight fades out.
            var none = machine.AddState("None", new Vector3(300f, 0f));
            none.writeDefaultValues = false;
            machine.defaultState = none;

            var states = new Dictionary<string, AnimatorState>();
            var row = 0;
            foreach (var action in actions)
            {
                var state = machine.AddState(action.state, new Vector3(300f + 250f * (row % 4), 80f + 60f * (row / 4)));
                row++;
                state.motion = action.resolved;
                state.speed = action.speed;
                state.speedParameter = SpeedParameter;
                state.speedParameterActive = true;
                state.writeDefaultValues = false;
                states[action.state] = state;
            }

            foreach (var action in actions)
            {
                if (string.IsNullOrEmpty(action.next) || !states.TryGetValue(action.next, out var next)) continue;
                var t = states[action.state].AddTransition(next);
                t.hasExitTime = true;
                t.exitTime = 1f;
                t.hasFixedDuration = true;
                t.duration = 0.05f;
            }
        }
    }
}
