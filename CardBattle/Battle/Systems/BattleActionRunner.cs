using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Event-driven action sequencer.
    /// Player attack timing is controlled by BattleUnitView animation events:
    /// - AnimEvent_AttackHit
    /// - AnimEvent_ActionFinished
    /// </summary>
    public class BattleActionRunner : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private DeckController deckController;
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleHUDController battleHUDController;
        [SerializeField] private CardToGraveyardVFXController graveyardVfx;
        [SerializeField] private PileCounterUI pileCounterUI;

        [Header("Audio")]
        [SerializeField] private CardSFXController cardSfx;
        [SerializeField] private CombatSFXController combatSfx;

        [Header("Fallback / Non-Attack Timing")]
        [SerializeField] private float nonAttackResolvePause = 0.05f;
        [SerializeField] private float endTurnPause = 0.2f;
        [SerializeField] private float enemyResolveSafetyPause = 0.1f;

        public bool IsBusy { get; private set; }
        public event System.Action<bool> OnBusyStateChanged;
        public bool CanAcceptInput => !IsBusy && player != null && player.CanAct && player.IsAlive;

        private bool waitingForPlayerHit;
        private bool waitingForPlayerFinish;
        private bool playerAttackResolved;
        private CardPlayContext pendingPlayerCardContext;
        private EnemyBattleUnit pendingPrimaryTarget;

        public void TryPlayCard(CardInstance card, EnemyBattleUnit primaryTarget = null)
        {
            if (IsBusy || card?.Data == null || player == null || !player.IsAlive)
                return;

            StartCoroutine(PlayCardSequence(card, primaryTarget));
        }

        public void TryEndTurn()
        {
            if (IsBusy || player == null || !player.IsAlive || !player.CanAct)
                return;

            StartCoroutine(EndTurnSequence());
        }

        private IEnumerator PlayCardSequence(CardInstance card, EnemyBattleUnit primaryTarget)
        {
            if (!ValidateCardPlay(card))
                yield break;

            SetBusy(true);
            RefreshExternalUI();
            cardSfx?.PlayCardPlayed();

            int cost = card.Data.ApCost;
            player.SpendApFromRunner(cost);

            CardViewUI handViewForVfx =
                handUIController != null ? handUIController.GetViewForCard(card) : null;
            if (graveyardVfx != null)
                graveyardVfx.PlaySingleCardToGraveyard(handViewForVfx);

            deckController.PlayCardFromHand(card);

            if (graveyardVfx == null)
                pileCounterUI?.ForceSyncDisplayedToReal();

            bool isAttack = card.Data.CardType == CardType.Attack;
            pendingPrimaryTarget = primaryTarget;

            if (isAttack)
            {
                if (player?.View == null)
                {
                    Debug.LogWarning("BattleActionRunner: Player view is missing, falling back to immediate resolve.");
                    ResolvePlayerCardImmediate(card, primaryTarget);
                }
                else
                {
                    pendingPlayerCardContext = new CardPlayContext(player, card, enemyActionSystem.Enemies, primaryTarget);
                    waitingForPlayerHit = true;
                    waitingForPlayerFinish = true;
                    playerAttackResolved = false;

                    SubscribePlayerViewEvents();
                    player.View.PlayAttack();

                    yield return new WaitUntil(() => !waitingForPlayerFinish);

                    CleanupPlayerAttackState();
                }
            }
            else
            {
                ResolvePlayerCardImmediate(card, primaryTarget);
                yield return new WaitForSeconds(nonAttackResolvePause);
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

        private IEnumerator EndTurnSequence()
        {
            SetBusy(true);
            RefreshExternalUI();

            if (graveyardVfx != null && handUIController != null)
                graveyardVfx.PlayBatchCardsToGraveyard(handUIController.GetCurrentHandViewsSnapshot());

            player.CommitEndTurnFromRunner();

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

            if (player != null && player.IsAlive && HasAliveEnemy())
                yield return enemyActionSystem.StartPlayerRoundRoutine();

            RefreshExternalUI();
            SetBusy(false);
            RefreshExternalUI();
        }

        private void ResolvePlayerCardImmediate(CardInstance card, EnemyBattleUnit primaryTarget)
        {
            var context = new CardPlayContext(player, card, enemyActionSystem.Enemies, primaryTarget);
            cardResolver.Resolve(context);
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

            cardResolver.Resolve(pendingPlayerCardContext);
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
                    cardResolver.Resolve(pendingPlayerCardContext);
            }

            waitingForPlayerFinish = false;
        }

        private void CleanupPlayerAttackState()
        {
            CleanupPlayerViewSubscriptions();
            waitingForPlayerHit = false;
            waitingForPlayerFinish = false;
            playerAttackResolved = false;
            pendingPlayerCardContext = null;
            pendingPrimaryTarget = null;
        }

        private bool ValidateCardPlay(CardInstance card)
        {
            if (player == null || deckController == null || cardResolver == null || enemyActionSystem == null)
            {
                Debug.LogError("BattleActionRunner missing references.");
                return false;
            }

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

        private void RefreshExternalUI()
        {
            handUIController?.RefreshInteractivityExternal();
            battleHUDController?.RefreshUIExternal();
        }

        private void OnDisable()
        {
            CleanupPlayerAttackState();
            SetBusy(false);
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

            // Single-target attack
            if (cardData.TargetMode == CardTargetMode.SingleEnemy)
            {
                return context.PrimaryTarget != null &&
                    context.PrimaryTarget.IsAlive;
            }

            // All-enemy attack
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

            // Legacy attack cards without the Effects pipeline
            if (cardData.CardType == CardType.Attack)
            {
                if (context.PrimaryTarget != null &&
                    context.PrimaryTarget.IsAlive)
                {
                    return true;
                }

                if (context.Enemies == null)
                    return false;

                for (int i = 0; i < context.Enemies.Count; i++)
                {
                    EnemyBattleUnit enemy = context.Enemies[i];

                    if (enemy != null && enemy.IsAlive)
                        return true;
                }
            }

            return false;
        }
    }
}