using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.UI
{
    /// <summary>One learned book in the skills panel: name and level, its forms, and a level-up button.</summary>
    public class SkillBookRow : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] Text forms;
        [SerializeField] Button levelUpButton;
        [SerializeField] Text levelUpLabel;

        public Button LevelUpButton => levelUpButton;
        public BookData Book { get; private set; }

        readonly StringBuilder builder = new StringBuilder();

        public void Bind(BookData book) => Book = book;

        public void Refresh(SkillState state, int maxLevel)
        {
            var level = state.BookLevel(Book.id);
            var hasForms = Book.forms.Length > 0;
            title.text = hasForms ? $"{Book.name}   <color=#c8b070>Lv {level} / {maxLevel}</color>" : Book.name;

            builder.Clear();
            if (!hasForms) builder.Append("Leap onto marked ledges, walls and tower roofs");
            foreach (var form in Book.forms)
            {
                if (builder.Length > 0) builder.Append("   ·   ");
                if (state.IsUnlocked(Book, form)) builder.Append(form.name);
                else builder.Append("<color=#7a7a7a>").Append(form.name).Append(" (Lv ").Append(form.unlockLevel).Append(")</color>");
            }
            forms.text = builder.ToString();

            levelUpButton.gameObject.SetActive(hasForms);
            if (!hasForms) return;
            var block = state.CheckLevelUp(Book.id, out var cost);
            levelUpButton.interactable = block == LevelUpBlock.None;
            levelUpLabel.text = block == LevelUpBlock.MaxLevel ? "Max" : $"+1  ({cost} pt)";
        }
    }
}
