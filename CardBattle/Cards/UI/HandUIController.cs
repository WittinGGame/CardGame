using System.Collections;
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
        [SerializeField] private RectTransform drawSpawnAnchor;

        [Header("Options")]
        [SerializeField] private bool autoRefreshOnStart = true;
        [SerializeField] private bool disableUnplayableCards = true;
        [SerializeField] private bool verboseLogs = false;
        [SerializeField] private bool useDealPresentation = true;
        [SerializeField] private bool dealLeftToRight = true;
        [SerializeField] private float dealStagger = 0.05f;
        [SerializeField] private float newCardSpawnRotationZ = 0f;
        [SerializeField] private float newCardSpawnScale = 0.92f;

        [Header("Fan layout")]
        [SerializeField] private float spacing = 135f;
        [SerializeField] private float curveHeight = 14f;
        [SerializeField] private float rotationStep = 6f;
        [SerializeField] private float hoverGap = 90f;
        [SerializeField] private float layoutLerpSpeed = 12f;

        private readonly List<CardViewUI> spawnedCards = new List<CardViewUI>();
        private CardViewUI selectedView;
        private CardViewUI hoveredCardView;
        private Coroutine dealRoutine;

        private void OnEnable()
        {
            if (deckController != null)
                deckController.OnPilesChanged += SyncHandViews;

            if (player != null)
            {
                player.OnApChangedEvent += HandlePlayerApChanged;
                player.OnTurnStateChanged += HandlePlayerTurnStateChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged += HandleBusyStateChanged;
        }

        private void OnDisable()
        {
            if (deckController != null)
                deckController.OnPilesChanged -= SyncHandViews;

            if (player != null)
            {
                player.OnApChangedEvent -= HandlePlayerApChanged;
                player.OnTurnStateChanged -= HandlePlayerTurnStateChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged -= HandleBusyStateChanged;

            if (dealRoutine != null)
                StopDealRoutineAndReleaseLocks();
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
            selectedView = null;
            hoveredCardView = null;

            var hand = deckController.Hand;
            var newlyCreatedViews = new List<CardViewUI>(hand.Count);
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                    continue;

                var view = CreateCardView(card);
                spawnedCards.Add(view);
                newlyCreatedViews.Add(view);
            }

            bool shouldPlayDeal = useDealPresentation && newlyCreatedViews.Count > 0;
            if (shouldPlayDeal)
                PrepareNewCardsForDeal(newlyCreatedViews);

            RefreshCardInteractivity();
            LayoutCards();

            if (shouldPlayDeal)
                dealRoutine = StartCoroutine(CoDealInNewCards(newlyCreatedViews));
        }

        /// <summary>Syncs list of card views with the deck hand without rebuilding views for cards that are still in hand.</summary>
        private void SyncHandViews()
        {
            if (!ValidateReferences())
                return;

            StopDealRoutineAndReleaseLocks();

            for (int i = spawnedCards.Count - 1; i >= 0; i--)
            {
                var view = spawnedCards[i];
                if (view == null)
                {
                    spawnedCards.RemoveAt(i);
                    continue;
                }

                var bound = view.BoundCard;
                if (bound == null || bound.Data == null || !deckController.IsInHand(bound))
                    RemoveView(view);
            }

            var hand = deckController.Hand;
            var used = new HashSet<CardViewUI>();
            var newOrder = new List<CardViewUI>(hand.Count);
            var newlyCreatedViews = new List<CardViewUI>();

            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                    continue;

                CardViewUI view = null;
                for (int j = 0; j < spawnedCards.Count; j++)
                {
                    var candidate = spawnedCards[j];
                    if (candidate != null && !used.Contains(candidate) && candidate.BoundCard == card)
                    {
                        view = candidate;
                        break;
                    }
                }

                if (view != null)
                    used.Add(view);
                else
                {
                    view = CreateCardView(card);
                    newlyCreatedViews.Add(view);
                }

                newOrder.Add(view);
            }

            spawnedCards.Clear();
            spawnedCards.AddRange(newOrder);

            bool shouldPlayDeal = useDealPresentation && newlyCreatedViews.Count > 0;
            if (shouldPlayDeal)
                PrepareNewCardsForDeal(newlyCreatedViews);

            RefreshCardInteractivity();
            LayoutCards();

            if (shouldPlayDeal)
                dealRoutine = StartCoroutine(CoDealInNewCards(newlyCreatedViews));
        }

        private bool HasViewForCard(CardInstance card)
        {
            if (card == null)
                return false;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var v = spawnedCards[i];
                if (v != null && v.BoundCard == card)
                    return true;
            }

            return false;
        }

        /// <summary>Returns the visible hand view for a card, if any (for presentation VFX before pile sync removes it).</summary>
        public CardViewUI GetViewForCard(CardInstance card)
        {
            if (card == null)
                return null;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var v = spawnedCards[i];
                if (v != null && v.BoundCard == card)
                    return v;
            }

            return null;
        }

        /// <summary>Copy of current hand views for batch graveyard VFX (call before discard removes them).</summary>
        public List<CardViewUI> GetCurrentHandViewsSnapshot()
        {
            var list = new List<CardViewUI>(spawnedCards.Count);
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var v = spawnedCards[i];
                if (v != null)
                    list.Add(v);
            }

            return list;
        }

        private CardViewUI CreateCardView(CardInstance card)
        {
            var view = Instantiate(cardViewPrefab, handContainer);

            view.Bind(card);
            view.SetLayoutLerpSpeed(layoutLerpSpeed);
            SetupCardView(view, card);

            view.OnHoverStarted += HandleCardHoverStarted;
            view.OnHoverEnded += HandleCardHoverEnded;

            return view;
        }

        private void RemoveView(CardViewUI view)
        {
            if (view == null)
                return;

            view.OnHoverStarted -= HandleCardHoverStarted;
            view.OnHoverEnded -= HandleCardHoverEnded;

            if (hoveredCardView == view)
                hoveredCardView = null;

            if (selectedView == view)
            {
                selectedView = null;
                view.Deselect();
            }

            spawnedCards.Remove(view);
            Destroy(view.gameObject);
        }

        private void SetupCardView(CardViewUI view, CardInstance card)
        {
            if (view == null || card?.Data == null)
                return;

            view.SetClickAction(() =>
            {
                if (!view.IsInteractable)
                    return;

                SelectView(view);
                TryPlayCardFromView(card);
            });
        }

        private void HandleCardHoverStarted(CardViewUI view)
        {
            hoveredCardView = view;
            LayoutCards();
        }

        private void HandleCardHoverEnded(CardViewUI view)
        {
            if (hoveredCardView == view)
                hoveredCardView = null;

            LayoutCards();
        }

        private CardViewUI GetFocusedCardView()
        {
            if (hoveredCardView != null &&
                spawnedCards.Contains(hoveredCardView) &&
                hoveredCardView.IsPointerOver)
            {
                return hoveredCardView;
            }

            if (selectedView != null && spawnedCards.Contains(selectedView))
                return selectedView;

            return null;
        }

        private int GetFocusedCardIndex()
        {
            var focused = GetFocusedCardView();
            return focused != null ? spawnedCards.IndexOf(focused) : -1;
        }

        /// <summary>Places cards in a fan; opens a gap at the focused card (hover takes priority over selection).</summary>
        private void LayoutCards()
        {
            var container = handContainer as RectTransform;
            if (container == null || spawnedCards.Count == 0)
                return;

            int count = spawnedCards.Count;
            float centerIndex = count > 1 ? (count - 1) * 0.5f : 0f;

            int focusedIndex = GetFocusedCardIndex();

            for (int i = 0; i < count; i++)
            {
                var view = spawnedCards[i];
                if (view == null)
                    continue;

                float relative = i - centerIndex;

                float x = relative * spacing;
                float y = -curveHeight * relative * relative;

                if (focusedIndex >= 0)
                {
                    if (i < focusedIndex)
                        x -= hoverGap * 0.5f;
                    else if (i > focusedIndex)
                        x += hoverGap * 0.5f;
                }

                float rotZ = -relative * rotationStep;

                view.SetLayoutPose(new Vector2(x, y), rotZ);
            }
        }

        private void SelectView(CardViewUI view)
        {
            if (view == null)
                return;

            if (selectedView != null && selectedView != view)
                selectedView.Deselect();

            selectedView = view;
            selectedView.Select();
            LayoutCards();
        }

        public void DeselectCurrentCard()
        {
            if (selectedView != null)
            {
                var previouslySelected = selectedView;
                previouslySelected.Deselect();
                selectedView = null;

                if (hoveredCardView == previouslySelected && !previouslySelected.IsPointerOver)
                    hoveredCardView = null;
            }

            LayoutCards();
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

                    // สำคัญ: อย่า RefreshHandUI ตรงนี้
                    // เพื่อให้ selected state ค้างอยู่
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
        }

        private void HandlePlayerApChanged(int currentAp, int maxAp)
        {
            RefreshCardInteractivity();
        }

        private void HandlePlayerTurnStateChanged(bool canAct)
        {
            RefreshCardInteractivity();
        }

        private void HandleBusyStateChanged(bool isBusy)
        {
            RefreshCardInteractivity();
        }

        private void RefreshCardInteractivity()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                if (view == null || view.BoundCard?.Data == null)
                    continue;

                var card = view.BoundCard;

                bool canPlay = player != null &&
                               player.CanAct &&
                               player.CanSpendAp(card.Data.ApCost) &&
                               deckController != null &&
                               deckController.IsInHand(card);

                if (battleActionRunner != null)
                    canPlay = canPlay && battleActionRunner.CanAcceptInput;

                if (disableUnplayableCards)
                    view.SetInteractable(canPlay);
                else
                    view.SetInteractable(true);
            }
        }

        public void RefreshInteractivityExternal()
        {
            RefreshCardInteractivity();
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
            hoveredCardView = null;

            if (dealRoutine != null)
                StopDealRoutineAndReleaseLocks();

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var v = spawnedCards[i];
                if (v != null)
                {
                    v.OnHoverStarted -= HandleCardHoverStarted;
                    v.OnHoverEnded -= HandleCardHoverEnded;
                    Destroy(v.gameObject);
                }
            }

            spawnedCards.Clear();
        }

        private IEnumerator CoDealInNewCards(List<CardViewUI> newViews)
        {
            if (newViews == null || newViews.Count == 0)
            {
                dealRoutine = null;
                yield break;
            }

            newViews.Sort((a, b) =>
            {
                int ia = spawnedCards.IndexOf(a);
                int ib = spawnedCards.IndexOf(b);
                return ia.CompareTo(ib);
            });

            if (!dealLeftToRight)
                newViews.Reverse();

            float stagger = Mathf.Max(0f, dealStagger);

            for (int i = 0; i < newViews.Count; i++)
            {
                var view = newViews[i];
                if (view != null)
                    view.SetLayoutMovementBlocked(false);

                if (stagger > 0f && i < newViews.Count - 1)
                    yield return new WaitForSeconds(stagger);
            }

            dealRoutine = null;
        }

        private void PrepareNewCardsForDeal(List<CardViewUI> newViews)
        {
            Vector2 spawnPos = GetDealSpawnAnchoredPosition();
            for (int i = 0; i < newViews.Count; i++)
            {
                var view = newViews[i];
                if (view == null)
                    continue;

                view.SetLayoutMovementBlocked(true);
                view.PrepareForDealIn(spawnPos, newCardSpawnRotationZ, newCardSpawnScale);
            }
        }

        private void StopDealRoutineAndReleaseLocks()
        {
            if (dealRoutine != null)
            {
                StopCoroutine(dealRoutine);
                dealRoutine = null;
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                if (view != null)
                    view.SetLayoutMovementBlocked(false);
            }
        }

        private Vector2 GetDealSpawnAnchoredPosition()
        {
            var containerRect = handContainer as RectTransform;
            if (containerRect == null)
                return Vector2.zero;

            if (drawSpawnAnchor == null)
                return containerRect.rect.center;

            Canvas canvas = containerRect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, drawSpawnAnchor.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, screenPoint, cam, out var local))
                return local;

            return containerRect.rect.center;
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