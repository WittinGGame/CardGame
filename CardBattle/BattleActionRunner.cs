using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Simple action sequencer for one card/action at a time.
    /// Locks player input while resolving animations and gameplay steps.
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

        [Header("Timing")]
        [SerializeField] private float playerAttackWindup = 0.25f;
        [SerializeField] private float hurtPause = 0.2f;
        [SerializeField] private float enemyAttackWindup = 0.25f;
        [SerializeField] private float endTurnPause = 0.2f;

        public bool IsBusy { get; private set; }

        public bool CanAcceptInput => !IsBusy && player != null && player.CanAct && player.IsAlive;

        private bool waitingForHit;
        private bool waitingForFinish;
        private CardPlayContext pendingContext;

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

            IsBusy = true;
            RefreshExternalUI();

            int cost = card.Data.ApCost;
            player.SpendApFromRunner(cost);
            deckController.PlayCardFromHand(card);

            bool isAttack = card.Data.CardType == CardType.Attack;
            var context = new CardPlayContext(player, card, enemyActionSystem.Enemies, primaryTarget);

            if (isAttack)
            {
                if (player.View != null)
                {
                    pendingContext = context;
                    waitingForHit = true;
                    waitingForFinish = true;

                    SubscribeToPlayerAttackEvents();
                    player.View.PlayAttack();

                    yield return new WaitUntil(() => !waitingForFinish);
                    UnsubscribeFromPlayerAttackEvents();
                    pendingContext = null;
                }
                else
                {
                    cardResolver.Resolve(context);
                    enemyActionSystem.HandlePlayerSuccessfullyPlayedCard();
                }
            }
            else
            {
                cardResolver.Resolve(context);
                enemyActionSystem.HandlePlayerSuccessfullyPlayedCard();
            }

            // wait until enemy interrupt actions (if any) are fully resolved
            yield return new WaitUntil(() => enemyActionSystem == null || !enemyActionSystem.IsResolvingEnemyActions);

            RefreshExternalUI();
            IsBusy = false;
            RefreshExternalUI();
        }

        private IEnumerator EndTurnSequence()
        {
            IsBusy = true;
            RefreshExternalUI();

            player.CommitEndTurnFromRunner();
            yield return new WaitForSeconds(endTurnPause);

            enemyActionSystem.ResolveEndTurnAttacks();
            yield return new WaitUntil(() => enemyActionSystem == null || !enemyActionSystem.IsResolvingEnemyActions);

            if (player != null && player.IsAlive && HasAliveEnemy())
            {
                enemyActionSystem.StartPlayerRound();
            }

            RefreshExternalUI();
            IsBusy = false;
            RefreshExternalUI();
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
            handUIController?.RefreshHandUI();
            battleHUDController?.RefreshUIExternal();
        }

        private void OnDisable()
        {
            UnsubscribeFromPlayerAttackEvents();
            waitingForHit = false;
            waitingForFinish = false;
            pendingContext = null;
        }

        private void SubscribeToPlayerAttackEvents()
        {
            if (player?.View == null)
                return;

            UnsubscribeFromPlayerAttackEvents();
            player.View.OnAttackHit += HandlePlayerAttackHit;
            player.View.OnActionFinished += HandlePlayerActionFinished;
        }

        private void UnsubscribeFromPlayerAttackEvents()
        {
            if (player?.View == null)
                return;

            player.View.OnAttackHit -= HandlePlayerAttackHit;
            player.View.OnActionFinished -= HandlePlayerActionFinished;
        }

        private void HandlePlayerAttackHit()
        {
            if (!waitingForHit)
                return;

            waitingForHit = false;
            if (pendingContext != null)
                cardResolver.Resolve(pendingContext);
        }

        private void HandlePlayerActionFinished()
        {
            if (!waitingForFinish)
                return;

            waitingForFinish = false;
            enemyActionSystem.HandlePlayerSuccessfullyPlayedCard();
        }
    }
}