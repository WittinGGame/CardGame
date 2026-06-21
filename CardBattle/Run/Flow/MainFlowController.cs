using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class MainFlowController : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private RunManager runManager;

        [Header("Gameplay")]
        [SerializeField] private MapRuntimeController mapRuntimeController;
        [SerializeField] private TreeMapUIController treeMapUIController;
        [SerializeField] private RunEndController runEndController;
        [SerializeField] private EncounterFlowResetController encounterFlowResetController;

        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI selectedClassText;

        [Header("Starter Run Settings")]
        [SerializeField] private string defaultClassId = "knight";
        [SerializeField] private int defaultMaxHp = 60;
        [SerializeField] private int defaultSeed = 12345;
        [SerializeField] private List<string> knightStarterDeckIds = new List<string>
        {
            "strike",
            "strike",
            "block",
            "strike",
            "AllStrike"
        };

        [Header("Options")]
        [SerializeField] private bool hideTreeMapOnStart = true;
        [SerializeField] private bool verboseLogs = true;

        public string SelectedClassId { get; private set; } = "knight";

        private RunEndController subscribedRunEndController;

        private void Awake()
        {
            if (hideTreeMapOnStart && treeMapUIController != null)
                treeMapUIController.Hide();

            SetPanelActive(gameplayRoot, false);
            SetPanelActive(characterSelectPanel, false);
        }

        private void Start()
        {
            SelectKnight();
            SetContinueButtonEnabled(false);
            ShowMainMenu();
        }

        private void OnEnable()
        {
            SubscribeRunEndController();
        }

        private void OnDisable()
        {
            UnsubscribeRunEndController();
        }

        private void OnDestroy()
        {
            UnsubscribeRunEndController();
        }

        public void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(characterSelectPanel, false);
            SetPanelActive(gameplayRoot, false);

            if (treeMapUIController != null)
                treeMapUIController.Hide();

            SetContinueButtonEnabled(false);

            if (verboseLogs)
                Debug.Log("[MainFlow] Showing Main Menu.");
        }

        public void ShowCharacterSelect()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(characterSelectPanel, true);
            SetPanelActive(gameplayRoot, false);

            if (treeMapUIController != null)
                treeMapUIController.Hide();

            if (verboseLogs)
                Debug.Log("[MainFlow] Showing Character Select.");
        }

        public void SelectKnight()
        {
            SelectedClassId = defaultClassId;

            if (selectedClassText != null)
                selectedClassText.text = SelectedClassId;

            if (verboseLogs)
                Debug.Log($"[MainFlow] Selected class: {SelectedClassId}");
        }

        public void StartSelectedRun()
        {
            RunManager manager = ResolveRunManager();
            if (manager == null)
            {
                Debug.LogError("[MainFlow] Cannot start run: RunManager is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedClassId))
                SelectKnight();

            List<RunCardRecord> starterDeckRecords = BuildStarterDeckRecords();
            if (starterDeckRecords.Count == 0)
            {
                Debug.LogError("[MainFlow] Cannot start run: starter deck is empty.");
                return;
            }

            string runId = $"run_{DateTime.UtcNow.Ticks}";
            int seed = defaultSeed != 0
                ? defaultSeed
                : UnityEngine.Random.Range(1, int.MaxValue);

            bool started = manager.StartNewRun(
                runId,
                seed,
                SelectedClassId,
                defaultMaxHp,
                starterDeckRecords);

            if (!started)
            {
                Debug.LogError("[MainFlow] RunManager.StartNewRun failed.");
                return;
            }

            ResetMainFlowForNewRun();

            if (mapRuntimeController != null)
                mapRuntimeController.InitializeMap();

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(characterSelectPanel, false);
            SetPanelActive(gameplayRoot, true);

            if (treeMapUIController != null)
            {
                treeMapUIController.Show();
                treeMapUIController.Rebuild();
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[MainFlow] Started new run. Class={SelectedClassId} | " +
                    $"HP={defaultMaxHp} | Deck={starterDeckRecords.Count}");
                Debug.Log("[MainFlow] Showing Tree Map.");
            }
        }

        public void BackToMainMenu()
        {
            SetPanelActive(gameplayRoot, false);

            if (treeMapUIController != null)
                treeMapUIController.Hide();

            SetPanelActive(characterSelectPanel, false);

            if (runEndController != null)
                runEndController.ResetRunEndStateForNewRun();

            ShowMainMenu();

            if (verboseLogs)
                Debug.Log("[MainFlow] Returned to Main Menu.");
        }

        public void ResetMainFlowForNewRun()
        {
            if (runEndController != null)
                runEndController.ResetRunEndStateForNewRun();

            if (encounterFlowResetController != null)
                encounterFlowResetController.TryPrepareNextEncounterState();
        }

        public void OnClickNewRun()
        {
            ShowCharacterSelect();
        }

        public void OnClickBackFromCharacterSelect()
        {
            ShowMainMenu();
        }

        public void OnClickSelectKnight()
        {
            SelectKnight();
        }

        public void OnClickStartRun()
        {
            StartSelectedRun();
        }

        public void OnClickBackToMain()
        {
            BackToMainMenu();
        }

        private List<RunCardRecord> BuildStarterDeckRecords()
        {
            var records = new List<RunCardRecord>();

            if (knightStarterDeckIds == null || knightStarterDeckIds.Count == 0)
                return records;

            for (int i = 0; i < knightStarterDeckIds.Count; i++)
            {
                string cardId = knightStarterDeckIds[i];
                if (string.IsNullOrWhiteSpace(cardId))
                    continue;

                records.Add(new RunCardRecord(cardId));
            }

            return records;
        }

        private RunManager ResolveRunManager()
        {
            if (runManager != null)
                return runManager;

            return RunManager.Instance;
        }

        private void HandleBackToMainRequested()
        {
            BackToMainMenu();
        }

        private void SubscribeRunEndController()
        {
            UnsubscribeRunEndController();

            if (runEndController == null)
                return;

            subscribedRunEndController = runEndController;
            subscribedRunEndController.OnBackToMainRequested += HandleBackToMainRequested;
        }

        private void UnsubscribeRunEndController()
        {
            if (subscribedRunEndController == null)
                return;

            subscribedRunEndController.OnBackToMainRequested -= HandleBackToMainRequested;
            subscribedRunEndController = null;
        }

        private void SetContinueButtonEnabled(bool enabled)
        {
            if (continueButton != null)
                continueButton.interactable = enabled;
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}
