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
        [SerializeField] private HandCardSelectionController handCardSelectionController;

        [Header("UI References")]
        [SerializeField] private Transform handContainer;
        [SerializeField] private CardViewUI cardViewPrefab;
        [SerializeField] private RectTransform drawSpawnAnchor;
        [Tooltip("Optional Canvas on Hand/HandPanel. During selection, override sorting so cards stay above the dim overlay.")]
        [SerializeField] private Canvas handSortingCanvas;
        [SerializeField] private int handSelectionSortingOrder = 50;

        [Header("Audio")]
        [SerializeField] private CardSFXController cardSfx;

        [Header("Options")]
        [SerializeField] private bool autoRefreshOnStart = true;
        [SerializeField] private bool disableUnplayableCards = true;
        [SerializeField] private bool verboseLogs = false;
        [SerializeField] private bool useDealPresentation = true;
        [SerializeField] private bool dealLeftToRight = true;
        [SerializeField] private float dealStagger = 0.05f;
        [SerializeField] private float newCardSpawnRotationZ = 0f;
        [SerializeField] private float newCardSpawnScale = 0.92f;

        [Header("Responsive Card Size")]
        [SerializeField] private Vector2 baseCardSize = new Vector2(200f, 300f);
        [SerializeField] private float maxCardScale = 1.1f; // 200 -> 220
        [SerializeField] private float minCardScale = 0.9f;
        [SerializeField] private int maxScaleCardCount = 5;
        [SerializeField] private int minScaleCardCount = 10;
        [SerializeField] private bool scaleSpacingWithCard = true;

        [Header("Fan layout")]
        [SerializeField] private float spacing = 135f;
        [SerializeField] private float curveHeight = 14f;
        [SerializeField] private float rotationStep = 6f;
        [SerializeField] private float hoverGap = 60f;
        [SerializeField] private float layoutLerpSpeed = 12f;

        [Header("Adaptive Fan Layout")]
        [SerializeField] private bool useAdaptiveFanLayout = true;
        [SerializeField] private float maxHandWidth = 1080f;
        [SerializeField] private float minimumSpacing = 90f;
        [SerializeField] private float maximumSpacing = 145f;
        [SerializeField] private float maxEdgeRotation = 8f;
        [SerializeField] private float maxEdgeDrop = 30f;
        [SerializeField] private float largeHandRaise = 55f;
        [SerializeField] private int largeHandStartCount = 8;

        [Header("Fixed Size Fan Layout")]
        [SerializeField] private bool useFixedSizeAdaptiveFan = true;
        [SerializeField] private float fixedCardScale = 1f;
        [SerializeField] private float preferredCenterSpacing = 140f;
        [SerializeField] private float minimumCenterSpacing = 80f;
        [SerializeField] private float hoverRaise = 150f;
        [SerializeField] private float hoverScale = 1.08f;

        [Header("Fan Strength By Hand Count")]
        [SerializeField] private bool useCustomFanStrengthByCount = true;
        [Tooltip("Index = card count. Values may exceed 1.0 (e.g. 1.12 = 112% of max edge drop/rotation).")]
        [SerializeField] private float[] fanStrengthByCardCount =
        {
            0f,    // 0
            0f,    // 1
            0.15f, // 2
            0.35f, // 3
            0.65f, // 4
            1.00f, // 5 baseline
            1.02f, // 6
            1.05f, // 7
            1.08f, // 8
            1.10f, // 9
            1.12f  // 10
        };

        [Header("Large Hand Hover")]
        [SerializeField] private int largeHandHoverStartCount = 8;
        [SerializeField] private float largeHandHoverGap = 85f;
        [SerializeField] private float largeHandHoverRaise = 175f;
        [SerializeField] private float largeHandHoverScale = 1.10f;
        [SerializeField] private float neighborPushFalloff = 0.55f;
        [SerializeField] private bool keepHoveredCardOnTop = true;
        [SerializeField] private float edgeHoverInwardOffset = 30f;

        private readonly List<CardViewUI> spawnedCards = new List<CardViewUI>();
        private CardViewUI selectedView;
        private CardViewUI hoveredCardView;
        private Coroutine dealRoutine;

        private bool handSelectionActive;
        private HandCardSelectionController activeHandSelection;
        private int previousHandCanvasSortingOrder;
        private bool previousHandCanvasOverrideSorting;
        private bool handCanvasSortBoosted;

        private int lastLoggedLayoutCount = -1;
        private float lastLoggedCardScale;
        private float lastLoggedCardWidth;
        private float lastLoggedPreferredSpacing;
        private float lastLoggedFitSpacing;
        private float lastLoggedSpacing;
        private float lastLoggedTotalSpan;
        private float lastLoggedTotalWidth;
        private float lastLoggedEdgeDrop;
        private float lastLoggedHandRaise;
        private float lastLoggedEdgeRotation;
        private float lastLoggedFanStrength;
        private float lastLoggedHoverGap;
        private float lastLoggedHoverRaise;
        private float lastLoggedHoverScale;
        private int lastLoggedHoveredIndex = -1;

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

        private float GetResponsiveCardScale(int count)
        {
            float t = Mathf.InverseLerp(maxScaleCardCount, minScaleCardCount, count);
            return Mathf.Lerp(maxCardScale, minCardScale, t);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxHandWidth = Mathf.Max(1f, maxHandWidth);
            minimumSpacing = Mathf.Max(0.01f, minimumSpacing);
            maximumSpacing = Mathf.Max(minimumSpacing, maximumSpacing);
            maxEdgeRotation = Mathf.Max(0f, maxEdgeRotation);
            maxEdgeDrop = Mathf.Max(0f, maxEdgeDrop);
            largeHandRaise = Mathf.Max(0f, largeHandRaise);
            largeHandStartCount = Mathf.Max(2, largeHandStartCount);
            minCardScale = Mathf.Max(0.01f, minCardScale);
            maxCardScale = Mathf.Max(minCardScale, maxCardScale);
            spacing = Mathf.Max(0.01f, spacing);
            hoverGap = Mathf.Max(0f, hoverGap);
            curveHeight = Mathf.Max(0f, curveHeight);
            layoutLerpSpeed = Mathf.Max(0f, layoutLerpSpeed);

            fixedCardScale = Mathf.Max(0.01f, fixedCardScale);
            preferredCenterSpacing = Mathf.Max(0.01f, preferredCenterSpacing);
            minimumCenterSpacing = Mathf.Max(0.01f, minimumCenterSpacing);
            if (minimumCenterSpacing > preferredCenterSpacing)
                minimumCenterSpacing = preferredCenterSpacing;
            hoverRaise = Mathf.Max(0f, hoverRaise);
            hoverScale = Mathf.Max(1f, hoverScale);

            largeHandHoverStartCount = Mathf.Max(2, largeHandHoverStartCount);
            largeHandHoverGap = Mathf.Max(hoverGap, largeHandHoverGap);
            largeHandHoverRaise = Mathf.Max(hoverRaise, largeHandHoverRaise);
            largeHandHoverScale = Mathf.Max(hoverScale, largeHandHoverScale);
            neighborPushFalloff = Mathf.Clamp01(neighborPushFalloff);
            edgeHoverInwardOffset = Mathf.Max(0f, edgeHoverInwardOffset);

            EnsureFanStrengthArraySize();
            if (fanStrengthByCardCount != null)
            {
                for (int i = 0; i < fanStrengthByCardCount.Length; i++)
                    fanStrengthByCardCount[i] = Mathf.Max(0f, fanStrengthByCardCount[i]);
            }
        }
#endif

        private void EnsureFanStrengthArraySize()
        {
            const int needed = 11; // indices 0..10
            if (fanStrengthByCardCount != null && fanStrengthByCardCount.Length >= needed)
                return;

            float[] defaults =
            {
                0f, 0f, 0.15f, 0.35f, 0.65f, 1.00f, 1.02f, 1.05f, 1.08f, 1.10f, 1.12f
            };

            if (fanStrengthByCardCount == null || fanStrengthByCardCount.Length == 0)
            {
                fanStrengthByCardCount = defaults;
                return;
            }

            var expanded = new float[needed];
            for (int i = 0; i < needed; i++)
            {
                if (i < fanStrengthByCardCount.Length)
                    expanded[i] = Mathf.Max(0f, fanStrengthByCardCount[i]);
                else
                    expanded[i] = defaults[i];
            }

            fanStrengthByCardCount = expanded;
        }

        [ContextMenu("Debug/Print Adaptive Hand Layout")]
        private void DebugPrintAdaptiveHandLayout()
        {
            if (spawnedCards.Count == 0)
            {
                Debug.Log("[AdaptiveHandLayout] No cards in hand.");
                return;
            }

            LayoutCards();
            LogAdaptiveHandLayout(force: true);
        }

        [ContextMenu("Debug/Print Fixed Fan Layout")]
        private void DebugPrintFixedFanLayout()
        {
            if (spawnedCards.Count == 0)
            {
                Debug.Log("[FixedFanLayout] No cards in hand.");
                return;
            }

            LayoutCards();
            LogFixedFanLayout(force: true);
        }

        [ContextMenu("Debug/Print Fan Strength")]
        private void DebugPrintFanStrength()
        {
            if (spawnedCards.Count == 0)
            {
                Debug.Log("[FanStrength] No cards in hand.");
                return;
            }

            LayoutCards();
            LogFanStrength(force: true);
        }

        [ContextMenu("Debug/Print Current Fan And Hover Settings")]
        private void DebugPrintCurrentFanAndHoverSettings()
        {
            if (spawnedCards.Count == 0)
            {
                Debug.Log("[HandPresentation] No cards in hand.");
                return;
            }

            LayoutCards();
            LogHandPresentation(force: true);
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

        /// <summary>
        /// Incremental sync wrapper for external systems. Preserves existing CardViews
        /// and only creates views for newly added hand cards.
        /// </summary>
        public void SyncHandViewsExternal()
        {
            SyncHandViews();
        }

        public bool IsDealPresentationRunning => dealRoutine != null;

        /// <summary>
        /// Waits until the current deal-in presentation finishes.
        /// Returns immediately when no deal is running or this component is disabled.
        /// </summary>
        public IEnumerator WaitForDealPresentationComplete()
        {
            while (dealRoutine != null && isActiveAndEnabled)
                yield return null;
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

                bool createdNew = false;
                if (view != null)
                {
                    used.Add(view);
                }
                else
                {
                    view = CreateCardView(card);
                    newlyCreatedViews.Add(view);
                    createdNew = true;
                }

                newOrder.Add(view);

                if (verboseLogs && createdNew)
                {
                    Debug.Log(
                        "[IncrementalHandDraw]\n" +
                        $"Card={card.Data.DisplayName}\n" +
                        "CreatedNewView=True\n" +
                        $"HandIndex={newOrder.Count - 1}\n" +
                        $"VisibleViews={newOrder.Count}");
                }
            }

            spawnedCards.Clear();
            spawnedCards.AddRange(newOrder);

            bool shouldPlayDeal = useDealPresentation && newlyCreatedViews.Count > 0;
            if (shouldPlayDeal)
                PrepareNewCardsForDeal(newlyCreatedViews);

            RefreshCardInteractivity();
            LayoutCards();

            if (handSelectionActive && activeHandSelection != null && activeHandSelection.IsSelecting)
                activeHandSelection.RefreshSelectionVisuals();

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
            if (useFixedSizeAdaptiveFan)
                ApplyFixedSizePresentationTuning(view, hoverRaise, hoverScale, 0f);
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
                if (handSelectionActive)
                {
                    HandleHandSelectionClick(view, card);
                    return;
                }

                if (!view.IsInteractable)
                    return;

                SelectView(view);
                TryPlayCardFromView(card);
            });
        }

        private void HandleHandSelectionClick(CardViewUI view, CardInstance card)
        {
            if (activeHandSelection == null || card == null)
                return;

            activeHandSelection.TryToggleCard(card);
        }

        /// <summary>Enter hand selection mode without rebuilding card views.</summary>
        public void BeginHandSelection(HandCardSelectionController controller)
        {
            activeHandSelection = controller;
            handSelectionActive = controller != null && controller.IsSelecting;
            DeselectCurrentCard();
            BoostHandCanvasSorting(true);
            RefreshCardInteractivity();
        }

        public void EndHandSelection()
        {
            handSelectionActive = false;
            activeHandSelection = null;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                if (view != null)
                    view.ClearHandSelectionState();
            }

            BoostHandCanvasSorting(false);
            RefreshCardInteractivity();
        }

        public void RefreshHandSelectionVisuals(
            IReadOnlyList<CardInstance> candidates,
            IReadOnlyList<CardInstance> selected,
            int requiredCount)
        {
            handSelectionActive = true;
            int selectedCount = selected != null ? selected.Count : 0;
            bool selectionFull = selectedCount >= requiredCount && requiredCount > 0;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                if (view == null)
                    continue;

                var card = view.BoundCard;
                bool isCandidate = card != null && candidates != null && ContainsCard(candidates, card);
                bool isSelected = card != null && selected != null && ContainsCard(selected, card);
                bool selectable = isCandidate && (isSelected || !selectionFull);

                view.SetHandSelectionState(true, isSelected, selectable);
            }
        }

        private static bool ContainsCard(IReadOnlyList<CardInstance> list, CardInstance card)
        {
            if (list == null || card == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == card)
                    return true;
            }

            return false;
        }

        private void BoostHandCanvasSorting(bool enable)
        {
            if (handSortingCanvas == null)
                return;

            if (enable)
            {
                if (!handCanvasSortBoosted)
                {
                    previousHandCanvasOverrideSorting = handSortingCanvas.overrideSorting;
                    previousHandCanvasSortingOrder = handSortingCanvas.sortingOrder;
                    handCanvasSortBoosted = true;
                }

                handSortingCanvas.overrideSorting = true;
                handSortingCanvas.sortingOrder = handSelectionSortingOrder;
            }
            else if (handCanvasSortBoosted)
            {
                handSortingCanvas.overrideSorting = previousHandCanvasOverrideSorting;
                handSortingCanvas.sortingOrder = previousHandCanvasSortingOrder;
                handCanvasSortBoosted = false;
            }
        }

        private void HandleCardHoverStarted(CardViewUI view)
        {
            if (view == null || hoveredCardView == view)
                return;

            hoveredCardView = view;
            cardSfx?.PlayHover();
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
            // Selected > Hovered > Normal
            if (selectedView != null && spawnedCards.Contains(selectedView))
                return selectedView;

            if (hoveredCardView != null &&
                spawnedCards.Contains(hoveredCardView) &&
                hoveredCardView.IsPointerOver)
            {
                return hoveredCardView;
            }

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

            if (useFixedSizeAdaptiveFan)
                LayoutCardsFixedSize();
            else if (useAdaptiveFanLayout)
                LayoutCardsAdaptive();
            else
                LayoutCardsLegacy();

            UpdateHandSiblingOrder();
        }

        private void LayoutCardsLegacy()
        {
            int count = spawnedCards.Count;
            float cardScale = GetResponsiveCardScale(count);

            float resolvedSpacing = scaleSpacingWithCard ? spacing * cardScale : spacing;
            float resolvedHoverGap = scaleSpacingWithCard ? hoverGap * cardScale : hoverGap;
            float resolvedCurveHeight = scaleSpacingWithCard ? curveHeight * cardScale : curveHeight;

            float centerIndex = count > 1 ? (count - 1) * 0.5f : 0f;
            int focusedIndex = GetFocusedCardIndex();

            for (int i = 0; i < count; i++)
            {
                var view = spawnedCards[i];
                if (view == null)
                    continue;

                var rt = view.LayoutRect;
                if (rt != null)
                {
                    rt.sizeDelta = baseCardSize;
                    rt.localScale = Vector3.one * cardScale;
                }

                float relative = i - centerIndex;

                float x = relative * resolvedSpacing;
                float y = -resolvedCurveHeight * relative * relative;

                if (focusedIndex >= 0)
                {
                    if (i < focusedIndex)
                        x -= resolvedHoverGap * 0.5f;
                    else if (i > focusedIndex)
                        x += resolvedHoverGap * 0.5f;
                }

                float rotZ = -relative * rotationStep;
                view.SetLayoutPose(new Vector2(x, y), rotZ);
            }
        }

        private void LayoutCardsFixedSize()
        {
            int count = spawnedCards.Count;
            float cardScale = Mathf.Max(0.01f, fixedCardScale);
            float cardWidth = ResolveFixedCardWidth(cardScale);

            float preferredSpacing = Mathf.Max(0.01f, preferredCenterSpacing);
            float fitSpacing = count > 1
                ? Mathf.Max(0f, (maxHandWidth - cardWidth) / (count - 1))
                : preferredSpacing;

            // Small hands keep preferred spacing and stay grouped (do not stretch to maxHandWidth).
            // Large hands compress only as far as needed to fit.
            float resolvedSpacing = count <= 1
                ? 0f
                : Mathf.Min(preferredSpacing, fitSpacing);

            if (fitSpacing >= minimumCenterSpacing)
                resolvedSpacing = Mathf.Max(minimumCenterSpacing, resolvedSpacing);

            resolvedSpacing = Mathf.Max(0f, resolvedSpacing);

            int maxHandSize = deckController != null ? Mathf.Max(5, deckController.MaxHandSize) : 10;
            float largeHandT = Mathf.InverseLerp(largeHandHoverStartCount, maxHandSize, count);
            float resolvedHoverGap = Mathf.Lerp(hoverGap, largeHandHoverGap, largeHandT);
            float resolvedHoverRaise = Mathf.Lerp(hoverRaise, largeHandHoverRaise, largeHandT);
            float resolvedHoverScale = Mathf.Lerp(hoverScale, largeHandHoverScale, largeHandT);

            float centerIndex = count > 1 ? (count - 1) * 0.5f : 0f;
            int focusedIndex = GetFocusedCardIndex();

            float fanStrength = GetFanStrength(count);
            float resolvedEdgeDrop = maxEdgeDrop * fanStrength;
            float resolvedEdgeRotation = maxEdgeRotation * fanStrength;

            float totalSpan = count > 1 ? resolvedSpacing * (count - 1) : 0f;
            float totalWidth = cardWidth + totalSpan;

            lastLoggedCardScale = cardScale;
            lastLoggedCardWidth = cardWidth;
            lastLoggedPreferredSpacing = preferredSpacing;
            lastLoggedFitSpacing = fitSpacing;
            lastLoggedSpacing = resolvedSpacing;
            lastLoggedTotalSpan = totalSpan;
            lastLoggedTotalWidth = totalWidth;
            lastLoggedEdgeDrop = resolvedEdgeDrop;
            lastLoggedHandRaise = 0f;
            lastLoggedEdgeRotation = resolvedEdgeRotation;
            lastLoggedFanStrength = fanStrength;
            lastLoggedHoverGap = resolvedHoverGap;
            lastLoggedHoverRaise = resolvedHoverRaise;
            lastLoggedHoverScale = resolvedHoverScale;
            lastLoggedHoveredIndex = focusedIndex;

            for (int i = 0; i < count; i++)
            {
                var view = spawnedCards[i];
                if (view == null)
                    continue;

                float edgeHoverOffsetX = 0f;
                if (focusedIndex >= 0 &&
                    i == focusedIndex &&
                    count > 1 &&
                    edgeHoverInwardOffset > 0f)
                {
                    if (i == 0)
                        edgeHoverOffsetX = edgeHoverInwardOffset;
                    else if (i == count - 1)
                        edgeHoverOffsetX = -edgeHoverInwardOffset;
                }

                // Edge inward correction moves VisualRoot only — root hit slot stays fixed.
                ApplyFixedSizePresentationTuning(
                    view,
                    resolvedHoverRaise,
                    resolvedHoverScale,
                    edgeHoverOffsetX);

                var rt = view.LayoutRect;
                if (rt != null)
                {
                    rt.sizeDelta = baseCardSize;
                    rt.localScale = Vector3.one * cardScale;
                }

                float relative = i - centerIndex;
                float normalized = centerIndex > 0f ? relative / centerIndex : 0f;
                float edgeFactor = normalized * normalized;

                float x = relative * resolvedSpacing;
                float y = -edgeFactor * resolvedEdgeDrop;

                if (focusedIndex >= 0 && i != focusedIndex)
                {
                    int distance = Mathf.Abs(i - focusedIndex);
                    float distanceMultiplier = distance <= 1
                        ? 1f
                        : Mathf.Pow(neighborPushFalloff, distance - 1);
                    float push = resolvedHoverGap * 0.5f * distanceMultiplier;

                    if (i < focusedIndex)
                        x -= push;
                    else
                        x += push;
                }

                float rotZ = -normalized * resolvedEdgeRotation;
                view.SetLayoutPose(new Vector2(x, y), rotZ);
            }

            if (verboseLogs && count != lastLoggedLayoutCount)
                LogFixedFanLayout(force: false);

            lastLoggedLayoutCount = count;
        }

        private float GetFanStrength(int cardCount)
        {
            if (!useCustomFanStrengthByCount)
                return 1f;

            if (cardCount <= 0)
                return 0f;

            if (fanStrengthByCardCount == null || fanStrengthByCardCount.Length == 0)
                return 1f;

            int index = Mathf.Clamp(cardCount, 0, fanStrengthByCardCount.Length - 1);
            return Mathf.Max(0f, fanStrengthByCardCount[index]);
        }

        private float ResolveFixedCardWidth(float cardScale)
        {
            float width = baseCardSize.x;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                var rt = view != null ? view.LayoutRect : null;
                if (rt == null)
                    continue;

                float rectWidth = rt.rect.width;
                if (rectWidth > 1f)
                {
                    width = rectWidth;
                    break;
                }

                if (rt.sizeDelta.x > 1f)
                {
                    width = rt.sizeDelta.x;
                    break;
                }
            }

            if (width <= 1f && cardViewPrefab != null)
            {
                var prefabRt = cardViewPrefab.LayoutRect != null
                    ? cardViewPrefab.LayoutRect
                    : cardViewPrefab.GetComponent<RectTransform>();
                if (prefabRt != null && prefabRt.sizeDelta.x > 1f)
                    width = prefabRt.sizeDelta.x;
            }

            if (width <= 1f)
                width = 200f;

            return width * Mathf.Max(0.01f, cardScale);
        }

        private void ApplyFixedSizePresentationTuning(
            CardViewUI view,
            float resolvedHoverRaise,
            float resolvedHoverScale,
            float edgeHoverOffsetX)
        {
            if (view == null)
                return;

            float selectedRaise = Mathf.Max(resolvedHoverRaise, resolvedHoverRaise + 20f);
            float selectedScaleMul = Mathf.Max(resolvedHoverScale, resolvedHoverScale + 0.04f);
            // Same inward X for hover and selected — ApplyStateVisuals uses one state at a time.
            view.ApplyHandPresentationTuning(
                resolvedHoverRaise,
                resolvedHoverScale,
                selectedRaise,
                selectedScaleMul,
                edgeHoverOffsetX,
                edgeHoverOffsetX);
        }

        private void UpdateHandSiblingOrder()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var view = spawnedCards[i];
                if (view != null)
                    view.transform.SetSiblingIndex(i);
            }

            if (!keepHoveredCardOnTop)
                return;

            // Selected > Hovered > Normal fan order for draw order only (not DeckController.Hand).
            var focused = GetFocusedCardView();
            if (focused != null)
                focused.transform.SetAsLastSibling();
        }

        private void LayoutCardsAdaptive()
        {
            int count = spawnedCards.Count;
            float cardScale = GetResponsiveCardScale(count);

            float preferredSpacing = scaleSpacingWithCard ? spacing * cardScale : spacing;
            preferredSpacing = Mathf.Clamp(preferredSpacing, minimumSpacing, maximumSpacing);

            float widthLimitedSpacing = count > 1
                ? maxHandWidth / (count - 1)
                : preferredSpacing;

            // Prefer fitting within maxHandWidth; never allow zero/negative spacing.
            float resolvedSpacing = Mathf.Max(0.01f, Mathf.Min(preferredSpacing, widthLimitedSpacing));

            float preferredHoverGap = scaleSpacingWithCard ? hoverGap * cardScale : hoverGap;
            float safeHoverGap = preferredHoverGap;

            int maxHandSize = deckController != null ? Mathf.Max(largeHandStartCount, deckController.MaxHandSize) : 10;
            if (count >= largeHandStartCount)
            {
                float compression = Mathf.InverseLerp(largeHandStartCount, maxHandSize, count);
                safeHoverGap = Mathf.Lerp(preferredHoverGap, preferredHoverGap * 0.65f, compression);
            }

            float centerIndex = count > 1 ? (count - 1) * 0.5f : 0f;
            int focusedIndex = GetFocusedCardIndex();

            float preferredEdgeDrop = curveHeight * centerIndex * centerIndex;
            if (scaleSpacingWithCard)
                preferredEdgeDrop *= cardScale;
            float resolvedEdgeDrop = Mathf.Min(maxEdgeDrop, preferredEdgeDrop);

            float preferredEdgeRotation = rotationStep * centerIndex;
            float resolvedEdgeRotation = Mathf.Min(maxEdgeRotation, preferredEdgeRotation);

            float largeHandT = Mathf.InverseLerp(largeHandStartCount, maxHandSize, count);
            float handRaise = Mathf.Lerp(0f, largeHandRaise, largeHandT);

            float totalSpan = count > 1 ? resolvedSpacing * (count - 1) : 0f;

            lastLoggedCardScale = cardScale;
            lastLoggedSpacing = resolvedSpacing;
            lastLoggedTotalSpan = totalSpan;
            lastLoggedEdgeDrop = resolvedEdgeDrop;
            lastLoggedHandRaise = handRaise;
            lastLoggedEdgeRotation = resolvedEdgeRotation;

            for (int i = 0; i < count; i++)
            {
                var view = spawnedCards[i];
                if (view == null)
                    continue;

                var rt = view.LayoutRect;
                if (rt != null)
                {
                    rt.sizeDelta = baseCardSize;
                    rt.localScale = Vector3.one * cardScale;
                }

                float relative = i - centerIndex;
                float normalized = centerIndex > 0f ? relative / centerIndex : 0f;
                float edgeFactor = normalized * normalized;

                float x = relative * resolvedSpacing;
                float y = -edgeFactor * resolvedEdgeDrop + handRaise;

                if (focusedIndex >= 0)
                {
                    if (i < focusedIndex)
                        x -= safeHoverGap * 0.5f;
                    else if (i > focusedIndex)
                        x += safeHoverGap * 0.5f;
                }

                float rotZ = -normalized * resolvedEdgeRotation;
                view.SetLayoutPose(new Vector2(x, y), rotZ);
            }

            if (verboseLogs && count != lastLoggedLayoutCount)
                LogAdaptiveHandLayout(force: false);

            lastLoggedLayoutCount = count;
        }

        private void LogAdaptiveHandLayout(bool force)
        {
            if (!force && !verboseLogs)
                return;

            Debug.Log(
                "[AdaptiveHandLayout]\n" +
                $"Count={spawnedCards.Count}\n" +
                $"CardScale={lastLoggedCardScale:0.##}\n" +
                $"Spacing={lastLoggedSpacing:0.##}\n" +
                $"TotalSpan={lastLoggedTotalSpan:0.##}\n" +
                $"EdgeDrop={lastLoggedEdgeDrop:0.##}\n" +
                $"HandRaise={lastLoggedHandRaise:0.##}\n" +
                $"EdgeRotation={lastLoggedEdgeRotation:0.##}");
        }

        private void LogFixedFanLayout(bool force)
        {
            if (!force && !verboseLogs)
                return;

            Debug.Log(
                "[FixedFanLayout]\n" +
                $"Count={spawnedCards.Count}\n" +
                $"CardWidth={lastLoggedCardWidth:0.##}\n" +
                $"Scale={lastLoggedCardScale:0.##}\n" +
                $"PreferredSpacing={lastLoggedPreferredSpacing:0.##}\n" +
                $"FitSpacing={lastLoggedFitSpacing:0.##}\n" +
                $"ResolvedSpacing={lastLoggedSpacing:0.##}\n" +
                $"TotalWidth={lastLoggedTotalWidth:0.##}\n" +
                $"FanStrength={lastLoggedFanStrength:0.##}\n" +
                $"EdgeDrop={lastLoggedEdgeDrop:0.##}\n" +
                $"EdgeRotation={lastLoggedEdgeRotation:0.##}");
        }

        private void LogFanStrength(bool force)
        {
            if (!force && !verboseLogs)
                return;

            Debug.Log(
                "[FanStrength]\n" +
                $"Count={spawnedCards.Count}\n" +
                $"Strength={lastLoggedFanStrength:0.##}\n" +
                $"ResolvedEdgeRotation={lastLoggedEdgeRotation:0.##}\n" +
                $"ResolvedEdgeDrop={lastLoggedEdgeDrop:0.##}");
        }

        private void LogHandPresentation(bool force)
        {
            if (!force && !verboseLogs)
                return;

            Debug.Log(
                "[HandPresentation]\n" +
                $"Count={spawnedCards.Count}\n" +
                $"FanStrength={lastLoggedFanStrength:0.##}\n" +
                $"ResolvedEdgeDrop={lastLoggedEdgeDrop:0.##}\n" +
                $"ResolvedEdgeRotation={lastLoggedEdgeRotation:0.##}\n" +
                $"HoverGap={lastLoggedHoverGap:0.##}\n" +
                $"HoverRaise={lastLoggedHoverRaise:0.##}\n" +
                $"HoverScale={lastLoggedHoverScale:0.##}\n" +
                $"HoveredIndex={lastLoggedHoveredIndex}");
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

        /// <summary>Whether this card needs the single-enemy target selection UI before play.</summary>
        private bool RequiresManualEnemyTarget(CardData data)
        {
            if (data == null)
                return false;

            return data.TargetMode == CardTargetMode.SingleEnemy;
        }

        private void TryPlayCardFromView(CardInstance card)
        {
            if (card?.Data == null || battleActionRunner == null)
                return;

            if (RequiresManualEnemyTarget(card.Data))
            {
                if (targetSelectionSystem != null)
                {
                    targetSelectionSystem.BeginTargetSelection(card);

                    if (verboseLogs)
                        Debug.Log($"[HandUI] Waiting for target selection: {card.Data.DisplayName} | TargetMode: {card.Data.TargetMode}");

                    // สำคัญ: อย่า RefreshHandUI ตรงนี้
                    // เพื่อให้ selected state ค้างอยู่
                    return;
                }
            }

            battleActionRunner.TryPlayCard(card, null);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[HandUI] Clicked {card.Data.DisplayName} | Immediate resolve | " +
                    $"TargetMode: {card.Data.TargetMode} | Target: None");
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
            if (handSelectionActive && activeHandSelection != null && activeHandSelection.IsSelecting)
            {
                activeHandSelection.RefreshSelectionVisuals();
                return;
            }

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

        /// <summary>Clears hand UI selection and spawned views before deck rebuild for a new battle.</summary>
        public void ResetHandRuntimeStateForNewBattle()
        {
            if (dealRoutine != null)
                StopDealRoutineAndReleaseLocks();

            handCardSelectionController?.ForceCancelSelection();
            EndHandSelection();
            DeselectCurrentCard();
            hoveredCardView = null;
            ClearSpawnedCards();
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
                {
                    view.SetLayoutMovementBlocked(false);
                    cardSfx?.PlayDraw();
                }

                if (stagger > 0f && i < newViews.Count - 1)
                    yield return new WaitForSeconds(stagger);
            }

            dealRoutine = null;
            RefreshCardInteractivity();
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
                {
                    if (view.IsDealPresentationPending)
                        view.ForceCompleteDealPresentation();
                    else
                        view.SetLayoutMovementBlocked(false);
                }
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