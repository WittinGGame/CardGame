using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Displays deck / hand / graveyard counts; listens to DeckController.OnPilesChanged.
    /// </summary>
    public class PileCounterUI : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private DeckController deckController;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI deckCountText;
        [SerializeField] private TextMeshProUGUI graveyardCountText;

        private void Start()
        {
            RefreshUI();
        }

        private void OnEnable()
        {
            if (deckController != null)
                deckController.OnPilesChanged += RefreshUI;
        }

        private void OnDisable()
        {
            if (deckController != null)
                deckController.OnPilesChanged -= RefreshUI;
        }

        [ContextMenu("Refresh UI")]
        public void RefreshUI()
        {
            if (deckController == null)
                return;

            if (deckCountText != null)
                deckCountText.text = deckController.Deck.Count.ToString();

            if (graveyardCountText != null)
                graveyardCountText.text = deckController.Graveyard.Count.ToString();
        }
    }
}