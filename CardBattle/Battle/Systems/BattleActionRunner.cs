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

        [SerializeField, Min(0.1f)] private float animationEventTimeout = 10f;

        public PlayerBattleUnit Player => player;
        public BattleOutcomeController OutcomeController => battleOutcomeController;
        public bool IsResolvingBattle => IsBusy ||
            (enemyActionSystem != null && enemyActionSystem.IsResolvingEnemyActions);
        public BattleActionExecution LastAction { get; private set; }
        public bool IsBusy { get; private set; }
        public event System.Action<bool> OnBusyStateChanged;

        private bool HasBattleEnded =>
            battleOutcomeController != null &&
            battleOutcomeController.IsBattleEnded;

        public bool CanAcceptInput =>
            isActiveAndEnabled &&
            !IsBusy &&
            (enemyActionSystem == null || !enemyActionSystem.IsResolvingEnemyActions) &&
            !HasBattleEnded &&
            player != null &&
            player.CanAct &&
            player.IsAlive;

        private bool waitingForPlayerHit;
        private bool waitingForPlayerFinish;
        private bool playerAttackResolved;
        private CardPlayContext pendingPlayerCardContext;
        private Coroutine runningActionRoutine;
        private bool runningSequence;

        private void OnEnable()
        {
            player?.BindActionRunner(this);
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void OnDisable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded -= HandleBattleEnded;

            ResetRuntimeActionState();
        }

        // Preserve the void UnityEvent entry point used by existing scenes/UI.
        public void TryPlayCard(CardInstance card, EnemyBattleUnit primaryTarget = null)
        {
            TryStartCard(card, primaryTarget);
        }

        /// <returns>Whether execution was accepted, not whether its asynchronous effects have finished.</returns>
        public bool TryStartCard(CardInstance card, EnemyBattleUnit primaryTarget = null)
        {
            if (!CanAcceptInput || runningSequence)
                return false;

            var execution = new BattleActionExecution();
            LastAction = execution;
            if (card?.Data == null || !ValidateCardPlay(card))
            {
                execution.Complete(BattleActionResult.Failed, "Card or required systems are not ready.");
                return false;
            }
            if (!HasValidPlayTarget(card, primaryTarget))
            {
                execution.Complete(BattleActionResult.Cancelled, "No valid target before execution.");
                return false;
            }

            // Validation and commit are synchronous, before AP/pile mutation and animation.
            execution.Commit();
            player.StatusController?.BeginOwnerAction(execution, card.Data.CardType == CardType.Attack);
            StartSequence(PlayCardSequence(card, primaryTarget, execution), execution);
            return true;
        }

        public void ResetRuntimeActionState()
        {
            LastAction?.Cancel("Battle action reset or disabled.");
            enemyActionSystem?.ResetRuntimeActions();
            if (runningActionRoutine != null)
                StopCoroutine(runningActionRoutine);
            runningActionRoutine = null;
            runningSequence = false;
            handCardSelectionController?.ForceCancelSelection();
            CleanupPlayerAttackState();
            SetBusy(false);
            RefreshExternalUI();
        }

        public void TryEndTurn()
        {
            if (!CanAcceptInput || runningSequence)
                return;
            var execution = new BattleActionExecution();
            LastAction = execution;
            if (deckController == null || enemyActionSystem == null)
            {
                execution.Complete(BattleActionResult.Failed, "End turn systems are missing.");
                return;
            }
            execution.Commit();
            StartSequence(EndTurnSequenceCore(), execution);
        }

        private void StartSequence(IEnumerator routine, BattleActionExecution execution)
        {
            runningSequence = true;
            SetBusy(true);
            Coroutine handle = StartCoroutine(RunSequence(routine, execution));
            // A coroutine can finish synchronously before StartCoroutine returns.
            if (runningSequence && ReferenceEquals(LastAction, execution))
                runningActionRoutine = handle;
        }

        private IEnumerator RunSequence(IEnumerator routine, BattleActionExecution execution)
        {
            try
            {
                yield return execution.Run(routine, exception => Debug.LogException(exception, this));
                execution.Complete(BattleActionResult.Successful);
            }
            finally
            {
                execution.Complete(BattleActionResult.Cancelled, "Execution interrupted.");
                if (ReferenceEquals(LastAction, execution))
                {
                    CleanupPlayerAttackState();
                    handCardSelectionController?.ForceCancelSelection();
                    runningActionRoutine = null;
                    runningSequence = false;
                    SetBusy(false);
                    RefreshExternalUI();
                }
            }
        }

        private bool HasValidPlayTarget(CardInstance card, EnemyBattleUnit target)
        {
            if (card.Data.TargetMode == CardTargetMode.SingleEnemy)
            {
                if (target == null || !target.IsAlive || !target.isActiveAndEnabled)
                    return false;
                foreach (var enemy in enemyActionSystem.Enemies)
                    if (enemy == target) return true;
                return false;
            }
            if (card.Data.TargetMode == CardTargetMode.AllEnemies)
            {
                foreach (var enemy in enemyActionSystem.Enemies)
                    if (enemy != null && enemy.IsAlive && enemy.isActiveAndEnabled) return true;
                return false;
            }
            return card.Data.CardType != CardType.Attack;
        }

        private IEnumerator PlayCardSequence(CardInstance card, EnemyBattleUnit primaryTarget, BattleActionExecution execution)
        {
            SetBusy(true);
            RefreshExternalUI();
            cardSfx?.PlayCardPlayed(card.Data.CardType);

            int cost = card.EffectiveApCost;
            player.SpendApFromRunner(cost);

            PlayedCardDestination destination = DeckController.ResolvePlayedCardDestination(card);
            CardViewUI handViewForVfx =
                handUIController != null ? handUIController.GetViewForCard(card) : null;

            // Destination is fixed before effects run. Only Graveyard should use Graveyard VFX.
            // Future: branch to destination-specific VFX when available.
            if (destination == PlayedCardDestination.Graveyard && graveyardVfx != null)
                graveyardVfx.PlaySingleCardToGraveyard(handViewForVfx);

            deckController.PlayCardFromHand(card);

            if (destination != PlayedCardDestination.Graveyard || graveyardVfx == null)
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

                    SubscribePlayerViewEvents();
                    player.View.PlayAttack();

                    float elapsed = 0f;
                    while (waitingForPlayerHit && waitingForPlayerFinish)
                    {
                        elapsed += Time.deltaTime;
                        if (elapsed >= Mathf.Max(0.1f, animationEventTimeout))
                        {
                            Debug.LogWarning("Player animation hit/finish timeout; resolving committed card once.", this);
                            waitingForPlayerFinish = false;
                            break;
                        }
                        yield return null;
                    }
                    waitingForPlayerHit = false;
                    playerAttackResolved = true;
                    yield return ExecuteEffectSequence(pendingPlayerCardContext);

                    // Human hand selection/effects are not subject to the animation timeout.
                    elapsed = 0f;
                    while (waitingForPlayerFinish)
                    {
                        elapsed += Time.deltaTime;
                        if (elapsed >= Mathf.Max(0.1f, animationEventTimeout))
                        {
                            Debug.LogWarning("Player animation finish timeout; completing committed card.", this);
                            waitingForPlayerFinish = false;
                            break;
                        }
                        yield return null;
                    }

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

            // Card completion precedes enemy reactions, even if the card killed the final target.
            execution.Complete(BattleActionResult.Successful);
            if (HasBattleEnded)
            {
                handCardSelectionController?.ForceCancelSelection();
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
        }


        private IEnumerator ExecuteEffectSequence(CardPlayContext context)
        {
            if (cardEffectSequenceRunner == null || !cardEffectSequenceRunner.isActiveAndEnabled)
                throw new System.InvalidOperationException("CardEffectSequenceRunner is unavailable during execution.");
            yield return cardEffectSequenceRunner.ExecuteEffectsSequentially(context);
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

        }

        private void HandlePlayerActionFinished()
        {
            if (!waitingForPlayerFinish)
                return;
            // The owning coroutine resolves effects once, including a missing-hit fallback.
            waitingForPlayerFinish = false;
        }

        private void CleanupPlayerAttackState()
        {
            CleanupPlayerViewSubscriptions();
            waitingForPlayerHit = false;
            waitingForPlayerFinish = false;
            playerAttackResolved = false;
            pendingPlayerCardContext = null;
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
                !enemyActionSystem.isActiveAndEnabled ||
                cardEffectSequenceRunner == null || !cardEffectSequenceRunner.isActiveAndEnabled)
            {
                Debug.LogError("BattleActionRunner missing references.");
                return false;
            }

            if (HasBattleEnded)
                return false;

            if (!player.isActiveAndEnabled || !player.CanAct || !player.IsAlive)
                return false;

            if (!deckController.IsInHand(card))
                return false;

            if (!player.CanSpendAp(card.EffectiveApCost))
                return false;

            return true;
        }

        [ContextMenu("Debug Print Last Action")]
        private void DebugPrintLastAction()
        {
            Debug.Log($"[BattleActionRunner] Result={LastAction?.Result} | Committed={LastAction?.IsCommitted} | Busy={IsBusy} | Reason={LastAction?.Reason}", this);
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
                if (card == null ||
                    DeckController.ResolveEndTurnDestination(card) != DeckController.EndTurnCardDestination.Graveyard)
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
