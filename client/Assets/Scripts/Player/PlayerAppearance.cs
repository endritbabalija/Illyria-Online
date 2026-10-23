using System;
using System.Collections.Generic;
using UnityEngine;
using Wuxia.World;

namespace Wuxia.Player
{
    /// <summary>
    /// Shows the current character's model and the held weapon's model, and tints the greybox cloth
    /// per character. The weapons share one pivot that moves to the active model's right hand; paired
    /// weapons (the sicas) put their second model under an off-hand pivot that moves to the left hand.
    /// A wielded item whose modelKey has a prefab in <see cref="ItemModelRegistry"/> shows that model;
    /// anything else shows the greybox model of its weapon type.
    /// </summary>
    public class PlayerAppearance : MonoBehaviour
    {
        [Serializable]
        public struct WeaponModel
        {
            public string weaponId;
            [Tooltip("Right-hand model, under the weapon pivot.")]
            public GameObject model;
            [Tooltip("Optional left-hand model for paired weapons, under the off-hand pivot.")]
            public GameObject offHand;
        }

        [Serializable]
        public struct CharacterLook
        {
            public string characterId;
            public Color cloth;
            [Tooltip("The character's Humanoid model; only the current character's is active.")]
            public Animator model;
        }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] PlayerCombat combat;
        [SerializeField] PlayerAnimator playerAnimator;
        [SerializeField] Renderer body;
        [Tooltip("Material slot on the body renderer that holds the cloth.")]
        [SerializeField] int clothMaterialIndex;
        [Tooltip("Parent of every weapon model; reparented onto the active model's RightHand bone, keeping its local pose.")]
        [SerializeField] Transform weaponPivot;
        [Tooltip("Parent of every off-hand model; reparented onto the active model's LeftHand bone, keeping its local pose.")]
        [SerializeField] Transform offHandPivot;
        [SerializeField] WeaponModel[] weapons;
        [Tooltip("modelKey → prefab for item models (Assets/Items/ItemModels.asset).")]
        [SerializeField] ItemModelRegistry itemModels;
        [SerializeField] CharacterLook[] looks;

        MaterialPropertyBlock block;
        // One instance per item model, made the first time it's wielded and kept under the pivot.
        readonly Dictionary<string, GameObject> itemInstances = new Dictionary<string, GameObject>();

        void Awake() => block = new MaterialPropertyBlock();

        void OnEnable() => combat.LoadoutChanged += Apply;
        void OnDisable() => combat.LoadoutChanged -= Apply;

        void Start() => Apply();

        void Apply()
        {
            var shown = ItemModel();
            foreach (var instance in itemInstances.Values) instance.SetActive(instance == shown);
            foreach (var w in weapons)
            {
                var wielded = shown == null && w.weaponId == combat.Weapon.id;
                if (w.model != null) w.model.SetActive(wielded);
                if (w.offHand != null) w.offHand.SetActive(wielded);
            }

            // Turn the others off first so only one Animator is ever live.
            foreach (var look in looks)
                if (look.model != null && look.characterId != combat.Character.id) look.model.gameObject.SetActive(false);

            foreach (var look in looks)
            {
                if (look.characterId != combat.Character.id) continue;
                body.GetPropertyBlock(block, clothMaterialIndex);
                block.SetColor(BaseColor, look.cloth);
                body.SetPropertyBlock(block, clothMaterialIndex);

                if (look.model == null) continue;
                look.model.gameObject.SetActive(true);
                // Both rigs share bone names and hand orientation, so the grip pose carries over as is.
                var hand = look.model.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand != null) weaponPivot.SetParent(hand, false);
                var leftHand = look.model.GetBoneTransform(HumanBodyBones.LeftHand);
                if (offHandPivot != null && leftHand != null) offHandPivot.SetParent(leftHand, false);
                playerAnimator.SetAnimator(look.model);
            }
        }

        /// <summary>The wielded item's own model under the pivot, or null to use the greybox one.</summary>
        GameObject ItemModel()
        {
            var item = combat.Items.Equipment.Weapon;
            if (item == null || itemModels == null) return null;
            var key = item.Def.modelKey;
            var prefab = itemModels.FindPrefab(key);
            if (prefab == null) return null;
            if (!itemInstances.TryGetValue(key, out var instance))
            {
                instance = Instantiate(prefab, weaponPivot, false);
                instance.name = prefab.name;
                foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Destroy(c);
                foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = weaponPivot.gameObject.layer;
                itemInstances[key] = instance;
            }
            return instance;
        }
    }
}
