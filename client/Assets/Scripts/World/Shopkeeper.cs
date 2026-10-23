using UnityEngine;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.Player;
using Wuxia.UI;

namespace Wuxia.World
{
    /// <summary>
    /// An NPC that runs a shop from items.json. Tapping it opens the shop when you stand close, or
    /// runs you over first. Walking away (or dying) ends the shopping session. Not a Combatant, so it
    /// can't be targeted, and it sits on the Npc layer, which the NavMesh bake leaves out.
    /// </summary>
    public class Shopkeeper : MonoBehaviour
    {
        [Tooltip("Shop id from items.json.")]
        [SerializeField] string shopId = "han_keeper";
        [SerializeField] ShopPanel panel;
        [SerializeField] PlayerCombat player;
        [Tooltip("Tapping within this distance (ground plane) opens the shop at once.")]
        [SerializeField] float interactRange = 3.5f;
        [Tooltip("Walking farther than this ends the shopping session.")]
        [SerializeField] float leaveRange = 7f;
        [Tooltip("Optional HUD label that floats over the NPC; its text is set from the shop's name.")]
        [SerializeField] Text nameLabel;

        public string ShopId => shopId;
        public ShopData Shop { get; private set; }

        void Start()
        {
            Shop = GameData.Items.GetShop(shopId);
            if (nameLabel != null) nameLabel.text = $"{Shop.name}\n<size=20>Shop · tap to trade</size>";
        }

        public void Interact(PlayerCombat who)
        {
            if (who.IsDead) return;
            if (FlatDistance(who.transform.position) <= interactRange) panel.Open(this);
            else who.Mover.RunTo(transform.position, interactRange * 0.7f, () => panel.Open(this));
        }

        void Update()
        {
            if (panel.Keeper != this) return;
            if (player.IsDead || FlatDistance(player.transform.position) > leaveRange) panel.EndSession();
        }

        float FlatDistance(Vector3 p)
        {
            var d = p - transform.position;
            d.y = 0f;
            return d.magnitude;
        }
    }
}
