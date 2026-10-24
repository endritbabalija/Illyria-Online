using System;
using System.Collections.Generic;
using UnityEngine;
using Wuxia.Combat;
using Wuxia.World;

namespace Wuxia.Player
{
    /// <summary>
    /// Shows the current character's model and the held weapon's model, and tints the greybox cloth
    /// per character. The weapons share one pivot that moves to the active model's right hand; paired
    /// weapons (the sicas) put their second model under an off-hand pivot that moves to the left hand.
    /// A wielded item whose modelKey has a prefab in <see cref="ItemModelRegistry"/> shows that model
    /// (and its off-hand model, if the entry has one); anything else shows the greybox model of its weapon type.
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
        // One instance per item model (main hand, off hand or null), made the first time it's wielded and kept under the pivots.
        readonly Dictionary<string, GameObject[]> itemInstances = new Dictionary<string, GameObject[]>();

        /// <summary>The weapon pivots on the active model's hands.</summary>
        public WeaponHands Hands => new WeaponHands { Main = weaponPivot, OffHand = offHandPivot };

        void Awake() => block = new MaterialPropertyBlock();

        void OnEnable() => combat.LoadoutChanged += Apply;
        void OnDisable() => combat.LoadoutChanged -= Apply;

        void Start() => Apply();

        void Apply()
        {
            var shown = ItemModel();
            foreach (var models in itemInstances.Values)
                foreach (var m in models)
                    if (m != null) m.SetActive(models == shown);
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

        /// <summary>
        /// Weapon models for another character model (other players): pivots on its hands with the same
        /// local pose as ours, holding copies of our greybox weapons or the item models.
        /// </summary>
        public WeaponRig CreateWeaponRig(Animator model)
        {
            var hand = model.GetBoneTransform(HumanBodyBones.RightHand);
            var leftHand = model.GetBoneTransform(HumanBodyBones.LeftHand);
            if (hand == null) return null;
            return new WeaponRig(this, Pivot(weaponPivot, hand), offHandPivot != null && leftHand != null ? Pivot(offHandPivot, leftHand) : null);
        }

        static Transform Pivot(Transform ours, Transform bone)
        {
            var pivot = new GameObject(ours.name).transform;
            pivot.SetParent(bone, false);
            pivot.SetLocalPositionAndRotation(ours.localPosition, ours.localRotation);
            pivot.localScale = ours.localScale;
            return pivot;
        }

        /// <summary>The weapon models another character model holds; <see cref="Show"/> picks the wielded one.</summary>
        public class WeaponRig
        {
            readonly PlayerAppearance source;
            readonly Transform pivot;
            readonly Transform offHandPivot;
            // One instance per item model key or greybox weapon type, made when first wielded.
            readonly Dictionary<string, GameObject[]> instances = new Dictionary<string, GameObject[]>();
            GameObject[] shown;

            /// <summary>The pivots on the model's hands.</summary>
            public WeaponHands Hands => new WeaponHands { Main = pivot, OffHand = offHandPivot };

            public WeaponRig(PlayerAppearance source, Transform pivot, Transform offHandPivot)
            {
                this.source = source;
                this.pivot = pivot;
                this.offHandPivot = offHandPivot;
            }

            /// <summary>Holds this weapon item: its own model if it has one, else the greybox model of its type.</summary>
            public void Show(Data.ItemDef weapon)
            {
                var models = weapon != null ? Models(weapon) : null;
                if (models == shown) return;
                if (shown != null) foreach (var m in shown) if (m != null) m.SetActive(false);
                shown = models;
                if (shown != null) foreach (var m in shown) if (m != null) m.SetActive(true);
            }

            GameObject[] Models(Data.ItemDef weapon)
            {
                var entry = source.itemModels != null ? source.itemModels.Find(weapon.modelKey) : null;
                var key = entry != null ? "item:" + weapon.modelKey : "type:" + weapon.weaponType;
                if (instances.TryGetValue(key, out var made)) return made;
                if (entry != null) made = new[] { Copy(entry.prefab, pivot), offHandPivot != null ? Copy(entry.offHandPrefab, offHandPivot) : null };
                else
                {
                    var i = Array.FindIndex(source.weapons, w => w.weaponId == weapon.weaponType);
                    if (i < 0) return null;
                    var w = source.weapons[i];
                    made = new[] { Copy(w.model, pivot), offHandPivot != null ? Copy(w.offHand, offHandPivot) : null };
                }
                instances[key] = made;
                return made;
            }

            /// <summary>A copy under <paramref name="parent"/> with the original's local pose, no colliders, on the parent's layer.</summary>
            static GameObject Copy(GameObject original, Transform parent)
            {
                if (original == null) return null;
                var copy = Instantiate(original, parent, false);
                copy.name = original.name;
                copy.transform.SetLocalPositionAndRotation(original.transform.localPosition, original.transform.localRotation);
                copy.transform.localScale = original.transform.localScale;
                foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Destroy(c);
                foreach (var t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = parent.gameObject.layer;
                copy.SetActive(false);
                return copy;
            }
        }

        /// <summary>The wielded item's own models under the pivots (main hand, off hand or null), or null to use the greybox ones.</summary>
        GameObject[] ItemModel()
        {
            var item = combat.Items.Equipment.Weapon;
            if (item == null || itemModels == null) return null;
            var key = item.Def.modelKey;
            var entry = itemModels.Find(key);
            if (entry == null) return null;
            if (!itemInstances.TryGetValue(key, out var models))
            {
                models = new[] { Instance(entry.prefab, weaponPivot), offHandPivot != null ? Instance(entry.offHandPrefab, offHandPivot) : null };
                itemInstances[key] = models;
            }
            return models;
        }

        static GameObject Instance(GameObject prefab, Transform pivot)
        {
            if (prefab == null) return null;
            var instance = Instantiate(prefab, pivot, false);
            instance.name = prefab.name;
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = pivot.gameObject.layer;
            return instance;
        }
    }
}
