using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class RunDeckViewerUI : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private CardCatalog cardCatalog;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("List")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private RunDeckCardRowUI rowPrefab;

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI emptyText;

        [Header("Options")]
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private bool verboseLogs;

        private readonly List<RunDeckCardRowUI> spawnedRows = new List<RunDeckCardRowUI>();

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
        }

        private void Start()
        {
            if (hideOnStart)
                Hide();
        }

        public void ShowFromCurrentRun()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            Refresh();
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Refresh()
        {
            ClearRows();

            RunManager manager = ResolveRunManager();
            if (manager == null || !manager.HasActiveRun)
            {
                SetEmptyMessage("No active run.");
                return;
            }

            RunState run = manager.CurrentRun;
            if (run?.currentDeck == null || run.currentDeck.Count == 0)
            {
                SetEmptyMessage("Deck is empty.");
                return;
            }

            SetEmptyMessage(string.Empty);

            if (titleText != null)
                titleText.text = "Current Deck";

            for (int i = 0; i < run.currentDeck.Count; i++)
            {
                RunCardRecord record = run.currentDeck[i];
                if (record == null || rowPrefab == null || contentRoot == null)
                    continue;

                CardData cardData = null;
                if (cardCatalog != null && !string.IsNullOrWhiteSpace(record.cardId))
                    cardCatalog.TryGetCard(record.cardId, out cardData);

                RunDeckCardRowUI row = Instantiate(rowPrefab, contentRoot);
                row.Bind(i, record, cardData);
                spawnedRows.Add(row);
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RunDeckViewer] Built deck list. Count={run.currentDeck.Count}");
            }
        }

        public void ClearRows()
        {
            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i] != null)
                    Destroy(spawnedRows[i].gameObject);
            }

            spawnedRows.Clear();
        }

        private void SetEmptyMessage(string message)
        {
            if (emptyText == null)
                return;

            bool hasMessage = !string.IsNullOrEmpty(message);
            emptyText.gameObject.SetActive(hasMessage);
            emptyText.text = message;
        }

        private RunManager ResolveRunManager()
        {
            if (runManager != null)
                return runManager;

            return RunManager.Instance;
        }
    }
}
