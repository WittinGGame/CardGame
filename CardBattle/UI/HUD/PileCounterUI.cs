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
        [SerializeField] private RectTransform graveyardAnchor;

        [Header("Display")]
        [SerializeField] private bool initializeDisplayFromRealOnStart = true;
        [SerializeField] private bool punchGraveyardLabelOnGhostArrival = true;
        [SerializeField] private float punchScale = 1.12f;
        [SerializeField] private float punchDuration = 0.12f;

        private int realGraveyardCount;
        private int displayedGraveyardCount;
        private Vector3 graveyardLabelBaseScale = Vector3.one;
        private Coroutine punchRoutine;

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

            realGraveyardCount = deckController.Graveyard.Count;
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

            realGraveyardCount = deckController.Graveyard.Count;
            if (displayedGraveyardCount > realGraveyardCount)
                displayedGraveyardCount = realGraveyardCount;

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
                realGraveyardCount = deckController.Graveyard.Count;
            displayedGraveyardCount = realGraveyardCount;
            RefreshGraveyardText();
        }

        private void RefreshDeckText()
        {
            if (deckCountText != null && deckController != null)
                deckCountText.text = deckController.Deck.Count.ToString();
        }

        private void RefreshGraveyardText()
        {
            if (graveyardCountText != null)
                graveyardCountText.text = displayedGraveyardCount.ToString();
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
