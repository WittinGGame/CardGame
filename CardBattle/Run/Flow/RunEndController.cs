using System;
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class RunEndController : MonoBehaviour
    {
        [Header("Battle Presentation")]
        [SerializeField] private BattleEndPresentationController battleEndPresentationController;

        [Header("Panels")]
        [SerializeField] private GameObject runCompletePanel;
        [SerializeField] private GameObject runFailedPanel;
        [SerializeField] private TextMeshProUGUI runCompleteSummaryText;
        [SerializeField] private TextMeshProUGUI runFailedSummaryText;

        [Header("Options")]
        [SerializeField] private bool clearRunOnRunEnd;
        [SerializeField] private bool hidePanelsOnStart = true;
        [SerializeField] private bool verboseLogs;

        public bool IsRunEnded { get; private set; }
        public RunEndType LastRunEndType { get; private set; } = RunEndType.None;
        public string LastRunEndReason { get; private set; } = string.Empty;
        public int RunCompletedCount { get; private set; }
        public int RunFailedCount { get; private set; }

        public event Action OnRunCompleted;
        public event Action OnRunFailed;
        public event Action<RunEndType> OnRunEnded;

        private BattleEndPresentationController subscribedPresentationController;

        private void Start()
        {
            if (hidePanelsOnStart)
            {
                SetPanelActive(runCompletePanel, false);
                SetPanelActive(runFailedPanel, false);
            }
        }

        private void OnEnable()
        {
            SubscribePresentationController();
        }

        private void OnDisable()
        {
            UnsubscribePresentationController();
        }

        private void OnDestroy()
        {
            UnsubscribePresentationController();
        }

        public bool TryCompleteRun(string sourceNodeId)
        {
            if (IsRunEnded)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[RunEnd] Cannot complete run: run already ended as {LastRunEndType}.");
                }

                return false;
            }

            IsRunEnded = true;
            LastRunEndType = RunEndType.Completed;
            LastRunEndReason = sourceNodeId ?? string.Empty;
            RunCompletedCount++;

            SetPanelActive(runFailedPanel, false);
            SetPanelActive(runCompletePanel, true);

            if (runCompleteSummaryText != null)
            {
                runCompleteSummaryText.text =
                    string.IsNullOrWhiteSpace(sourceNodeId)
                        ? "Act Cleared!"
                        : $"Act Cleared!\nNode: {sourceNodeId}";
            }

            TryClearActiveRun();

            OnRunCompleted?.Invoke();
            OnRunEnded?.Invoke(RunEndType.Completed);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RunEnd] Run completed by Boss node: {sourceNodeId}");
            }

            return true;
        }

        public bool TryFailRun(string reason)
        {
            if (IsRunEnded)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        $"[RunEnd] Cannot fail run: run already ended as {LastRunEndType}.");
                }

                return false;
            }

            IsRunEnded = true;
            LastRunEndType = RunEndType.Failed;
            LastRunEndReason = reason ?? string.Empty;
            RunFailedCount++;

            SetPanelActive(runCompletePanel, false);
            SetPanelActive(runFailedPanel, true);

            if (runFailedSummaryText != null)
            {
                runFailedSummaryText.text =
                    string.IsNullOrWhiteSpace(reason)
                        ? "Run Failed."
                        : $"Run Failed.\nReason: {reason}";
            }

            TryClearActiveRun();

            OnRunFailed?.Invoke();
            OnRunEnded?.Invoke(RunEndType.Failed);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RunEnd] Run failed. Reason={reason}");
            }

            return true;
        }

        public void ResetRunEndStateForNewRun()
        {
            IsRunEnded = false;
            LastRunEndType = RunEndType.None;
            LastRunEndReason = string.Empty;

            SetPanelActive(runCompletePanel, false);
            SetPanelActive(runFailedPanel, false);

            if (verboseLogs)
                Debug.Log("[RunEnd] Run end state reset for new run.");
        }

        private void HandleBattleEndPresentationReady(BattleOutcome outcome)
        {
            if (outcome != BattleOutcome.PlayerDefeated)
                return;

            TryFailRun("PlayerDefeated");
        }

        private void TryClearActiveRun()
        {
            if (!clearRunOnRunEnd)
                return;

            RunManager runManager = RunManager.Instance;
            if (runManager == null)
            {
                if (verboseLogs)
                {
                    Debug.LogWarning(
                        "[RunEnd] clearRunOnRunEnd is enabled, but RunManager.Instance is null.");
                }

                return;
            }

            bool cleared = runManager.ClearRun();

            if (verboseLogs)
            {
                Debug.Log(
                    $"[RunEnd] Active run clear requested. Cleared={cleared} | HasActiveRun={runManager.HasActiveRun}");
            }
        }

        private void SubscribePresentationController()
        {
            UnsubscribePresentationController();

            if (battleEndPresentationController == null)
                return;

            subscribedPresentationController = battleEndPresentationController;
            subscribedPresentationController.OnBattleEndPresentationReady +=
                HandleBattleEndPresentationReady;
        }

        private void UnsubscribePresentationController()
        {
            if (subscribedPresentationController == null)
                return;

            subscribedPresentationController.OnBattleEndPresentationReady -=
                HandleBattleEndPresentationReady;
            subscribedPresentationController = null;
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}
