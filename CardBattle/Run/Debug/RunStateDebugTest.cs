using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class RunManagerDebugTest : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;

        private int startedCount;
        private int changedCount;
        private int clearedCount;

        private void OnEnable()
        {
            if (runManager == null)
                return;

            runManager.OnRunStarted += HandleRunStarted;
            runManager.OnRunChanged += HandleRunChanged;
            runManager.OnRunCleared += HandleRunCleared;
        }

        private void OnDisable()
        {
            if (runManager == null)
                return;

            runManager.OnRunStarted -= HandleRunStarted;
            runManager.OnRunChanged -= HandleRunChanged;
            runManager.OnRunCleared -= HandleRunCleared;
        }

        [ContextMenu("Run Phase 3B Tests")]
        public void RunTests()
        {
            startedCount = 0;
            changedCount = 0;
            clearedCount = 0;

            var starterDeck = new List<RunCardRecord>
            {
                new RunCardRecord("knight_strike"),
                new RunCardRecord("knight_strike"),
                new RunCardRecord("knight_guard")
            };

            bool started = runManager.StartNewRun(
                "run_001",
                12345,
                "knight",
                60,
                starterDeck
            );

            Debug.Log(
                $"[RunManager Test] Start | " +
                $"Success={started} | " +
                $"Active={runManager.HasActiveRun} | " +
                $"HP={runManager.CurrentRun.currentHp}/{runManager.CurrentRun.maxHp} | " +
                $"Deck={runManager.CurrentRun.currentDeck.Count} | " +
                $"StartedEvents={startedCount} | " +
                $"ChangedEvents={changedCount}"
            );

            runManager.SetCurrentHp(40);
            int changedAfterHp = changedCount;

            runManager.SetCurrentHp(40);

            Debug.Log(
                $"[RunManager Test] HP | " +
                $"HP={runManager.CurrentRun.currentHp} | " +
                $"ChangedAfterFirst={changedAfterHp} | " +
                $"ChangedAfterSameValue={changedCount}"
            );

            runManager.SetMaxHp(80, false);
            Debug.Log(
                $"[RunManager Test] MaxHP No Refill | " +
                $"HP={runManager.CurrentRun.currentHp}/{runManager.CurrentRun.maxHp}"
            );

            runManager.SetMaxHp(100, true);
            Debug.Log(
                $"[RunManager Test] MaxHP Refill | " +
                $"HP={runManager.CurrentRun.currentHp}/{runManager.CurrentRun.maxHp}"
            );

            bool addGold = runManager.AddGold(50);
            bool spend20 = runManager.SpendGold(20);
            bool spend40 = runManager.SpendGold(40);
            bool spend0 = runManager.SpendGold(0);
            bool spendNegative = runManager.SpendGold(-1);

            Debug.Log(
                $"[RunManager Test] Gold | " +
                $"Gold={runManager.CurrentRun.gold} | " +
                $"Add50={addGold} | Spend20={spend20} | " +
                $"Spend40={spend40} | Spend0={spend0} | " +
                $"SpendNegative={spendNegative}"
            );

            runManager.AddCard("knight_strike");
            runManager.AddCard("knight_strike");

            var sourceRecord = new RunCardRecord("shield_wall", 1);
            runManager.AddCard(sourceRecord);
            sourceRecord.SetUpgradeLevel(9);

            var storedRecord =
                runManager.CurrentRun.currentDeck[
                    runManager.CurrentRun.currentDeck.Count - 1
                ];

            Debug.Log(
                $"[RunManager Test] Cards | " +
                $"Deck={runManager.CurrentRun.currentDeck.Count} | " +
                $"SourceUpgrade={sourceRecord.upgradeLevel} | " +
                $"StoredUpgrade={storedRecord.upgradeLevel}"
            );

            bool invalidRemove = runManager.RemoveCardAt(999);

            Debug.Log(
                $"[RunManager Test] Invalid Remove | " +
                $"Result={invalidRemove}"
            );

            RunState snapshot = runManager.GetSnapshot();
            snapshot.gold = 9999;
            snapshot.currentDeck[0].SetUpgradeLevel(7);

            Debug.Log(
                $"[RunManager Test] Snapshot | " +
                $"SnapshotGold={snapshot.gold} | " +
                $"RealGold={runManager.CurrentRun.gold} | " +
                $"SnapshotUpgrade={snapshot.currentDeck[0].upgradeLevel} | " +
                $"RealUpgrade={runManager.CurrentRun.currentDeck[0].upgradeLevel}"
            );

            bool cleared = runManager.ClearRun();
            bool clearedAgain = runManager.ClearRun();

            Debug.Log(
                $"[RunManager Test] Clear | " +
                $"First={cleared} | Second={clearedAgain} | " +
                $"Active={runManager.HasActiveRun} | " +
                $"Deck={runManager.CurrentRun.currentDeck.Count} | " +
                $"ClearedEvents={clearedCount}"
            );
        }

        private void HandleRunStarted(RunState state)
        {
            startedCount++;
            Debug.Log($"[RunManager Event] Started: {state.playerClassId}");
        }

        private void HandleRunChanged(RunState state)
        {
            changedCount++;
            Debug.Log($"[RunManager Event] Changed #{changedCount}");
        }

        private void HandleRunCleared()
        {
            clearedCount++;
            Debug.Log("[RunManager Event] Cleared");
        }
    }
}