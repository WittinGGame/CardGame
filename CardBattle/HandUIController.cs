using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle.Core
{
    /// <summary>
    /// Minimal hand UI for testing card play without fancy visuals.
    /// - Rebuilds the hand whenever deck piles change
    /// - Creates one button per card in hand
    /// - Clicking a button plays that card
    /// - Uses the first alive enemy as default target for attack cards
    /// </summary>
    public class HandUIController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;

        [Header("UI References")]
        [SerializeField] private Transform handContainer;
        [SerializeField] private Button cardButtonPrefab;

        [Header("Options")]
        [SerializeField] private bool autoRefreshOnStart = true;
        [SerializeField] private bool showCost = true;
        [SerializeField] private bool showType = true;
        [SerializeField] private bool disableUnplayableCards = true;
        [SerializeField] private bool verboseLogs = false;

        private readonly List<Button> _spawnedButtons = new List<Button>();

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

            ClearSpawnedButtons();

            var hand = deckController.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                    continue;

                var button = Instantiate(cardButtonPrefab, handContainer);
                _spawnedButtons.Add(button);

                SetButtonLabel(button, BuildCardLabel(card));
                SetupButton(button, card);
            }
        }

        private void SetupButton(Button button, CardInstance card)
        {
            if (button == null || card?.Data == null)
                return;

            bool canPlay = player != null &&
                           player.CanAct &&
                           player.CanSpendAp(card.Data.ApCost) &&
                           deckController != null &&
                           deckController.IsInHand(card);

            if (disableUnplayableCards)
                button.interactable = canPlay;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                TryPlayCardFromButton(card);
            });
        }

        private void TryPlayCardFromButton(CardInstance card)
        {
            if (player == null || card?.Data == null)
                return;

            if (card.Data.CardType == CardType.Attack)
            {
                if (targetSelectionSystem != null)
                {
                    targetSelectionSystem.BeginTargetSelection(card);

                    if (verboseLogs)
                        Debug.Log($"[HandUI] Waiting for target selection: {card.Data.DisplayName}");

                    return;
                }
            }

            EnemyBattleUnit target = GetDefaultAliveEnemy();
            bool success = player.TryPlayCard(card, target);

            if (verboseLogs)
            {
                string targetName = target != null ? target.name : "None";
                Debug.Log($"[HandUI] Clicked {card.Data.DisplayName} | Target: {targetName} | Success: {success}");
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

        private string BuildCardLabel(CardInstance card)
        {
            var data = card.Data;
            string label = data.DisplayName;

            if (showCost)
                label += $"\nAP: {data.ApCost}";

            if (showType)
                label += $"\n{data.CardType}";

            switch (data.CardType)
            {
                case CardType.Attack:
                    label += $"\nDMG: {data.AttackDamage}";
                    break;

                case CardType.Heal:
                    label += $"\nHEAL: {data.HealAmount}";
                    break;

                case CardType.Buff:
                    label += $"\nBUFF: {data.BuffPotency}";
                    break;
            }

            return label;
        }

        private void SetButtonLabel(Button button, string textValue)
        {
            if (button == null)
                return;

            // First try TMP
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = textValue;
                return;
            }

            // Fallback to legacy Text
            var legacyText = button.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                legacyText.text = textValue;
            }
        }

        private void ClearSpawnedButtons()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                    Destroy(_spawnedButtons[i].gameObject);
            }

            _spawnedButtons.Clear();
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

            if (cardButtonPrefab == null)
            {
                Debug.LogError("HandUIController: Card button prefab reference is missing.");
                valid = false;
            }

            return valid;
        }
    }
}