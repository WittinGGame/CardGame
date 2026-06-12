using System;
using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Delays the presentation-ready signal after battle logic has ended.
    /// Does not open UI, change battle state, or stop gameplay coroutines.
    /// </summary>
    public class BattleEndPresentationController : MonoBehaviour
    {
        [SerializeField] private BattleOutcomeController battleOutcomeController;

        [Header("Timing")]
        [SerializeField] private float encounterClearedDelay = 1.0f;
        [SerializeField] private float playerDefeatedDelay = 1.2f;

        private Coroutine presentationRoutine;

        public bool IsPresentationPending { get; private set; }
        public bool IsPresentationReady { get; private set; }
        public BattleOutcome ReadyOutcome { get; private set; } = BattleOutcome.None;

        public event Action<BattleOutcome> OnBattleEndPresentationReady;

        private void OnEnable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void OnDisable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded -= HandleBattleEnded;

            StopPresentationRoutine();
            IsPresentationPending = false;
        }

        public void ResetPresentation()
        {
            StopPresentationRoutine();
            IsPresentationPending = false;
            IsPresentationReady = false;
            ReadyOutcome = BattleOutcome.None;
            Debug.Log("[BattleEndPresentation] Reset.");
        }

        [ContextMenu("Debug Reset Presentation")]
        private void DebugResetPresentation()
        {
            ResetPresentation();
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.None || IsPresentationPending || IsPresentationReady)
                return;

            IsPresentationPending = true;
            presentationRoutine = StartCoroutine(PresentationDelayRoutine(outcome));
        }

        private IEnumerator PresentationDelayRoutine(BattleOutcome outcome)
        {
            float delay = GetDelay(outcome);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            presentationRoutine = null;
            IsPresentationPending = false;
            IsPresentationReady = true;
            ReadyOutcome = outcome;

            Debug.Log($"[BattleEndPresentation] Ready: {outcome}");
            OnBattleEndPresentationReady?.Invoke(outcome);
        }

        private float GetDelay(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.EncounterCleared:
                    return Mathf.Max(0f, encounterClearedDelay);
                case BattleOutcome.PlayerDefeated:
                    return Mathf.Max(0f, playerDefeatedDelay);
                default:
                    return 0f;
            }
        }

        private void StopPresentationRoutine()
        {
            if (presentationRoutine == null)
                return;

            StopCoroutine(presentationRoutine);
            presentationRoutine = null;
        }
    }
}
