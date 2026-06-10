using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Coordinates enemy reactions to the player's cards and turn boundaries.
    /// Handles countdown interrupts (sorted by <see cref="EnemyBattleUnit.Speed"/> descending)
    /// and end-of-turn attackers while respecting the "one attack per enemy per player round" rule.
    /// </summary>
    public class EnemyActionSystem : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private List<EnemyBattleUnit> enemies = new List<EnemyBattleUnit>();
        [SerializeField] private GraveyardToDeckVFXController graveyardToDeckVfx;
        [SerializeField] private float postReshuffleDrawDelay = 0.08f;

        [Header("Turn Presentation")]
        [SerializeField] private TurnPresentationController turnPresentation;

        private Coroutine runningEnemyActions;

        public PlayerBattleUnit Player => player;
        public IReadOnlyList<EnemyBattleUnit> Enemies => enemies;
        public bool IsResolvingEnemyActions => runningEnemyActions != null;
        public int CurrentTurn { get; private set; }

        public event System.Action<int> OnTurnStarted;

#if UNITY_EDITOR
        private void OnValidate()
        {
            enemies.RemoveAll(e => e == null);
        }
#endif

        public void ResetTurnCounter()
        {
            CurrentTurn = 0;
        }

        /// <summary>Designer helper to register enemies without code.</summary>
        public void RegisterEnemy(EnemyBattleUnit enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        /// <summary>
        /// Begins the player's round: clears enemy attack flags, refreshes AP, and draws cards.
        /// Call this from your battle director after enemy phases (if any) complete.
        /// </summary>
        public void StartPlayerRound()
        {
            StartCoroutine(StartPlayerRoundRoutine());
        }

        public IEnumerator StartPlayerRoundRoutine()
        {
            if (player == null)
            {
                Debug.LogError("EnemyActionSystem requires a PlayerBattleUnit reference.");
                yield break;
            }

            CurrentTurn++;
            OnTurnStarted?.Invoke(CurrentTurn);

            if (turnPresentation != null)
                yield return turnPresentation.PlayTurnIntro(CurrentTurn);

            // Reset enemy flags
            foreach (var enemy in enemies)
                enemy?.ResetRoundCombatFlags();

            // Player round start state
            player.BeginRoundState();

            if (player.DeckController == null)
            {
                Debug.LogError("Player is missing a DeckController.");
                yield break;
            }

            int requestedDraw = Mathf.Max(0, player.DrawPerRound);

            // ==============================
            // STEP A — DRAW FROM DECK FIRST
            // ==============================
            int availableDeck = player.DeckController.GetDeckCount();
            int firstDraw = Mathf.Min(requestedDraw, availableDeck);

            if (firstDraw > 0)
                player.DeckController.DrawCardsImmediate(firstDraw);

            int remaining = requestedDraw - firstDraw;
            if (remaining <= 0)
                yield break;

            // ==============================
            // STEP B — RESHUFFLE PRESENTATION
            // ==============================
            int graveCount = player.DeckController.GetGraveyardCount();
            if (graveCount <= 0)
                yield break;

            if (graveyardToDeckVfx != null)
                yield return graveyardToDeckVfx.PlayReshuffleVfx(graveCount);

            // ==============================
            // STEP C — APPLY REAL RESHUFFLE
            // ==============================
            player.DeckController.ReshuffleGraveyardIntoDeckImmediate();

            // ==============================
            // STEP D — SMALL DELAY (POLISH)
            // ==============================
            if (postReshuffleDrawDelay > 0f)
                yield return new WaitForSeconds(postReshuffleDrawDelay);

            // ==============================
            // STEP E — DRAW REMAINING CARDS
            // ==============================
            int secondDraw = Mathf.Min(remaining, player.DeckController.GetDeckCount());

            if (secondDraw > 0)
                player.DeckController.DrawCardsImmediate(secondDraw);
        }

        /// <summary>
        /// Invoked after a card fully resolves. Steps countdowns, then processes simultaneous interrupts.
        /// </summary>
        public void HandlePlayerSuccessfullyPlayedCard()
        {
            if (player == null)
                return;

            foreach (var enemy in enemies)
                enemy?.StepCountdownAfterPlayerCard();

            var ready = new List<EnemyBattleUnit>();
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsCountdownReady)
                    ready.Add(enemy);
            }

            ready.Sort((a, b) => b.Speed.CompareTo(a.Speed));
            if (runningEnemyActions != null)
                return;

            runningEnemyActions = StartCoroutine(RunCountdownAttacksSequentially(ready));
        }

        /// <summary>
        /// Runs after the player discards their hand for ending the turn.
        /// Includes end-turn attackers and eligible countdown attackers.
        /// </summary>
        public void ResolveEndTurnAttacks()
        {
            if (player == null)
                return;

            var actors = new List<EnemyBattleUnit>();
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                bool isEndTurnAttacker =
                    enemy.Behavior == EnemyBehaviorType.EndTurnAttacker && !enemy.HasAttackedThisPlayerRound;
                bool isEligibleCountdownAttacker =
                    enemy.Behavior == EnemyBehaviorType.CountdownAttacker && enemy.CanExecuteCountdownAttackAtEndTurn();

                if (!isEndTurnAttacker && !isEligibleCountdownAttacker)
                    continue;

                actors.Add(enemy);
            }

            actors.Sort((a, b) => b.Speed.CompareTo(a.Speed));
            if (runningEnemyActions != null)
                return;

            runningEnemyActions = StartCoroutine(RunEndTurnAttacksSequentially(actors));
        }

        private IEnumerator RunCountdownAttacksSequentially(List<EnemyBattleUnit> ready)
        {
            for (int i = 0; i < ready.Count; i++)
            {
                var enemy = ready[i];
                if (enemy == null)
                    continue;

                yield return enemy.ExecuteCountdownAttackRoutine(player);
            }

            runningEnemyActions = null;
        }

        private IEnumerator RunEndTurnAttacksSequentially(List<EnemyBattleUnit> actors)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                var enemy = actors[i];
                if (enemy == null)
                    continue;

                if (enemy.Behavior == EnemyBehaviorType.CountdownAttacker)
                    yield return enemy.ExecuteEndTurnCountdownAttackRoutine(player);
                else
                    yield return enemy.ExecuteEndTurnAttackRoutine(player);
            }

            runningEnemyActions = null;
        }
    }
}
