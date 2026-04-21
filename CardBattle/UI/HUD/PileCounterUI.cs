using System.Collections;
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Displays deck / graveyard counts; graveyard display can lag real counts until VFX arrival.
    /// </summary>
    public class PileCounterUI : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private DeckController deckController;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI deckCountText;
        [SerializeField] private TextMeshProUGUI graveyardCountText;
        [SerializeField] private RectTransform deckAnchor;
        [SerializeField] private RectTransform graveyardAnchor;

        [Header("Display")]
        [SerializeField] private bool initializeDisplayFromRealOnStart = true;
        [SerializeField] private bool punchGraveyardLabelOnGhostArrival = true;
        [SerializeField] private float punchScale = 1.12f;
        [SerializeField] private float punchDuration = 0.12f;

        private int realDeckCount;
        private int displayedDeckCount;
        private int realGraveyardCount;
        private int displayedGraveyardCount;
        private bool reshufflePresentationActive;
        private Vector3 graveyardLabelBaseScale = Vector3.one;
        private Coroutine punchRoutine;

        public RectTransform DeckAnchor => deckAnchor;
        public RectTransform GraveyardAnchor => graveyardAnchor;

        private void Start()
        {
            if (graveyardCountText != null)
                graveyardLabelBaseScale = graveyardCountText.rectTransform.localScale;

            SyncFromDeckControllerInitialize();
        }

        private void OnEnable()
        {
            if (deckController != null)
                deckController.OnPilesChanged += OnPilesChanged;
        }

        private void OnDisable()
        {
            if (deckController != null)
                deckController.OnPilesChanged -= OnPilesChanged;
        }

        private void SyncFromDeckControllerInitialize()
        {
            if (deckController == null)
                return;

            realDeckCount = deckController.Deck.Count;
            realGraveyardCount = deckController.Graveyard.Count;
            displayedDeckCount = realDeckCount;
            displayedGraveyardCount = initializeDisplayFromRealOnStart
                ? realGraveyardCount
                : 0;

            RefreshDeckText();
            RefreshGraveyardText();
        }

        private void OnPilesChanged()
        {
            if (deckController == null)
                return;

            realDeckCount = deckController.Deck.Count;
            realGraveyardCount = deckController.Graveyard.Count;
            if (!reshufflePresentationActive)
            {
                displayedDeckCount = realDeckCount;
                if (displayedGraveyardCount > realGraveyardCount)
                    displayedGraveyardCount = realGraveyardCount;
            }

            RefreshDeckText();
            RefreshGraveyardText();
        }

        [ContextMenu("Refresh UI")]
        public void RefreshUI()
        {
            OnPilesChanged();
        }

        /// <summary>After a single ghost reaches the graveyard anchor, bumps display by 1 (clamped to real).</summary>
        public void OnSingleGhostArrived()
        {
            displayedGraveyardCount = Mathf.Min(displayedGraveyardCount + 1, realGraveyardCount);
            RefreshGraveyardText();
            TriggerGraveyardPunch();
        }

        /// <summary>After a discard batch finishes flying, bumps display by the whole batch (clamped).</summary>
        public void OnBatchGhostArrived(int amount)
        {
            if (amount <= 0)
                return;

            displayedGraveyardCount = Mathf.Min(displayedGraveyardCount + amount, realGraveyardCount);
            RefreshGraveyardText();
            TriggerGraveyardPunch();
        }

        public void ForceSyncDisplayedToReal()
        {
            if (deckController != null)
            {
                realDeckCount = deckController.Deck.Count;
                realGraveyardCount = deckController.Graveyard.Count;
            }
            displayedDeckCount = realDeckCount;
            displayedGraveyardCount = realGraveyardCount;
            reshufflePresentationActive = false;
            RefreshGraveyardText();
            RefreshDeckText();
        }

        private void RefreshDeckText()
        {
            if (deckCountText != null)
                deckCountText.text = displayedDeckCount.ToString();
        }

        private void RefreshGraveyardText()
        {
            if (graveyardCountText != null)
                graveyardCountText.text = displayedGraveyardCount.ToString();
        }

        /// <summary>Locks deck/graveyard display into reshuffle presentation mode.</summary>
        public void BeginReshufflePresentation(int transferCount)
        {
            if (transferCount <= 0)
                return;

            if (deckController != null)
            {
                realDeckCount = deckController.Deck.Count;
                realGraveyardCount = deckController.Graveyard.Count;
            }

            displayedDeckCount = realDeckCount;
            displayedGraveyardCount = Mathf.Min(displayedGraveyardCount, realGraveyardCount);
            reshufflePresentationActive = true;
            RefreshDeckText();
            RefreshGraveyardText();
        }

        /// <summary>Advances display counts as reshuffle ghosts arrive at the deck.</summary>
        public void OnReshuffleGhostArrived(int amount)
        {
            if (amount <= 0)
                return;

            if (!reshufflePresentationActive)
                BeginReshufflePresentation(amount);

            displayedDeckCount += amount;
            displayedGraveyardCount = Mathf.Max(0, displayedGraveyardCount - amount);
            RefreshDeckText();
            RefreshGraveyardText();
        }

        /// <summary>Ends reshuffle presentation mode and snaps display to real piles.</summary>
        public void CompleteReshufflePresentation()
        {
            reshufflePresentationActive = false;
        }

        private void TriggerGraveyardPunch()
        {
            if (!punchGraveyardLabelOnGhostArrival || graveyardCountText == null)
                return;

            if (punchRoutine != null)
                StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(CoPunchGraveyardLabel());
        }

        private IEnumerator CoPunchGraveyardLabel()
        {
            var rt = graveyardCountText.rectTransform;
            float half = punchDuration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                float s = Mathf.Lerp(1f, punchScale, u);
                rt.localScale = graveyardLabelBaseScale * s;
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                float s = Mathf.Lerp(punchScale, 1f, u);
                rt.localScale = graveyardLabelBaseScale * s;
                yield return null;
            }

            rt.localScale = graveyardLabelBaseScale;
            punchRoutine = null;
        }
    }
}
