using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.Net;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>
    /// One character on the character select: the kind (The Kreshnik, The Zana's Chosen), its weapons
    /// and their icons, and either the hero this account made of it (name, gold, kills, where they
    /// were) with Play, or a name field with Create.
    /// </summary>
    public class CharacterCard : MonoBehaviour
    {
        [SerializeField] Text kindText;
        [SerializeField] Text weaponsText;
        [Tooltip("One per weapon of the character, showing its starter weapon's icon.")]
        [SerializeField] RawImage[] weaponIcons;
        [SerializeField] Text heroText;
        [SerializeField] Text detailsText;
        [SerializeField] InputField nameField;
        [SerializeField] Button playButton;
        [SerializeField] Text playLabel;

        public CharacterData Character { get; private set; }
        /// <summary>This account's hero of this kind, or null if there is none yet.</summary>
        public NetCharacterSummary Hero { get; private set; }
        public Button PlayButton => playButton;
        public string EnteredName => nameField != null ? nameField.text : "";

        public void Bind(CharacterData character, NetCharacterSummary hero, bool current, ItemModelRegistry icons)
        {
            Character = character;
            Hero = hero;
            kindText.text = character.name;

            var names = new string[character.weapons.Length];
            for (var i = 0; i < names.Length; i++) names[i] = GameData.Combat.GetWeapon(character.weapons[i]).name;
            weaponsText.text = string.Join("  ·  ", names);
            var kit = GameData.Items.GetStarterKit(character.id);
            for (var i = 0; i < weaponIcons.Length; i++)
                ItemIcons.Apply(weaponIcons[i], icons, i < character.weapons.Length ? StarterWeapon(kit, character.weapons[i]) : null);

            var exists = hero != null;
            heroText.gameObject.SetActive(exists);
            nameField.gameObject.SetActive(!exists);
            if (exists)
            {
                heroText.text = hero.name;
                var where = string.IsNullOrEmpty(hero.zone) ? "New to the valley" : "Last seen in " + ZoneTitle(hero.zone);
                detailsText.text = $"{hero.gold} gold  ·  {hero.kills} {(hero.kills == 1 ? "kill" : "kills")}\n{where}";
                playLabel.text = current ? "Continue" : "Play";
            }
            else
            {
                detailsText.text = "No hero yet.\nName one to begin.";
                playLabel.text = "Create";
            }
        }

        public void SetInteractable(bool on)
        {
            playButton.interactable = on;
            if (nameField != null) nameField.interactable = on;
        }

        static ItemDef StarterWeapon(StarterKitData kit, string weaponType)
        {
            foreach (var id in kit.equip)
            {
                var def = GameData.Items.FindItem(id);
                if (def != null && def.weaponType == weaponType) return def;
            }
            foreach (var stack in kit.bag)
            {
                var def = GameData.Items.FindItem(stack.item);
                if (def != null && def.weaponType == weaponType) return def;
            }
            return null;
        }

        /// <summary>"oak_valley" → "Oak Valley" (the client doesn't load zones.json).</summary>
        static string ZoneTitle(string zoneId) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(zoneId.Replace('_', ' '));
    }
}
