================================================================================
FILE: BattleTestBootstrap.cs
PATH: Assets/Scripts/CardBattle/Battle/Bootstrap/BattleTestBootstrap.cs
================================================================================
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

            deckController.BuildFromInspectorBlueprint();
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

================================================================================
FILE: BattleActionRunner.cs
PATH: Assets/Scripts/CardBattle/Battle/Systems/BattleActionRunner.cs
================================================================================
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

            if (pendingPlayerCardContext != null)
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
    }
}

================================================================================
FILE: EnemyActionSystem.cs
PATH: Assets/Scripts/CardBattle/Battle/Systems/EnemyActionSystem.cs
================================================================================
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
        private Coroutine runningEnemyActions;

        public PlayerBattleUnit Player => player;
        public IReadOnlyList<EnemyBattleUnit> Enemies => enemies;
        public bool IsResolvingEnemyActions => runningEnemyActions != null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            enemies.RemoveAll(e => e == null);
        }
#endif

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

================================================================================
FILE: TargetSelectionSystem.cs
PATH: Assets/Scripts/CardBattle/Battle/Systems/TargetSelectionSystem.cs
================================================================================
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class TargetSelectionSystem : MonoBehaviour
    {
        public enum GuideStartSource
        {
            Card,
            Player
        }

        [SerializeField] private GuideStartSource guideStartSource = GuideStartSource.Card;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private EnemyTargetHighlight[] enemyHighlights;
        [SerializeField] private TargetGuideLineUI guideLine;
        [SerializeField] private RectTransform handContainer;
        [SerializeField] private float handPaddingX = 30f;
        [SerializeField] private float handPaddingY = 20f;

        public bool IsSelectingTarget => _pendingCard != null;

        private CardInstance _pendingCard;
        private RectTransform _selectedCardRect;
        private RectTransform _selectedCardGuideStartAnchor;
        private bool _canShowGuideLine;

        private void Update()
        {
            if (!IsSelectingTarget)
                return;

            // คลิกขวา
            if (Input.GetMouseButtonDown(1))
            {
                CancelTargetSelection();
                return;
            }

            // กด ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargetSelection();
                return;
            }

            bool insideSelectedCard = IsPointerInsideHandArea();
            if (insideSelectedCard)
            {
                _canShowGuideLine = false;
                if (guideLine != null)
                    guideLine.Hide();
                return;
            }

            if (!_canShowGuideLine)
            {
                _canShowGuideLine = true;
                ShowGuideLineFromCurrentStartSource();
            }

            if (_canShowGuideLine && guideLine != null)
            {
                Transform hoveredEnemyAnchor = GetHoveredEnemyAnchor();
                if (hoveredEnemyAnchor != null)
                    guideLine.UpdateTowardEnemy(hoveredEnemyAnchor);
                else
                    guideLine.UpdateTowardScreen(Input.mousePosition);
            }
        }

        /// <summary>This flow only supports picking one enemy; gate entry by card rules.</summary>
        private bool CanUseSingleEnemySelection(CardData data)
        {
            if (data == null)
                return false;

            if (data.HasEffects)
                return data.TargetMode == CardTargetMode.SingleEnemy;

            return data.CardType == CardType.Attack;
        }

        public void BeginTargetSelection(CardInstance card)
        {
            if (card?.Data == null)
                return;

            if (!CanUseSingleEnemySelection(card.Data))
                return;

            _pendingCard = card;
            _canShowGuideLine = false;
            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;

            SetHighlight(true);

            if (handUIController != null)
            {
                var view = handUIController.GetViewForCard(card);
                if (view != null)
                {
                    _selectedCardRect = view.LayoutRect;
                    _selectedCardGuideStartAnchor = view.GuideStartAnchor != null
                        ? view.GuideStartAnchor
                        : null;
                }
            }

            if (guideLine != null)
                guideLine.Hide();

            Debug.Log($"Selecting target for card: {card.Data.DisplayName}");
        }

        public void CancelTargetSelection()
        {
            if (_pendingCard == null)
                return;

            Debug.Log("Target selection cancelled.");

            _pendingCard = null;

            SetHighlight(false);

            if (handUIController != null)
                handUIController.DeselectCurrentCard();

            if (guideLine != null)
                guideLine.Hide();

            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;
            _canShowGuideLine = false;
        }

        public void ConfirmTarget(EnemyBattleUnit target)
        {
            if (_pendingCard == null || battleActionRunner == null || target == null || !target.IsAlive)
                return;

            SetHighlight(false);

            battleActionRunner.TryPlayCard(_pendingCard, target);
            _pendingCard = null;

            if (guideLine != null)
                guideLine.Hide();

            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;
            _canShowGuideLine = false;
        }

        private void SetHighlight(bool value)
        {
            if (enemyHighlights == null)
                return;

            for (int i = 0; i < enemyHighlights.Length; i++)
            {
                if (enemyHighlights[i] != null)
                    enemyHighlights[i].SetSelectable(value);
            }
        }

        private Transform GetHoveredEnemyAnchor()
        {
            if (EventSystem.current == null)
                return null;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                var go = raycastResults[i].gameObject;
                if (go == null)
                    continue;

                var unit = go.GetComponentInParent<EnemyBattleUnit>();
                if (unit != null && unit.IsAlive)
                {
                    if (unit.UIAnchorTargetGuide != null)
                        return unit.UIAnchorTargetGuide;

                    if (unit.UIAnchorDamage != null)
                        return unit.UIAnchorDamage;

                    if (unit.UIAnchorHP != null)
                        return unit.UIAnchorHP;

                    return unit.transform;
                }
            }

            // TODO: Swap to a dedicated hover-tracking source if introduced later.
            return null;
        }

        private bool IsPointerInsideHandArea()
        {
            if (handContainer == null)
                return false;

            Canvas canvas = handContainer.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            handContainer.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);

            min.x -= handPaddingX;
            min.y -= handPaddingY;
            max.x += handPaddingX;
            max.y += handPaddingY;

            Vector2 mouse = Input.mousePosition;
            return mouse.x >= min.x && mouse.x <= max.x &&
                   mouse.y >= min.y && mouse.y <= max.y;
        }

        private void ShowGuideLineFromCurrentStartSource()
        {
            if (guideLine == null)
                return;

            if (guideStartSource == GuideStartSource.Card)
            {
                RectTransform startAnchor = _selectedCardGuideStartAnchor != null
                    ? _selectedCardGuideStartAnchor
                    : _selectedCardRect;

                if (startAnchor != null)
                    guideLine.ShowFromCard(startAnchor);
            }
            else
            {
                if (player != null)
                {
                    Transform startWorld = player.UIAnchorTargetGuide != null
                        ? player.UIAnchorTargetGuide
                        : player.transform;

                    if (startWorld != null)
                        guideLine.ShowFromWorld(startWorld);
                }
            }
        }
    }
}

================================================================================
FILE: BattleUnit.cs
PATH: Assets/Scripts/CardBattle/Battle/Units/BattleUnit.cs
================================================================================
using System;
using UnityEngine;

namespace CardBattle.Core
{
    public abstract class BattleUnit : MonoBehaviour
    {
        [SerializeField] protected int maxHp = 10;
        [SerializeField] protected int currentHp;
        protected int currentBlock;

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public int CurrentBlock => currentBlock;
        public bool IsAlive => currentHp > 0;

        public event Action<int, int> OnHpChangedEvent;
        public event Action<int> OnBlockChangedEvent;

        public event Action<BattleUnit, int> OnDamageTakenEvent;
        public event Action<BattleUnit, int> OnHealedEvent;

        protected virtual void Awake()
        {
            if (currentHp <= 0)
                currentHp = maxHp;

            NotifyHpChanged();
        }

        /// <returns>Damage applied to HP after block (not raw incoming amount).</returns>
        public virtual int TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return 0;

            int remaining = amount;

            if (currentBlock > 0)
            {
                int absorbed = Mathf.Min(currentBlock, remaining);
                currentBlock -= absorbed;
                remaining -= absorbed;
                NotifyBlockChanged();
            }

            int hpDamage = 0;
            if (remaining > 0)
            {
                hpDamage = remaining;
                currentHp = Mathf.Max(0, currentHp - hpDamage);
                OnHpChanged();
                NotifyHpChanged();
            }

            OnDamageTakenEvent?.Invoke(this, hpDamage);

            if (currentHp == 0)
                OnDefeated();

            return hpDamage;
        }

        public virtual void AddBlock(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentBlock += amount;
            NotifyBlockChanged();
        }

        public virtual void ClearBlock()
        {
            if (currentBlock == 0)
                return;

            currentBlock = 0;
            NotifyBlockChanged();
        }

        private void NotifyBlockChanged()
        {
            OnBlockChangedEvent?.Invoke(currentBlock);
        }

        public virtual void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentHp = Mathf.Min(maxHp, currentHp + amount);
            OnHpChanged();
            NotifyHpChanged();
            OnHealedEvent?.Invoke(this, amount);
        }

        public virtual void SetMaxHp(int value, bool refillToMax = false)
        {
            maxHp = Mathf.Max(1, value);

            if (refillToMax)
                currentHp = maxHp;
            else
                currentHp = Mathf.Min(currentHp, maxHp);

            OnHpChanged();
            NotifyHpChanged();
        }

        protected virtual void OnHpChanged() { }
        protected virtual void OnDefeated() { }

        private void NotifyHpChanged()
        {
            OnHpChangedEvent?.Invoke(currentHp, maxHp);
        }
    }
}

================================================================================
FILE: EnemyBattleUnit.cs
PATH: Assets/Scripts/CardBattle/Battle/Units/EnemyBattleUnit.cs
================================================================================
using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Enemy-specific state: behavior pattern, countdown, and per-player-round attack tracking.
    /// </summary>
    public class EnemyBattleUnit : BattleUnit
    {
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private BattleUnitView battleUnitView;

        [Header("UI")]
        [SerializeField] private Transform uiAnchorHP;
        [SerializeField] private Transform uiAnchorIntent;
        [SerializeField] private Transform uiAnchorBuff;
        [SerializeField] private Transform uiAnchorDamage;
        [SerializeField] private Transform uiAnchorTargetGuide;

        public BattleUnitView View => battleUnitView;
        public Transform UIAnchorHP => uiAnchorHP;
        public Transform UIAnchorIntent => uiAnchorIntent;
        public Transform UIAnchorBuff => uiAnchorBuff;
        public Transform UIAnchorDamage => uiAnchorDamage;
        public Transform UIAnchorTargetGuide => uiAnchorTargetGuide;

        private int _countdown;
        private bool _hasAttackedThisPlayerRound;
        private bool waitingForHit;
        private bool waitingForFinish;
        private PlayerBattleUnit pendingTarget;
        private bool attackInProgress;
        public event System.Action OnEnemyStateChanged;

        public EnemyData Data => enemyData;
        public EnemyBehaviorType Behavior => enemyData != null ? enemyData.Behavior : EnemyBehaviorType.EndTurnAttacker;
        public int Speed => enemyData != null ? enemyData.Speed : 0;
        public int CurrentCountdown => _countdown;
        public bool HasAttackedThisPlayerRound => _hasAttackedThisPlayerRound;
        public bool IsAttackInProgress => attackInProgress;
        public bool AllowEndTurnAttackAfterCountdownAttackThisRound =>
            enemyData != null && enemyData.AllowEndTurnAttackAfterCountdownAttackThisRound;

        protected override void Awake()
        {
            base.Awake();
            ApplyEnemyData();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (enemyData != null)
                ApplyEnemyData();
        }
#endif

        /// <summary>Swap template at runtime (e.g. encounter scripting).</summary>
        public void BindEnemyData(EnemyData data)
        {
            enemyData = data;
            ApplyEnemyData();
        }

        private void ApplyEnemyData()
        {
            if (enemyData == null)
                return;

            SetMaxHp(enemyData.MaxHp, true);
            _countdown = enemyData.BaseCountdown;
        }

        /// <summary>Reset flags when a new player round begins.</summary>
        public void ResetRoundCombatFlags()
        {
            _hasAttackedThisPlayerRound = false;
            NotifyStateChanged();
        }

        /// <summary>Countdown attackers lose one tick after each successful player card.</summary>
        public void StepCountdownAfterPlayerCard()
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.CountdownAttacker)
                return;

            if (_countdown > 0)
            {
                _countdown--;
                NotifyStateChanged();
            }
        }

        /// <summary>True immediately after stepping when this unit should interrupt the player.</summary>
        public bool IsCountdownReady => Behavior == EnemyBehaviorType.CountdownAttacker && IsAlive && _countdown <= 0;

        /// <summary>Perform the interrupt attack, mark round flag, and reload countdown.</summary>
        public void ExecuteCountdownAttack(PlayerBattleUnit player)
        {
            if (!IsCountdownReady)
                return;

            StartCoroutine(ExecuteCountdownAttackRoutine(player));
        }

        public IEnumerator ExecuteCountdownAttackRoutine(PlayerBattleUnit player)
        {
            if (!IsCountdownReady)
                yield break;

            yield return PerformStrike(player);
            _countdown = enemyData != null ? enemyData.BaseCountdown : 0;
            NotifyStateChanged();
        }

        public bool CanExecuteCountdownAttackAtEndTurn()
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.CountdownAttacker)
                return false;

            if (!_hasAttackedThisPlayerRound)
                return true;

            return AllowEndTurnAttackAfterCountdownAttackThisRound;
        }

        public IEnumerator ExecuteEndTurnCountdownAttackRoutine(PlayerBattleUnit player)
        {
            if (!CanExecuteCountdownAttackAtEndTurn())
                yield break;

            // End-turn rule: eligible countdown attackers can force-ready and strike now.
            _countdown = 0;
            NotifyStateChanged();
            yield return ExecuteCountdownAttackRoutine(player);
        }

        /// <summary>End-of-turn attack for <see cref="EnemyBehaviorType.EndTurnAttacker"/>.</summary>
        public void ExecuteEndTurnAttack(PlayerBattleUnit player)
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.EndTurnAttacker)
                return;

            if (_hasAttackedThisPlayerRound)
                return;

            StartCoroutine(ExecuteEndTurnAttackRoutine(player));
        }

        public IEnumerator ExecuteEndTurnAttackRoutine(PlayerBattleUnit player)
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.EndTurnAttacker)
                yield break;

            if (_hasAttackedThisPlayerRound)
                yield break;

            yield return PerformStrike(player);
        }

        private IEnumerator PerformStrike(PlayerBattleUnit player)
        {
            if (attackInProgress || player == null || !player.IsAlive)
                yield break;

            attackInProgress = true;
            pendingTarget = player;
            waitingForHit = true;
            waitingForFinish = true;

            if (View != null)
            {
                SubscribeToViewEvents();
                View.PlayAttack();

                yield return new WaitUntil(() => !waitingForFinish);
                UnsubscribeFromViewEvents();
            }
            else
            {
                ApplyDamageOnHit();
                waitingForFinish = false;
            }

            waitingForHit = false;
            pendingTarget = null;
            attackInProgress = false;
            NotifyStateChanged();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewEvents();
            waitingForHit = false;
            waitingForFinish = false;
            pendingTarget = null;
            attackInProgress = false;
        }

        private void SubscribeToViewEvents()
        {
            if (View == null)
                return;

            UnsubscribeFromViewEvents();
            View.OnAttackHit += HandleAttackHit;
            View.OnActionFinished += HandleActionFinished;
        }

        private void UnsubscribeFromViewEvents()
        {
            if (View == null)
                return;

            View.OnAttackHit -= HandleAttackHit;
            View.OnActionFinished -= HandleActionFinished;
        }

        private void HandleAttackHit()
        {
            if (!waitingForHit)
                return;

            waitingForHit = false;
            ApplyDamageOnHit();
        }

        private void HandleActionFinished()
        {
            if (!waitingForFinish)
                return;

            waitingForFinish = false;
        }

        private void ApplyDamageOnHit()
        {
            if (pendingTarget == null || !pendingTarget.IsAlive)
                return;

            var damage = enemyData != null ? enemyData.AttackDamage : 0;
            bool wasAliveBeforeHit = pendingTarget.IsAlive;

            int hpDamage = pendingTarget.TakeDamage(damage);

            if (wasAliveBeforeHit)
            {
                if (!pendingTarget.IsAlive)
                    pendingTarget.View?.PlayDead();
                else if (hpDamage > 0)
                    pendingTarget.View?.PlayHurt();
            }

            _hasAttackedThisPlayerRound = true;
        }

        private void NotifyStateChanged()
        {
            OnEnemyStateChanged?.Invoke();
        }
    }
}

================================================================================
FILE: PlayerBattleUnit.cs
PATH: Assets/Scripts/CardBattle/Battle/Units/PlayerBattleUnit.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Player-facing state: AP pool, deck access, and hooks buff cards can influence.
    /// Turn flow: <see cref="BeginRoundState"/> → play cards until out of AP or <see cref="RequestEndTurn"/> → enemies react.
    /// </summary>
    public class PlayerBattleUnit : BattleUnit
    {
        [Header("Turn Rules")]
        [SerializeField] private int apPerRound = 3;
        [SerializeField] private int drawPerRound = 5;

        [Header("Systems")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private BattleUnitView battleUnitView;
        [SerializeField] private Transform uiAnchorTargetGuide;
        public BattleUnitView View => battleUnitView;

        private int _pendingAttackBonus;
        private bool _turnCommitted;
        public event System.Action<int, int> OnApChangedEvent;
        public event System.Action<bool> OnTurnStateChanged;
        public event System.Action<int> OnDebugBuffChanged;

        public int CurrentAp { get; private set; }
        public int ApPerRound => apPerRound;
        public int DrawPerRound => drawPerRound;
        public bool HasCommittedTurn => _turnCommitted;
        public bool CanAct => !_turnCommitted && IsAlive;

        public DeckController DeckController => deckController;
        public Transform UIAnchorTargetGuide => uiAnchorTargetGuide;
        public int DebugBuffValue => _pendingAttackBonus;
        public int DebugBuffCount => _pendingAttackBonus > 0 ? 1 : 0;

        /// <summary>True when the player may attempt to spend AP on a card.</summary>
        public bool CanSpendAp(int amount) => CanAct && CurrentAp >= amount;

        /// <summary>Reset AP, unlock input, and clear transient modifiers at the start of the player's round.</summary>
        public void BeginRoundState()
        {
            _turnCommitted = false;
            CurrentAp = Mathf.Max(0, apPerRound);
            _pendingAttackBonus = 0;
            OnDebugBuffChanged?.Invoke(DebugBuffCount);
            ClearBlock();
            NotifyApChanged();
            NotifyTurnStateChanged();
        }

        /// <summary>
        /// Attempts to play a card: spends AP, moves it to the graveyard, resolves effects, then notifies enemies.
        /// Returns false if the turn is locked, the card is not in hand, or AP is insufficient.
        /// </summary>
        public bool TryPlayCard(CardInstance card, EnemyBattleUnit primaryTarget = null)
        {
            if (!CanAct || card?.Data == null)
                return false;

            if (deckController == null || cardResolver == null || enemyActionSystem == null)
            {
                Debug.LogError("PlayerBattleUnit missing one of its serialized systems.");
                return false;
            }

            if (!deckController.IsInHand(card))
                return false;

            var cost = card.Data.ApCost;
            if (!CanSpendAp(cost))
                return false;

            CurrentAp -= cost;
            NotifyApChanged();
            deckController.PlayCardFromHand(card);

            var context = new CardPlayContext(this, card, enemyActionSystem.Enemies, primaryTarget);
            cardResolver.Resolve(context);

            enemyActionSystem.HandlePlayerSuccessfullyPlayedCard();
            return true;
        }

        /// <summary>
        /// Locks further plays, discards the hand, then lets end-of-turn enemies strike.
        /// Countdown enemies that already attacked this round are skipped automatically.
        /// </summary>
        public void RequestEndTurn()
        {
            if (!IsAlive || _turnCommitted)
                return;

            if (deckController == null || enemyActionSystem == null)
            {
                Debug.LogError("PlayerBattleUnit missing deck or enemy system references.");
                return;
            }

            _turnCommitted = true;
            NotifyTurnStateChanged();
            deckController.DiscardEntireHand();
            enemyActionSystem.ResolveEndTurnAttacks();
        }

        /// <summary>Buff cards add to the next attack's damage; consumed when an attack card resolves.</summary>
        public void ApplyBuffFromCard(CardData data)
        {
            if (data == null)
                return;

            _pendingAttackBonus += Mathf.Max(0, data.BuffPotency);
            OnDebugBuffChanged?.Invoke(DebugBuffCount);
        }

        /// <summary>Called by <see cref="CardResolver"/> when applying attack damage.</summary>
        public int ConsumeDamageBonus()
        {
            var bonus = _pendingAttackBonus;
            ConsumeNextAttackBonus();
            return bonus;
        }

        public void ConsumeNextAttackBonus()
        {
            _pendingAttackBonus = 0;
            OnDebugBuffChanged?.Invoke(DebugBuffCount);
        }

        public void SpendApFromRunner(int amount)
        {
            if (amount <= 0)
                return;

            CurrentAp = Mathf.Max(0, CurrentAp - amount);
            NotifyApChanged();
        }

        public void CommitEndTurnFromRunner()
        {
            if (!IsAlive || _turnCommitted)
                return;

            _turnCommitted = true;
            NotifyTurnStateChanged();

            if (deckController != null)
                deckController.DiscardEntireHand();
        }

        private void NotifyApChanged()
        {
            OnApChangedEvent?.Invoke(CurrentAp, ApPerRound);
        }

        private void NotifyTurnStateChanged()
        {
            OnTurnStateChanged?.Invoke(CanAct);
        }

        protected override void OnDefeated()
        {
            base.OnDefeated();
            NotifyTurnStateChanged();
        }
    }
}

================================================================================
FILE: BattleUnitView.cs
PATH: Assets/Scripts/CardBattle/Battle/Views/BattleUnitView.cs
================================================================================
using System;
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleUnitView : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        public event Action OnAttackHit;
        public event Action OnActionFinished;

        public void PlayAttack()
        {
            if (animator == null) return;
            animator.SetTrigger(AttackHash);
        }

        public void PlayHurt()
        {
            if (animator == null) return;
            animator.SetTrigger(HurtHash);
        }

        public void PlayDead()
        {
            if (animator == null) return;
            animator.SetTrigger(DeadHash);
        }

        // Animation Event
        public void AnimEvent_AttackHit()
        {
            OnAttackHit?.Invoke();
        }

        // Animation Event
        public void AnimEvent_ActionFinished()
        {
            OnActionFinished?.Invoke();
        }
    }
}

================================================================================
FILE: BattleCameraFeedbackController.cs
PATH: Assets/Scripts/CardBattle/Battle/Presentation/BattleCameraFeedbackController.cs
================================================================================
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation bridge: listens to real damage events and triggers camera shake profiles.
    /// </summary>
    public class BattleCameraFeedbackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraShakeController cameraShake;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [Header("Toggles")]
        [SerializeField] private bool shakeOnPlayerDamage = true;
        [SerializeField] private bool shakeOnEnemyDamage = true;

        [Header("Player Damage Shake")]
        [SerializeField] private float playerDamageDuration = 0.10f;
        [SerializeField] private float playerDamageStrength = 0.05f;
        [SerializeField] private float playerDamageFrequency = 36f;

        [Header("Enemy Damage Shake")]
        [SerializeField] private float enemyDamageDuration = 0.12f;
        [SerializeField] private float enemyDamageStrength = 0.06f;
        [SerializeField] private float enemyDamageFrequency = 38f;

        private readonly List<EnemyBattleUnit> subscribedEnemies = new List<EnemyBattleUnit>();

        private void OnEnable()
        {
            SubscribePlayer();
            SubscribeEnemies();
        }

        private void OnDisable()
        {
            UnsubscribePlayer();
            UnsubscribeEnemies();
        }

        /// <summary>
        /// Call this after runtime enemy registration/spawn to resubscribe the current enemy set.
        /// </summary>
        public void RefreshEnemySubscriptions()
        {
            UnsubscribeEnemies();
            SubscribeEnemies();
        }

        private void SubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent += HandleUnitDamageTaken;
        }

        private void UnsubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent -= HandleUnitDamageTaken;
        }

        private void SubscribeEnemies()
        {
            if (enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || subscribedEnemies.Contains(enemy))
                    continue;

                enemy.OnDamageTakenEvent += HandleUnitDamageTaken;
                subscribedEnemies.Add(enemy);
            }
        }

        private void UnsubscribeEnemies()
        {
            for (int i = 0; i < subscribedEnemies.Count; i++)
            {
                var enemy = subscribedEnemies[i];
                if (enemy == null)
                    continue;

                enemy.OnDamageTakenEvent -= HandleUnitDamageTaken;
            }

            subscribedEnemies.Clear();
        }

        private void HandleUnitDamageTaken(BattleUnit unit, int amount)
        {
            if (cameraShake == null || unit == null || amount <= 0)
                return;

            if (player != null && unit == player)
            {
                if (shakeOnPlayerDamage)
                    cameraShake.Shake(playerDamageDuration, playerDamageStrength, playerDamageFrequency);
                return;
            }

            if (unit is EnemyBattleUnit)
            {
                if (shakeOnEnemyDamage)
                    cameraShake.Shake(enemyDamageDuration, enemyDamageStrength, enemyDamageFrequency);
            }
        }
    }
}

================================================================================
FILE: CameraShakeController.cs
PATH: Assets/Scripts/CardBattle/Battle/Presentation/CameraShakeController.cs
================================================================================
using System.Collections;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only camera shake utility. Knows only how to shake a transform.
    /// </summary>
    public class CameraShakeController : MonoBehaviour
    {
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private float defaultDuration = 0.12f;
        [SerializeField] private float defaultStrength = 0.06f;
        [SerializeField] private float defaultFrequency = 38f;

        private Vector3 originalLocalPosition;
        private Coroutine shakeRoutine;

        private void Awake()
        {
            if (shakeTarget == null && Camera.main != null)
                shakeTarget = Camera.main.transform;

            CacheOriginalLocalPosition();
        }

        private void OnEnable()
        {
            CacheOriginalLocalPosition();
        }

        private void OnDisable()
        {
            StopShakeRoutine();
            RestoreOriginalLocalPosition();
        }

        public void Shake()
        {
            Shake(defaultDuration, defaultStrength, defaultFrequency);
        }

        public void Shake(float duration, float strength, float frequency)
        {
            if (shakeTarget == null)
                return;

            if (shakeRoutine != null)
            {
                StopShakeRoutine();
                RestoreOriginalLocalPosition();
            }

            CacheOriginalLocalPosition();
            shakeRoutine = StartCoroutine(CoShake(duration, strength, frequency));
        }

        [ContextMenu("Test Shake")]
        private void TestShake()
        {
            Shake();
        }

        private IEnumerator CoShake(float duration, float strength, float frequency)
        {
            float clampedDuration = Mathf.Max(0.01f, duration);
            float clampedStrength = Mathf.Max(0f, strength);
            float clampedFrequency = Mathf.Max(0f, frequency);
            float elapsed = 0f;

            float seedX = Random.value * 1000f;
            float seedY = Random.value * 1000f + 100f;

            while (elapsed < clampedDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / clampedDuration);
                float fade = 1f - t;

                float sampleTime = Time.unscaledTime * clampedFrequency;
                float offsetX = (Mathf.PerlinNoise(seedX, sampleTime) - 0.5f) * 2f;
                float offsetY = (Mathf.PerlinNoise(seedY, sampleTime) - 0.5f) * 2f;
                Vector3 offset = new Vector3(offsetX, offsetY, 0f) * (clampedStrength * fade);

                shakeTarget.localPosition = originalLocalPosition + offset;
                yield return null;
            }

            RestoreOriginalLocalPosition();
            shakeRoutine = null;
        }

        private void StopShakeRoutine()
        {
            if (shakeRoutine == null)
                return;

            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        private void CacheOriginalLocalPosition()
        {
            if (shakeTarget != null)
                originalLocalPosition = shakeTarget.localPosition;
        }

        private void RestoreOriginalLocalPosition()
        {
            if (shakeTarget != null)
                shakeTarget.localPosition = originalLocalPosition;
        }
    }
}