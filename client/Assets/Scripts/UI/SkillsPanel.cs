using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.Net;
using Wuxia.Player;

namespace Wuxia.UI
{
    /// <summary>
    /// Learned skill books, their levels and forms, and spending skill points on them. Also holds the
    /// character button: online it opens the character select, offline (a test helper) it plays the
    /// other character with a fresh starter kit.
    /// </summary>
    public class SkillsPanel : MonoBehaviour
    {
        [SerializeField] PlayerSkills skills;
        [SerializeField] PlayerCombat combat;
        [SerializeField] Text characterText;
        [SerializeField] Text pointsText;
        [Tooltip("Disabled row used as the template for every book.")]
        [SerializeField] SkillBookRow rowTemplate;
        [SerializeField] Button closeButton;
        [SerializeField] Button switchCharacterButton;

        readonly List<SkillBookRow> rows = new List<SkillBookRow>();
        Text switchLabel;
        int shownVersion = -1;
        string shownCharacter;

        void Awake()
        {
            rowTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(Close);
            switchCharacterButton.onClick.AddListener(SwitchCharacter);
            switchLabel = switchCharacterButton.GetComponentInChildren<Text>();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            shownVersion = -1;
        }

        public void Close() => gameObject.SetActive(false);

        public void Toggle()
        {
            if (gameObject.activeSelf) Close();
            else Open();
        }

        void Update()
        {
            var state = skills.State;
            if (switchLabel != null) switchLabel.text = NetZone.Online ? "Change character" : "Play the other character";
            if (combat.Character.id != shownCharacter) Rebuild();
            if (state.Version == shownVersion) return;
            shownVersion = state.Version;

            pointsText.text = $"Skill points: {state.SkillPoints}      Next point: {state.MeterKills} / {state.KillsPerPoint} kills";
            var maxLevel = GameData.Skills.MaxBookLevel;
            foreach (var row in rows) row.Refresh(state, maxLevel);
        }

        void Rebuild()
        {
            shownCharacter = combat.Character.id;
            shownVersion = -1;
            characterText.text = combat.Character.name;
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();

            foreach (var book in GameData.Skills.books)
            {
                if (!skills.State.Knows(book.id)) continue;
                var row = Instantiate(rowTemplate, rowTemplate.transform.parent);
                row.name = book.name;
                row.gameObject.SetActive(true);
                row.Bind(book);
                var id = book.id;
                row.LevelUpButton.onClick.AddListener(() => skills.LevelUpBook(id));
                rows.Add(row);
            }
        }

        void SwitchCharacter()
        {
            if (NetZone.Online)
            {
                Close();
                NetZone.Instance.OpenCharacterSelect();
                return;
            }
            var characters = GameData.Combat.characters;
            var index = System.Array.FindIndex(characters, c => c.id == combat.Character.id);
            combat.SetCharacter(characters[(index + 1) % characters.Length].id);
        }
    }
}
