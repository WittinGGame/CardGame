using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Event-driven action sequencer.
    /// Player attack timing is controlled by BattleUnitView animation events:
    /// - AnimEvent_AttackHit
    /// - AnimEvent_ActionFinished
    /// Card effects execute sequentially via <see cref="CardEffectSequenceRunner"/>.
    /// </summary>
    public class BattleActionRunner : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private DeckController deckController;
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private CardEffectSequenceRunner cardEffectSequenceRunner;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private HandCardSelectionController handCardSelectionController;
        [SerializeField] private BattleHUDController battleHUDController;
        [SerializeField] private CardToGraveyardVFXController graveyardVfx;
        [SerializeField] private PileCounterUI pileCounterUI;

        [Header("Battle State")]
        [SerializeField] private BattleOutcomeController battleOutcomeController;

        [Header("Audio")]
        [SerializeField] private CardSFXController cardSfx;
        [SerializeField] private CombatSFXController combatSfx;

        [Header("Fallback / Non-Attack Timing")]
        [SerializeField] private float nonAttackResolvePause = 0.05f;
        [SerializeField] private float endTurnPause = 0.2f;
        [SerializeField] private float enemyResolveSafetyPause = 0.1f;

        public bool IsBusy { get; private set; }
        public event System.Action<bool> OnBusyStateChanged;

        private bool HasBattleEnded =>
            battleOutcomeController != null &&
            battleOutcomeController.IsBattleEnded;

        public bool CanAcceptInput =>
            !IsBusy &&
            !HasBattleEnded &&
            player != null &&
            player.CanAct &&
            player.IsAlive;

        private bool waitingForPlayerHit;
        private bool waitingForPlayerFinish;
        private bool playerAttackResolved;
        private bool effectSequenceStarted;
        private bool effectSequenceComplete;
        private CardPlayContext pendingPlayerCardContext;
        private Coroutine runningActionRoutine;
        private Coroutine pendingEffectSequenceRoutine;

        private void OnEnable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void OnDisable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded -= HandleBattleEnded;

            if (runningActionRoutine != null)
            {
                StopCoroutine(runningActionRoutine);
                runningActionRoutine = null;
            }

            StopPendingEffectSequence();
            handCardSelectionController?.ForceCancelSelection();
            CleanupPlayerAttackState();
            SetBusy(false);
        }

        public void TryPlayCard(CardInstance card, EnemyBattleUnit primaryTarget = null)
        {
            if (HasBattleEnded ||
                IsBusy ||
                card?.Data == null ||
                player == null ||
                !player.IsAlive)
            {
                return;
            }

            runningActionRoutine = StartCoroutine(PlayCardSequence(card, primaryTarget));
        }

        public void ResetRuntimeActionState()
        {
            if (runningActionRoutine != null)
            {
                StopCoroutine(runningActionRoutine);
                runningActionRoutine = null;
            }

            StopPendingEffectSequence();
            handCardSelectionController?.ForceCancelSelection();
            CleanupPlayerAttackState();
            SetBusy(false);
            RefreshExternalUI();
        }

        public void TryEndTurn()
        {
            if (HasBattleEnded ||
                IsBusy ||
                player == null ||
                !player.IsAlive ||
                !player.CanAct)
            {
                return;
            }

            runningActionRoutine = StartCoroutine(EndTurnSequence());
        }

        private IEnumerator PlayCardSequence(CardInstance card, EnemyBattleUnit primaryTarget)
        {
            try
            {
                if (!ValidateCardPlay(card))
                    yield break;

                SetBusy(true);
                RefreshExternalUI();
                cardSfx?.PlayCardPlayed(card.Data.CardType);

                int cost = card.Data.ApCost;
                player.SpendApFromRunner(cost);

                bool willExhaust = card.Data.ExhaustAfterPlay;
                CardViewUI handViewForVfx =
                    handUIController != null ? handUIController.GetViewForCard(card) : null;

                // Destination is fixed before effects run. Exhaust must not use Graveyard VFX.
                // Future: branch to CardToExhaustVFXController when available.
                if (!willExhaust && graveyardVfx != null)
                    graveyardVfx.PlaySingleCardToGraveyard(handViewForVfx);

                deckController.PlayCardFromHand(card);

                if (willExhaust || graveyardVfx == null)
                    pileCounterUI?.ForceSyncDisplayedToReal();

                bool isAttack = card.Data.CardType == CardType.Attack;

                if (isAttack)
                {
                    if (player?.View == null)
                    {
                        Debug.LogWarning("BattleActionRunner: Player view is missing, falling back to immediate resolve.");
                        var fallbackContext = new CardPlayContext(
                            player, card, enemyActionSystem.Enemies, primaryTarget);
                        yield return ExecuteEffectSequence(fallbackContext);
                    }
                    else
                    {
                        pendingPlayerCardContext = new CardPlayContext(
                            player, card, enemyActionSystem.Enemies, primaryTarget);
                        waitingForPlayerHit = true;
                        waitingForPlayerFinish = true;
                        playerAttackResolved = false;
                        effectSequenceStarted = false;
                        effectSequenceComplete = false;

                        SubscribePlayerViewEvents();
                        player.View.PlayAttack();

                        yield return new WaitUntil(() =>
                            !waitingForPlayerFinish &&
                            (!effectSequenceStarted || effectSequenceComplete));

                        CleanupPlayerAttackState();
                    }
                }
                else
                {
                    var context = new CardPlayContext(
                        player, card, enemyActionSystem.Enemies, primaryTarget);
                    yield return ExecuteEffectSequence(context);
                    yield return new WaitForSeconds(nonAttackResolvePause);
                }

                if (HasBattleEnded)
                {
                    handCardSelectionController?.ForceCancelSelection();
                    RefreshExternalUI();
                    SetBusy(false);
                    RefreshExternalUI();
                    yield break;
                }

                enemyActionSystem.HandlePlayerSuccessfullyPlayedCard();

                if (enemyActionSystem.IsResolvingEnemyActions)
                {
                    yield return new WaitUntil(() => !enemyActionSystem.IsResolvingEnemyActions);
                }
                else
                {
                    yield return new WaitForSeconds(enemyResolveSafetyPause);
                }

                RefreshExternalUI();
                SetBusy(false);
                RefreshExternalUI();
            }
            finally
            {
                runningActionRoutine = null;
            }
        }

        private IEnumerator ExecuteEffectSequence(CardPlayContext context)
        {
            if (cardEffectSequenceRunner != null)
            {
                yield return cardEffectSequenceRunner.ExecuteEffectsSequentially(context);
                yield break;
            }

            Debug.LogError(
                "BattleActionRunner: CardEffectSequenceRunner is missing. " +
                "Falling back to sync CardResolver (draws will not present).");

            if (cardResolver != null)
                cardResolver.Resolve(context);
        }

        private IEnumerator EndTurnSequence()
        {
            try
            {
                yield return EndTurnSequenceCore();
            }
            finally
            {
                runningActionRoutine = null;
            }
        }

        private IEnumerator EndTurnSequenceCore()
        {
            SetBusy(true);
            RefreshExternalUI();

            var discardableViews = CollectEndTurnDiscardableHandViews();

            if (graveyardVfx != null && discardableViews.Count > 0)
                graveyardVfx.PlayBatchCardsToGraveyard(discardableViews);

            player.CommitEndTurnFromRunner();

            if (deckController != null)
                deckController.ResolveEndTurnHand();

            if (graveyardVfx == null)
                pileCounterUI?.ForceSyncDisplayedToReal();
            yield return new WaitForSeconds(endTurnPause);

            enemyActionSystem.ResolveEndTurnAttacks();

            if (enemyActionSystem.IsResolvingEnemyActions)
            {
                yield return new WaitUntil(() => !enemyActionSystem.IsResolvingEnemyActions);
            }
            else
            {
                yield return new WaitForSeconds(enemyResolveSafetyPause);
            }

            if (!HasBattleEnded &&
                player != null &&
                player.IsAlive &&
                HasAliveEnemy())
            {
                yield return enemyActionSystem.StartPlayerRoundRoutine();
            }

            RefreshExternalUI();
            SetBusy(false);
            RefreshExternalUI();
        }

        private void SubscribePlayerViewEvents()
        {
            if (player?.View == null)
                return;

            CleanupPlayerViewSubscriptions();
            player.View.OnAttackHit += HandlePlayerAttackHit;
            player.View.OnActionFinished += HandlePlayerActionFinished;
        }

        private void CleanupPlayerViewSubscriptions()
        {
            if (player?.View == null)
                return;

            player.View.OnAttackHit -= HandlePlayerAttackHit;
            player.View.OnActionFinished -= HandlePlayerActionFinished;
        }

        private void HandlePlayerAttackHit()
        {
            if (!waitingForPlayerHit || playerAttackResolved)
                return;

            waitingForPlayerHit = false;
            playerAttackResolved = true;

            if (pendingPlayerCardContext == null)
                return;

            if (HasValidAttackHitTarget(pendingPlayerCardContext))
                combatSfx?.PlayAttackHit();

            BeginPendingEffectSequenceOnce(pendingPlayerCardContext);
        }

        private void HandlePlayerActionFinished()
        {
            if (!waitingForPlayerFinish)
                return;

            if (waitingForPlayerHit && !playerAttackResolved)
            {
                waitingForPlayerHit = false;
                playerAttackResolved = true;

                if (pendingPlayerCardContext != null)
                    BeginPendingEffectSequenceOnce(pendingPlayerCardContext);
            }

            waitingForPlayerFinish = false;
        }

        /// <summary>
        /// Starts sequential effects exactly once for the current attack card
        /// (hit event or finish-event fallback).
        /// </summary>
        private void BeginPendingEffectSequenceOnce(CardPlayContext context)
        {
            if (effectSequenceStarted || context == null)
                return;

            // Cancel any leftover coroutine without flipping started/complete flags.
            CancelPendingEffectCoroutine();

            effectSequenceStarted = true;
            effectSequenceComplete = false;
            pendingEffectSequenceRoutine = StartCoroutine(CoRunPendingEffectSequence(context));
        }

        private IEnumerator CoRunPendingEffectSequence(CardPlayContext context)
        {
            try
            {
                yield return ExecuteEffectSequence(context);
            }
            finally
            {
                effectSequenceComplete = true;
                pendingEffectSequenceRoutine = null;
            }
        }

        /// <summary>Stops the pending effect coroutine without mutating start/complete flags.</summary>
        private void CancelPendingEffectCoroutine()
        {
            if (pendingEffectSequenceRoutine == null)
                return;

            StopCoroutine(pendingEffectSequenceRoutine);
            pendingEffectSequenceRoutine = null;
        }

        /// <summary>Stops the pending effect sequence and marks it complete (reset / disable).</summary>
        private void StopPendingEffectSequence()
        {
            CancelPendingEffectCoroutine();
            effectSequenceStarted = false;
            effectSequenceComplete = true;
        }

        private void CleanupPlayerAttackState()
        {
            CleanupPlayerViewSubscriptions();
            waitingForPlayerHit = false;
            waitingForPlayerFinish = false;
            playerAttackResolved = false;
            pendingPlayerCardContext = null;
            // Do not stop an in-flight effect sequence from here during normal wait completion;
            // ResetRuntimeActionState / OnDisable stop it explicitly.
            effectSequenceStarted = false;
            effectSequenceComplete = false;
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            handCardSelectionController?.ForceCancelSelection();
            RefreshExternalUI();
        }

        private bool ValidateCardPlay(CardInstance card)
        {
            if (player == null ||
                deckController == null ||
                enemyActionSystem == null ||
                (cardEffectSequenceRunner == null && cardResolver == null))
            {
                Debug.LogError("BattleActionRunner missing references.");
                return false;
            }

            if (HasBattleEnded)
                return false;

            if (!player.CanAct || !player.IsAlive)
                return false;

            if (!deckController.IsInHand(card))
                return false;

            if (!player.CanSpendAp(card.Data.ApCost))
                return false;

            return true;
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

        private List<CardViewUI> CollectEndTurnDiscardableHandViews()
        {
            var views = new List<CardViewUI>();
            if (deckController == null || handUIController == null)
                return views;

            var hand = deckController.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card == null || DeckController.ResolveRetainAtEndTurn(card))
                    continue;

                var view = handUIController.GetViewForCard(card);
                if (view != null)
                    views.Add(view);
            }

            return views;
        }

        private void RefreshExternalUI()
        {
            handUIController?.RefreshInteractivityExternal();
            battleHUDController?.RefreshUIExternal();
        }

        private void SetBusy(bool value)
        {
            if (IsBusy == value)
                return;

            IsBusy = value;
            OnBusyStateChanged?.Invoke(IsBusy);
        }

        private static bool HasValidAttackHitTarget(CardPlayContext context)
        {
            if (context?.Card?.Data == null)
                return false;

            CardData cardData = context.Card.Data;

            if (cardData.TargetMode == CardTargetMode.SingleEnemy)
            {
                return context.PrimaryTarget != null &&
                    context.PrimaryTarget.IsAlive;
            }

            if (cardData.TargetMode == CardTargetMode.AllEnemies)
            {
                if (context.Enemies == null)
                    return false;

                for (int i = 0; i < context.Enemies.Count; i++)
                {
                    EnemyBattleUnit enemy = context.Enemies[i];

                    if (enemy != null && enemy.IsAlive)
                        return true;
                }

                return false;
            }

            return false;
        }
    }
}
