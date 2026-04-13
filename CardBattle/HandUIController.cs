using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class HandUIController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;
        [SerializeField] private BattleActionRunner battleActionRunner;

        [Header("UI References")]
        [SerializeField] private Transform handContainer;
        [SerializeField] private CardViewUI cardViewPrefab;

        [Header("Options")]
        [SerializeField] private bool autoRefreshOnStart = true;
        [SerializeField] private bool disableUnplayableCards = true;
        [SerializeField] private bool verboseLogs = false;

        private readonly List<CardViewUI> spawnedCards = new List<CardViewUI>();

        private void OnEnable()
        {
            if (deckController != null)
                deckController.OnPilesChanged += RefreshHandUI;
        }

        private void OnDisable()
        {
            if (deckController != null)
                deckController.OnPilesChanged -= RefreshHandUI;
        }

        private void Start()
        {
            if (autoRefreshOnStart)
                RefreshHandUI();
        }

        [ContextMenu("Refresh Hand UI")]
        public void RefreshHandUI()
        {
            if (!ValidateReferences())
                return;

            ClearSpawnedCards();

            var hand = deckController.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                    continue;

                var view = Instantiate(cardViewPrefab, handContainer);
                spawnedCards.Add(view);

                view.Bind(card);
                SetupCardView(view, card);
            }
        }

        private void SetupCardView(CardViewUI view, CardInstance card)
        {
            if (view == null || card?.Data == null)
                return;

            bool canPlay = player != null &&
                           player.CanAct &&
                           player.CanSpendAp(card.Data.ApCost) &&
                           deckController != null &&
                           deckController.IsInHand(card);

            if (battleActionRunner != null)
                canPlay = canPlay && battleActionRunner.CanAcceptInput;

            if (disableUnplayableCards)
                view.SetInteractable(canPlay);

            view.SetClickAction(() =>
            {
                TryPlayCardFromView(card);
            });
        }

        private void TryPlayCardFromView(CardInstance card)
        {
            if (card?.Data == null || battleActionRunner == null)
                return;

            if (card.Data.CardType == CardType.Attack)
            {
                if (targetSelectionSystem != null)
                {
                    targetSelectionSystem.BeginTargetSelection(card);

                    if (verboseLogs)
                        Debug.Log($"[HandUI] Waiting for target selection: {card.Data.DisplayName}");

                    RefreshHandUI();
                    return;
                }
            }

            EnemyBattleUnit target = GetDefaultAliveEnemy();
            battleActionRunner.TryPlayCard(card, target);

            if (verboseLogs)
            {
                string targetName = target != null ? target.name : "None";
                Debug.Log($"[HandUI] Clicked {card.Data.DisplayName} | Target: {targetName}");
            }

            RefreshHandUI();
        }

        private EnemyBattleUnit GetDefaultAliveEnemy()
        {
            if (enemyActionSystem == null)
                return null;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return enemies[i];
            }

            return null;
        }

        private void ClearSpawnedCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    Destroy(spawnedCards[i].gameObject);
            }

            spawnedCards.Clear();
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (deckController == null)
            {
                Debug.LogError("HandUIController: DeckController reference is missing.");
                valid = false;
            }

            if (player == null)
            {
                Debug.LogError("HandUIController: PlayerBattleUnit reference is missing.");
                valid = false;
            }

            if (enemyActionSystem == null)
            {
                Debug.LogError("HandUIController: EnemyActionSystem reference is missing.");
                valid = false;
            }

            if (handContainer == null)
            {
                Debug.LogError("HandUIController: Hand container reference is missing.");
                valid = false;
            }

            if (cardViewPrefab == null)
            {
                Debug.LogError("HandUIController: CardView prefab reference is missing.");
                valid = false;
            }

            return valid;
        }
    }
}