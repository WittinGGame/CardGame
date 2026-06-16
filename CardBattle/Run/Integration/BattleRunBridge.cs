using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Transfers persistent run data into battle-scene systems.
    /// </summary>
    public class BattleRunBridge : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CardCatalog cardCatalog;

        [Header("Battle Scene")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private PlayerBattleUnit player;

        [Header("Options")]
        [SerializeField] private bool verboseLogs;

        public bool HasActiveRun =>
            RunManager.Instance != null &&
            RunManager.Instance.HasActiveRun;

        public int LastResolvedCardCount { get; private set; }
        public int LastMissingCardCount { get; private set; }

        public bool LastPlayerVitalsApplied { get; private set; }
        public int LastAppliedCurrentHp { get; private set; }
        public int LastAppliedMaxHp { get; private set; }

        public bool TryInitializeBattleFromActiveRun()
        {
            ResetAllDiagnostics();

            if (!TryGetValidActiveRun(out RunState run))
                return false;

            if (!TryResolveDeckFromRun(run, out List<CardData> resolvedCards))
                return false;

            if (!TryValidatePlayerVitalsFromRun(run, out int maxHp, out int currentHp))
                return false;

            deckController.BuildFromCardDataList(resolvedCards);
            player.InitializeVitals(maxHp, currentHp);

            LastPlayerVitalsApplied = true;
            LastAppliedMaxHp = player.MaxHp;
            LastAppliedCurrentHp = player.CurrentHp;

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleRunBridge] Built battle deck from active run. " +
                    $"Class={run.playerClassId} | Records={run.currentDeck.Count} | " +
                    $"Resolved={LastResolvedCardCount} | Missing={LastMissingCardCount}");
                Debug.Log(
                    $"[BattleRunBridge] Applied player vitals from active run. " +
                    $"HP={LastAppliedCurrentHp}/{LastAppliedMaxHp}");
            }

            return true;
        }

        public bool TryBuildDeckFromActiveRun()
        {
            ResetDeckDiagnostics();

            if (!TryGetValidActiveRun(out RunState run))
                return false;

            if (!TryResolveDeckFromRun(run, out List<CardData> resolvedCards))
                return false;

            deckController.BuildFromCardDataList(resolvedCards);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleRunBridge] Built battle deck from active run. " +
                    $"Class={run.playerClassId} | Records={run.currentDeck.Count} | " +
                    $"Resolved={LastResolvedCardCount} | Missing={LastMissingCardCount}");
            }

            return true;
        }

        public bool TryApplyPlayerVitalsFromActiveRun()
        {
            ResetVitalsDiagnostics();

            if (!TryGetValidActiveRun(out RunState run))
                return false;

            if (!TryValidatePlayerVitalsFromRun(run, out int maxHp, out int currentHp))
                return false;

            player.InitializeVitals(maxHp, currentHp);

            LastPlayerVitalsApplied = true;
            LastAppliedMaxHp = player.MaxHp;
            LastAppliedCurrentHp = player.CurrentHp;

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleRunBridge] Applied player vitals from active run. " +
                    $"HP={LastAppliedCurrentHp}/{LastAppliedMaxHp}");
            }

            return true;
        }

        private bool TryGetValidActiveRun(out RunState run)
        {
            run = null;

            RunManager runManager = RunManager.Instance;
            if (runManager == null)
            {
                Debug.LogError("[BattleRunBridge] RunManager.Instance is null.");
                return false;
            }

            if (!runManager.HasActiveRun)
            {
                Debug.LogError("[BattleRunBridge] No active run exists.");
                return false;
            }

            run = runManager.CurrentRun;
            if (run == null)
            {
                Debug.LogError("[BattleRunBridge] CurrentRun is null.");
                return false;
            }

            return true;
        }

        private bool TryResolveDeckFromRun(RunState run, out List<CardData> resolvedCards)
        {
            resolvedCards = null;

            if (deckController == null)
            {
                Debug.LogError("[BattleRunBridge] DeckController reference is missing.");
                return false;
            }

            if (cardCatalog == null)
            {
                Debug.LogError("[BattleRunBridge] CardCatalog reference is missing.");
                return false;
            }

            if (run.currentDeck == null)
            {
                Debug.LogError("[BattleRunBridge] CurrentRun.currentDeck is null.");
                return false;
            }

            resolvedCards = new List<CardData>(run.currentDeck.Count);

            for (int i = 0; i < run.currentDeck.Count; i++)
            {
                RunCardRecord record = run.currentDeck[i];
                if (record == null)
                {
                    Debug.LogWarning("[BattleRunBridge] Skipping null RunCardRecord in current deck.");
                    LastMissingCardCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(record.cardId))
                {
                    Debug.LogWarning("[BattleRunBridge] Skipping RunCardRecord with blank card ID.");
                    LastMissingCardCount++;
                    continue;
                }

                // Phase 3D-A: upgradeLevel is retained in RunState.
                // A later card-instance factory will apply upgrade modifiers.

                if (!cardCatalog.TryGetCard(record.cardId, out CardData data))
                {
                    Debug.LogWarning(
                        $"[BattleRunBridge] Card ID not found in catalog: '{record.cardId}'.");
                    LastMissingCardCount++;
                    continue;
                }

                resolvedCards.Add(data);
            }

            LastResolvedCardCount = resolvedCards.Count;

            if (resolvedCards.Count == 0)
            {
                Debug.LogError(
                    "[BattleRunBridge] Active run deck contains no valid resolvable cards.");
                return false;
            }

            return true;
        }

        private bool TryValidatePlayerVitalsFromRun(RunState run, out int maxHp, out int currentHp)
        {
            maxHp = 0;
            currentHp = 0;

            if (player == null)
            {
                Debug.LogError("[BattleRunBridge] PlayerBattleUnit reference is missing.");
                return false;
            }

            if (run == null)
            {
                Debug.LogError("[BattleRunBridge] RunState is null.");
                return false;
            }

            if (run.maxHp <= 0)
            {
                Debug.LogError(
                    $"[BattleRunBridge] Active run max HP is invalid: {run.maxHp}.");
                return false;
            }

            if (run.currentHp <= 0)
            {
                Debug.LogError(
                    $"[BattleRunBridge] Active run current HP is invalid: {run.currentHp}.");
                return false;
            }

            maxHp = run.maxHp;
            currentHp = run.currentHp;

            if (currentHp > maxHp)
            {
                Debug.LogWarning(
                    $"[BattleRunBridge] Active run current HP ({currentHp}) exceeds max HP ({maxHp}). Clamping.");
                currentHp = maxHp;
            }

            return true;
        }

        private void ResetDeckDiagnostics()
        {
            LastResolvedCardCount = 0;
            LastMissingCardCount = 0;
        }

        private void ResetVitalsDiagnostics()
        {
            LastPlayerVitalsApplied = false;
            LastAppliedCurrentHp = 0;
            LastAppliedMaxHp = 0;
        }

        private void ResetAllDiagnostics()
        {
            ResetDeckDiagnostics();
            ResetVitalsDiagnostics();
        }
    }
}
