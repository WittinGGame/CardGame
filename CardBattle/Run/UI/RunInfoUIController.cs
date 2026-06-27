using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class RunInfoUIController : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private RunManager runManager;

        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI deckCountText;
        [SerializeField] private Button deckButton;

        [Header("Deck Viewer")]
        [SerializeField] private RunDeckViewerUI deckViewer;

        [Header("Options")]
        [SerializeField] private bool hideWhenNoActiveRun = true;
        [SerializeField] private bool verboseLogs;

        private RunManager subscribedRunManager;

        private void Awake()
        {
            if (deckButton != null)
            {
                deckButton.onClick.RemoveListener(HandleDeckButtonClicked);
                deckButton.onClick.AddListener(HandleDeckButtonClicked);
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            SubscribeRunManager();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeRunManager();
        }

        private void OnDestroy()
        {
            UnsubscribeRunManager();
        }

        public void Refresh()
        {
            RunManager manager = ResolveRunManager();
            bool hasActiveRun = manager != null && manager.HasActiveRun;

            if (!hasActiveRun)
            {
                if (hideWhenNoActiveRun && root != null)
                    root.SetActive(false);
                else
                {
                    SetPlaceholderValues();
                    if (root != null)
                        root.SetActive(true);
                }

                if (deckViewer != null)
                    deckViewer.Hide();

                if (verboseLogs)
                    Debug.Log("[RunInfoUI] Refreshed. No active run.");

                return;
            }

            RunState run = manager.CurrentRun;
            int deckCount = run?.currentDeck != null ? run.currentDeck.Count : 0;

            if (root != null)
                root.SetActive(true);

            if (goldText != null)
                goldText.text = run != null ? run.gold.ToString() : "0";

            if (deckCountText != null)
                deckCountText.text = deckCount.ToString();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RunInfoUI] Refreshed. Gold={run?.gold ?? 0} | Deck={deckCount}");
            }
        }

        public void Show()
        {
            if (root != null)
                root.SetActive(true);

            Refresh();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);

            if (deckViewer != null)
                deckViewer.Hide();
        }

        private void HandleDeckButtonClicked()
        {
            if (deckViewer == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[RunInfoUI] Deck viewer reference is missing.");

                return;
            }

            deckViewer.ShowFromCurrentRun();
        }

        private void HandleRunStarted(RunState _)
        {
            Refresh();
        }

        private void HandleRunChanged(RunState _)
        {
            Refresh();
        }

        private void HandleRunCleared()
        {
            Refresh();
        }

        private void SetPlaceholderValues()
        {
            if (goldText != null)
                goldText.text = "-";

            if (deckCountText != null)
                deckCountText.text = "-";
        }

        private void SubscribeRunManager()
        {
            UnsubscribeRunManager();

            RunManager manager = ResolveRunManager();
            if (manager == null)
                return;

            subscribedRunManager = manager;
            subscribedRunManager.OnRunStarted += HandleRunStarted;
            subscribedRunManager.OnRunChanged += HandleRunChanged;
            subscribedRunManager.OnRunCleared += HandleRunCleared;
        }

        private void UnsubscribeRunManager()
        {
            if (subscribedRunManager == null)
                return;

            subscribedRunManager.OnRunStarted -= HandleRunStarted;
            subscribedRunManager.OnRunChanged -= HandleRunChanged;
            subscribedRunManager.OnRunCleared -= HandleRunCleared;
            subscribedRunManager = null;
        }

        private RunManager ResolveRunManager()
        {
            if (runManager != null)
                return runManager;

            return RunManager.Instance;
        }
    }
}
