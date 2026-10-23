using UnityEngine;

namespace Wuxia.World
{
    /// <summary>
    /// One pooled pile on the ground (LootDrop.prefab): a light beam and a glow disc tinted in the
    /// rarity colour, and a slot the item's model is parented to. Driven by <see cref="GroundLoot"/>.
    /// </summary>
    public class GroundDropView : MonoBehaviour
    {
        [SerializeField] Renderer beam;
        [SerializeField] Renderer disc;
        [Tooltip("Parent of the item or gold model.")]
        [SerializeField] Transform modelSlot;

        public Renderer Beam => beam;
        public Renderer Disc => disc;
        public Transform ModelSlot => modelSlot;
    }
}
