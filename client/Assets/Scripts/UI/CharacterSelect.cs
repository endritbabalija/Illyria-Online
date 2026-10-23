using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Wuxia.Data;
using Wuxia.Net;
using Wuxia.World;

namespace Wuxia.UI
{
    /// <summary>
    /// The character select (M4): after logging in, one card per character in combat.json, showing
    /// the hero this account made of it or a name field to create one. Play (or Create) hands the
    /// choice to <see cref="NetZone"/>, which creates the character if needed and joins the zone; the
    /// zone's Welcome hides the screen, a refusal (a taken or bad name, a join that failed) shows here.
    /// Opened again from the Skills panel while playing, it can be closed to keep playing.
    /// Full-screen and opaque to touches, so the joystick and camera never see them.
    /// </summary>
    public class CharacterSelect : MonoBehaviour
    {
        // The server's rule (server/src/character.ts cleanCharacterName), checked here first for a quick answer.
        const int NameMin = 2, NameMax = 20;

        [SerializeField] Text subtitleText;
        [SerializeField] Text statusText;
        [SerializeField] Button closeButton;
        [Tooltip("One card per character, in combat.json order.")]
        [SerializeField] CharacterCard[] cards;
        [SerializeField] ItemModelRegistry icons;
        [SerializeField] Color errorColor = new Color(1f, 0.45f, 0.4f);
        [SerializeField] Color infoColor = new Color(1f, 1f, 1f, 0.8f);

        Func<string, string, Task> choose;
        string current;
        bool busy;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                card.PlayButton.onClick.AddListener(() => Pick(card));
            }
        }

        /// <param name="heroes">This account's characters (character_list).</param>
        /// <param name="currentCharacter">The character being played, if <paramref name="canClose"/>.</param>
        /// <param name="onChoose">(character id, new name or null): creates and joins; throws to refuse.</param>
        public void Show(NetCharacterSummary[] heroes, string currentCharacter, bool canClose, Func<string, string, Task> onChoose)
        {
            choose = onChoose;
            current = canClose ? currentCharacter : null;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            closeButton.gameObject.SetActive(canClose);
            subtitleText.text = canClose ? $"Online  ·  {ZoneSession.Instance.ZoneName}  ·  switching leaves this channel" : $"Online  ·  {ZoneSession.Instance.ZoneName}";
            SetStatus("", false);
            busy = false;
            Refresh(heroes);
        }

        /// <summary>Rebinds the cards to a fresh character list (after a create or a reconnect).</summary>
        public void Refresh(NetCharacterSummary[] heroes)
        {
            var characters = GameData.Combat.characters;
            for (var i = 0; i < cards.Length; i++)
            {
                var shown = i < characters.Length;
                cards[i].gameObject.SetActive(shown);
                if (!shown) continue;
                var hero = Array.Find(heroes ?? new NetCharacterSummary[0], h => h.characterId == characters[i].id);
                cards[i].Bind(characters[i], hero, characters[i].id == current, icons);
                cards[i].SetInteractable(!busy);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetStatus(string text, bool error)
        {
            statusText.text = text;
            statusText.color = error ? errorColor : infoColor;
        }

        /// <summary>Presses a card's button, for scripts and MCP: Play, or Create with this name.</summary>
        public void Pick(string characterId, string newName = null)
        {
            foreach (var card in cards)
                if (card.Character != null && card.Character.id == characterId) Pick(card, newName);
        }

        void Pick(CharacterCard card, string typed = null)
        {
            if (busy || choose == null) return;
            string newName = null;
            if (card.Hero == null)
            {
                newName = CleanName(typed ?? card.EnteredName);
                if (newName == null)
                {
                    SetStatus($"A name is {NameMin}–{NameMax} letters, digits, spaces, ' or -", true);
                    return;
                }
            }
            _ = Run(card, newName);
        }

        async Task Run(CharacterCard card, string newName)
        {
            busy = true;
            SetCardsInteractable(false);
            SetStatus(newName != null ? $"Creating {newName}…" : $"Joining {ZoneSession.Instance.ZoneName} as {card.Hero.name}…", false);
            try
            {
                await choose(card.Character.id, newName);
                SetStatus("", false);
            }
            catch (Exception e)
            {
                Debug.Log($"[Net] Character select: {e.Message}");
                SetStatus(Sentence(e.Message), true);
            }
            finally
            {
                busy = false;
                SetCardsInteractable(true);
            }
        }

        void SetCardsInteractable(bool on)
        {
            foreach (var card in cards) card.SetInteractable(on);
            closeButton.interactable = on;
        }

        /// <summary>Trims, folds runs of spaces and checks the characters; null if the server would refuse it.</summary>
        static string CleanName(string name)
        {
            if (name == null) return null;
            var trimmed = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
            if (trimmed.Length < NameMin || trimmed.Length > NameMax) return null;
            foreach (var c in trimmed)
                if (!(c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == ' ' || c == '\'' || c == '-')) return null;
            return trimmed;
        }

        static string Sentence(string message) =>
            string.IsNullOrEmpty(message) ? "Something went wrong" : char.ToUpperInvariant(message[0]) + message.Substring(1);
    }
}
