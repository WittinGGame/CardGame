using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Minimal bootstrap for testing the core battle loop without UI.
    ///
    /// Controls:
    /// 1 = Play hand card at index 0
    /// 2 = Play hand card at index 1
    /// 3 = Play hand card at index 2
    /// 4 = Play hand card at index 3
    /// 5 = Play hand card at index 4
    /// E = End turn
    /// R = Restart battle setup
    /// T = Print battle state to console
    /// </summary>
    public class BattleTestBootstrap : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private DeckController deckController;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [Header("Optional")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private bool verboseLogs = true;
        [SerializeField] private int defaultTargetEnemyIndex = 0;

        [Header("Run Integration")]
        [SerializeField] private BattleRunBridge battleRunBridge;

        private bool _initialized;

        private void Start()
        {
            if (autoStartOnPlay)
                StartTestBattle();
        }

        private void Update()
        {
            if (!_initialized)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) TryPlayCardAtHandIndex(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryPlayCardAtHandIndex(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryPlayCardAtHandIndex(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryPlayCardAtHandIndex(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TryPlayCardAtHandIndex(4);

            if (Input.GetKeyDown(KeyCode.E)) EndTurn();
            if (Input.GetKeyDown(KeyCode.R)) StartTestBattle();
            if (Input.GetKeyDown(KeyCode.T)) PrintBattleState();
        }

        [ContextMenu("Start Test Battle")]
        public void StartTestBattle()
        {
            if (!ValidateReferences())
                return;

            if (battleRunBridge != null && battleRunBridge.HasActiveRun)
            {
                if (!battleRunBridge.TryInitializeBattleFromActiveRun())
                {
                    Debug.LogError(
                        "BattleTestBootstrap: Active run exists, " +
                        "but Battle data could not be initialized.");
                    return;
                }
            }
            else
            {
                deckController.BuildFromInspectorBlueprint();
            }

            enemyActionSystem.ResetTurnCounter();
            enemyActionSystem.StartPlayerRound();
            _initialized = true;

            if (verboseLogs)
            {
                Debug.Log("=== Test Battle Started ===");
                PrintBattleState();
            }
        }

        public void TryPlayCardAtHandIndex(int handIndex)
        {
            if (!ValidateReferences())
                return;

            var hand = deckController.Hand;
            if (handIndex < 0 || handIndex >= hand.Count)
            {
                if (verboseLogs)
                    Debug.LogWarning($"No card in hand slot {handIndex}.");
                return;
            }

            var card = hand[handIndex];
            var target = GetDefaultAliveEnemy();

            if (card == null)
            {
                if (verboseLogs)
                    Debug.LogWarning($"Hand slot {handIndex} is null.");
                return;
            }

            var success = player.TryPlayCard(card, target);

            if (verboseLogs)
            {
                var targetName = target != null ? target.name : "None";
                Debug.Log($"Play Card [{handIndex}] => {card.Data.DisplayName} | Target: {targetName} | Success: {success}");
                PrintBattleState();
            }

            CheckSimpleBattleEnd();
        }

        public void EndTurn()
        {
            if (!ValidateReferences())
                return;

            player.RequestEndTurn();

            if (verboseLogs)
            {
                Debug.Log("=== End Turn ===");
                PrintBattleState();
            }

            CheckSimpleBattleEnd();

            if (player != null && player.IsAlive && HasAliveEnemy())
            {
                enemyActionSystem.StartPlayerRound();

                if (verboseLogs)
                {
                    Debug.Log("=== New Player Round Started ===");
                    PrintBattleState();
                }
            }
        }

        [ContextMenu("Print Battle State")]
        public void PrintBattleState()
        {
            if (!ValidateReferences())
                return;

            var sb = new StringBuilder();

            sb.AppendLine("----- Battle State -----");

            if (player != null)
            {
                sb.AppendLine($"Player HP: {player.CurrentHp}/{player.MaxHp}");
                sb.AppendLine($"Player AP: {player.CurrentAp}/{player.ApPerRound}");
                sb.AppendLine($"Player CanAct: {player.CanAct}");
            }

            sb.AppendLine($"Deck: {deckController.Deck.Count}");
            sb.AppendLine($"Hand: {deckController.Hand.Count}");
            sb.AppendLine($"Graveyard: {deckController.Graveyard.Count}");

            for (int i = 0; i < deckController.Hand.Count; i++)
            {
                var card = deckController.Hand[i];
                if (card?.Data == null) continue;

                sb.AppendLine($"Hand[{i}] = {card.Data.DisplayName} | Cost: {card.Data.ApCost} | Type: {card.Data.CardType}");
            }

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;

                sb.AppendLine(
                    $"Enemy[{i}] {enemy.name} | HP: {enemy.CurrentHp}/{enemy.MaxHp} | Alive: {enemy.IsAlive} | Behavior: {enemy.Behavior} | Countdown: {enemy.CurrentCountdown} | Speed: {enemy.Speed} | ActedThisRound: {enemy.HasAttackedThisPlayerRound}"
                );
            }

            Debug.Log(sb.ToString());
        }

        private EnemyBattleUnit GetDefaultAliveEnemy()
        {
            var enemies = enemyActionSystem.Enemies;
            if (enemies == null || enemies.Count == 0)
                return null;

            if (defaultTargetEnemyIndex >= 0 &&
                defaultTargetEnemyIndex < enemies.Count &&
                enemies[defaultTargetEnemyIndex] != null &&
                enemies[defaultTargetEnemyIndex].IsAlive)
            {
                return enemies[defaultTargetEnemyIndex];
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return enemies[i];
            }

            return null;
        }

        private bool HasAliveEnemy()
        {
            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return true;
            }

            return false;
        }

        private void CheckSimpleBattleEnd()
        {
            if (player == null)
                return;

            if (!player.IsAlive)
            {
                Debug.Log("=== Defeat: Player HP reached 0 ===");
                return;
            }

            if (!HasAliveEnemy())
            {
                Debug.Log("=== Victory: All enemies defeated ===");
            }
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (player == null)
            {
                Debug.LogError("BattleTestBootstrap: PlayerBattleUnit reference is missing.");
                valid = false;
            }

            if (deckController == null)
            {
                Debug.LogError("BattleTestBootstrap: DeckController reference is missing.");
                valid = false;
            }

            if (enemyActionSystem == null)
            {
                Debug.LogError("BattleTestBootstrap: EnemyActionSystem reference is missing.");
                valid = false;
            }

            return valid;
        }
    }
}