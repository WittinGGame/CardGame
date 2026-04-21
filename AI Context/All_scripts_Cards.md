================================================================================
FILE: CardData.cs
PATH: Assets/Scripts/CardBattle/Cards/Data/CardData.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Designer-facing definition of a card. Runtime copies are <see cref="CardInstance"/>.
    /// Keep numeric hooks here; layer modifiers via <see cref="ICardModifier"/> on instances later.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Card Battle/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [SerializeField] private string cardId;
        [SerializeField] private string displayName;
        [SerializeField] private CardType cardType = CardType.Attack;
        [Tooltip("AP spent when the card is played successfully.")]
        [SerializeField] private int apCost = 1;

        [Header("Attack")]
        [SerializeField] private int attackDamage = 3;

        [Header("Heal")]
        [SerializeField] private int healAmount = 2;

        [Header("Buff")]
        [Tooltip("Generic potency for buffs (e.g. extra damage on next attack, block, etc.). Wired in CardResolver / player hooks.")]
        [SerializeField] private int buffPotency = 1;

        [Header("Defend")]
        [SerializeField] private int blockAmount = 5;

        [Header("Visuals")]
        [SerializeField] private Sprite artwork;

        public string CardId => string.IsNullOrEmpty(cardId) ? name : cardId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public CardType CardType => cardType;
        public int ApCost => Mathf.Max(0, apCost);
        public int AttackDamage => Mathf.Max(0, attackDamage);
        public int HealAmount => Mathf.Max(0, healAmount);
        public int BuffPotency => buffPotency;
        public int BlockAmount => Mathf.Max(0, blockAmount);
        public Sprite Artwork => artwork;
    }
}


================================================================================
FILE: CardType.cs
PATH: Assets/Scripts/CardBattle/Cards/Data/CardType.cs
================================================================================
namespace CardBattle.Core
{
    /// <summary>Primary card families; extend with new enum values or parallel systems as content grows.</summary>
    public enum CardType
    {
        Attack,
        Buff,
        Heal,
        Defend
    }
}

================================================================================
FILE: CardInstance.cs
PATH: Assets/Scripts/CardBattle/Cards/Runtime/CardInstance.cs
================================================================================
using System;
using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>
    /// Runtime card in a pile (deck / hand / graveyard). Holds a reference to static <see cref="CardData"/>
    /// plus optional modifiers for upgrades and temporary effects.
    /// </summary>
    public class CardInstance
    {
        public CardData Data { get; }
        public Guid InstanceId { get; }

        private readonly List<ICardModifier> _modifiers = new List<ICardModifier>();

        public IReadOnlyList<ICardModifier> Modifiers => _modifiers;

        public CardInstance(CardData data, Guid? instanceId = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            InstanceId = instanceId ?? Guid.NewGuid();
        }

        public void AddModifier(ICardModifier modifier)
        {
            if (modifier != null)
                _modifiers.Add(modifier);
        }

        public void ClearModifiers() => _modifiers.Clear();
    }
}

================================================================================
FILE: CardPlayContext.cs
PATH: Assets/Scripts/CardBattle/Cards/Runtime/CardPlayContext.cs
================================================================================
using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>Mutable snapshot passed through resolution so modifiers can read/write shared battle state.</summary>
    public class CardPlayContext
    {
        public PlayerBattleUnit Player { get; }
        public CardInstance Card { get; }
        public IReadOnlyList<EnemyBattleUnit> Enemies { get; }
        public EnemyBattleUnit PrimaryTarget { get; set; }

        /// <summary>Set to false by modifiers to skip default type handling (e.g. replaced entirely by an upgrade).</summary>
        public bool ApplyBaseCardLogic { get; set; } = true;

        public CardPlayContext(PlayerBattleUnit player, CardInstance card, IReadOnlyList<EnemyBattleUnit> enemies, EnemyBattleUnit primaryTarget = null)
        {
            Player = player;
            Card = card;
            Enemies = enemies;
            PrimaryTarget = primaryTarget;
        }
    }
}


================================================================================
FILE: ICardModifier.cs
PATH: Assets/Scripts/CardBattle/Cards/Runtime/ICardModifier.cs
================================================================================
namespace CardBattle.Core
{
    /// <summary>
    /// Hook for future upgrades, relics, or temporary effects that alter how a card resolves.
    /// CardResolver can iterate modifiers before/after base resolution.
    /// </summary>
    public interface ICardModifier
    {
        /// <summary>Called before base card logic; return false to cancel further resolution for this play.</summary>
        bool PreResolve(CardPlayContext context);

        /// <summary>Called after base card logic.</summary>
        void PostResolve(CardPlayContext context);
    }
}

================================================================================
FILE: CardResolver.cs
PATH: Assets/Scripts/CardBattle/Cards/Systems/CardResolver.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Central place for card effect execution. Add branching per <see cref="CardType"/> here,
    /// and let <see cref="ICardModifier"/> adjust <see cref="CardPlayContext"/> before/after.
    /// </summary>
    public class CardResolver : MonoBehaviour
    {
        [SerializeField] private bool logResolution;

        public void Resolve(CardPlayContext context)
        {
            if (context?.Card?.Data == null || context.Player == null)
                return;

            foreach (var modifier in context.Card.Modifiers)
            {
                if (modifier != null && !modifier.PreResolve(context))
                    context.ApplyBaseCardLogic = false;
            }

            if (context.ApplyBaseCardLogic)
                ApplyCoreCardLogic(context);

            foreach (var modifier in context.Card.Modifiers)
                modifier?.PostResolve(context);

            if (logResolution)
                Debug.Log($"Resolved {context.Card.Data.DisplayName} ({context.Card.Data.CardType}).");
        }

        private static void ApplyCoreCardLogic(CardPlayContext context)
        {
            var data = context.Card.Data;
            switch (data.CardType)
            {
                case CardType.Attack:
                    ResolveAttack(context, data);
                    break;
                case CardType.Heal:
                    context.Player.Heal(data.HealAmount);
                    break;
                case CardType.Buff:
                    context.Player.ApplyBuffFromCard(data);
                    break;
                case CardType.Defend:
                    context.Player.AddBlock(data.BlockAmount);
                    break;
                default:
                    Debug.LogWarning($"Unhandled card type {data.CardType}.");
                    break;
            }
        }

        private static void ResolveAttack(CardPlayContext context, CardData data)
        {
            var target = ChooseAttackTarget(context);
            if (target == null || !target.IsAlive)
                return;

            var bonus = context.Player.ConsumeDamageBonus();
            var total = data.AttackDamage + bonus;
            bool wasAliveBeforeHit = target.IsAlive;

            int hpDamage = target.TakeDamage(total);

            if (wasAliveBeforeHit)
            {
                if (!target.IsAlive)
                    target.View?.PlayDead();
                else if (hpDamage > 0)
                    target.View?.PlayHurt();
            }
        }

        private static EnemyBattleUnit ChooseAttackTarget(CardPlayContext context)
        {
            if (context.PrimaryTarget != null && context.PrimaryTarget.IsAlive)
                return context.PrimaryTarget;

            if (context.Enemies == null)
                return null;

            foreach (var enemy in context.Enemies)
            {
                if (enemy != null && enemy.IsAlive)
                    return enemy;
            }

            return null;
        }
    }
}

================================================================================
FILE: DeckController.cs
PATH: Assets/Scripts/CardBattle/Cards/Systems/DeckController.cs
================================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Owns the three piles (deck, hand, graveyard) and draw/discard/shuffle rules.
    /// When the deck is empty during a draw, the graveyard is shuffled back into the deck.
    /// </summary>
    public class DeckController : MonoBehaviour
    {
        [Tooltip("Optional designer list consumed by BuildFromCardDataList / BuildFromInspectorBlueprint at battle setup.")]
        [SerializeField] private List<CardData> starterDeckBlueprint = new List<CardData>();

        private readonly List<CardInstance> _deck = new List<CardInstance>();
        private readonly List<CardInstance> _hand = new List<CardInstance>();
        private readonly List<CardInstance> _graveyard = new List<CardInstance>();

        public IReadOnlyList<CardInstance> Deck => _deck;
        public IReadOnlyList<CardInstance> Hand => _hand;
        public IReadOnlyList<CardInstance> Graveyard => _graveyard;

        /// <summary>Fired after any pile mutation so UI or VFX can subscribe later.</summary>
        public event Action OnPilesChanged;

        /// <summary>Replace runtime piles using blueprint assets (one <see cref="CardInstance"/> per entry).</summary>
        public void BuildFromCardDataList(IEnumerable<CardData> cards)
        {
            ClearAllPiles();
            if (cards == null)
                return;

            foreach (var data in cards)
            {
                if (data != null)
                    _deck.Add(new CardInstance(data));
            }

            ShuffleDeck();
            NotifyChanged();
        }

        /// <summary>Uses the serialized starter blueprint when no explicit list is provided.</summary>
        public void BuildFromInspectorBlueprint()
        {
            BuildFromCardDataList(starterDeckBlueprint);
        }

        public void ClearAllPiles()
        {
            _deck.Clear();
            _hand.Clear();
            _graveyard.Clear();
            NotifyChanged();
        }

        public bool IsInHand(CardInstance card) => card != null && _hand.Contains(card);

        /// <summary>Draw up to <paramref name="count"/> cards into the hand, reshuffling graveyard into deck as needed.</summary>
        public void DrawCards(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!TryDrawSingleCard())
                    break;
            }

            NotifyChanged();
        }

        /// <summary>Move every card from hand to graveyard (end of player turn).</summary>
        public void DiscardEntireHand()
        {
            for (var i = _hand.Count - 1; i >= 0; i--)
                MoveToGraveyard(_hand[i]);

            NotifyChanged();
        }

        /// <summary>Play resolution: remove from hand and send to graveyard.</summary>
        public void PlayCardFromHand(CardInstance card)
        {
            if (card == null || !_hand.Remove(card))
                return;

            _graveyard.Add(card);
            NotifyChanged();
        }

        public void ShuffleDeck()
        {
            ShuffleListInPlace(_deck);
            NotifyChanged();
        }

        private bool TryDrawSingleCard()
        {
            if (_deck.Count == 0)
                ReshuffleGraveyardIntoDeck();

            if (_deck.Count == 0)
                return false;

            var index = _deck.Count - 1;
            var drawn = _deck[index];
            _deck.RemoveAt(index);
            _hand.Add(drawn);
            return true;
        }

        private void ReshuffleGraveyardIntoDeck()
        {
            if (_graveyard.Count == 0)
                return;

            _deck.AddRange(_graveyard);
            _graveyard.Clear();
            ShuffleListInPlace(_deck);
        }

        private static void ShuffleListInPlace(IList<CardInstance> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void MoveToGraveyard(CardInstance card)
        {
            if (card == null)
                return;

            _hand.Remove(card);
            _deck.Remove(card);
            if (!_graveyard.Contains(card))
                _graveyard.Add(card);
        }

        private void NotifyChanged() => OnPilesChanged?.Invoke();
    }
}

================================================================================
FILE: CardViewUI.cs
PATH: Assets/Scripts/CardBattle/Cards/UI/CardViewUI.cs
================================================================================
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class CardViewUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum CardVisualState
        {
            Normal,
            Hovered,
            Selected,
            Disabled
        }

        [Header("UI References")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Core References")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button button;

        [Header("Visual Tuning")]
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float hoveredScale = 1.08f;
        [SerializeField] private float selectedScale = 1.14f;
        [SerializeField] private float disabledScale = 0.96f;

        [SerializeField] private float normalAlpha = 1f;
        [SerializeField] private float disabledAlpha = 0.5f;

        [SerializeField] private float hoveredYOffset = 25f;
        [SerializeField] private float selectedYOffset = 40f;

        [SerializeField] private float scaleLerpSpeed = 12f;
        [SerializeField] private float moveLerpSpeed = 12f;
        [SerializeField] private float layoutLerpSpeed = 12f;
        [SerializeField] private float rotationLerpSpeed = 14f;

        [Header("Deal-In Presentation")]
        [SerializeField] private bool useDealFadeIn = true;
        [SerializeField] private float dealSpawnAlpha = 0f;
        [SerializeField] private float dealFadeDuration = 0.12f;

        private CardInstance boundCard;
        private CardVisualState currentState = CardVisualState.Normal;

        private RectTransform _rectTransform;
        private Vector2 targetLayoutAnchoredPos;
        private float _layoutRotationZ;
        private float targetRotationZ;

        private Vector3 baseScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;

        private Vector3 baseLocalPosition = Vector3.zero;
        private Vector3 targetLocalPosition = Vector3.zero;

        private bool isInteractable = true;
        private bool isSelected = false;
        private bool isPointerOver;
        private bool layoutMovementBlocked;
        private bool pendingDealFadeIn;
        private Coroutine dealFadeRoutine;

        public CardInstance BoundCard => boundCard;
        /// <summary>Root layout rect (anchored fan position). Used by presentation VFX.</summary>
        public RectTransform LayoutRect => _rectTransform;
        public bool IsSelected => isSelected;
        public bool IsInteractable => isInteractable;
        public bool IsPointerOver => isPointerOver;

        public event System.Action<CardViewUI> OnHoverStarted;
        public event System.Action<CardViewUI> OnHoverEnded;

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = transform.Find("VisualRoot") as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (button == null)
                button = GetComponent<Button>();

            if (visualRoot != null)
            {
                baseScale = visualRoot.localScale;
                targetScale = baseScale * normalScale;

                baseLocalPosition = visualRoot.localPosition;
                targetLocalPosition = baseLocalPosition;

                targetRotationZ = visualRoot.localEulerAngles.z;
                _layoutRotationZ = targetRotationZ;
            }

            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
                targetLayoutAnchoredPos = _rectTransform.anchoredPosition;
        }

        private void OnDisable()
        {
            StopDealFadeRoutine();
        }

        /// <summary>Tuning from <see cref="HandUIController"/> so hand and card share one layout motion speed.</summary>
        public void SetLayoutLerpSpeed(float speed)
        {
            layoutLerpSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>Targets root layout position and idle fan rotation; motion is smoothed in <see cref="Update"/>.</summary>
        public void SetLayoutPose(Vector2 anchoredPos, float rotationZ)
        {
            targetLayoutAnchoredPos = anchoredPos;
            _layoutRotationZ = rotationZ;
            SyncRotationTargetToState();
        }

        /// <summary>
        /// Sets an immediate spawn pose before the normal layout lerp pulls this card toward its target slot.
        /// </summary>
        public void PrepareForDealIn(Vector2 startAnchoredPos, float startRotationZ, float startScale)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
                _rectTransform.anchoredPosition = startAnchoredPos;

            if (visualRoot != null)
            {
                float clampedScale = Mathf.Max(0.01f, startScale);
                visualRoot.localScale = baseScale * clampedScale;
                visualRoot.localEulerAngles = new Vector3(0f, 0f, startRotationZ);
            }

            StopDealFadeRoutine();
            pendingDealFadeIn = useDealFadeIn && canvasGroup != null;
            if (pendingDealFadeIn && canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(dealSpawnAlpha);
        }

        public void SetLayoutMovementBlocked(bool value)
        {
            bool wasBlocked = layoutMovementBlocked;
            layoutMovementBlocked = value;

            if (layoutMovementBlocked)
            {
                StopDealFadeRoutine();
                return;
            }

            if (wasBlocked && pendingDealFadeIn)
                StartDealFadeIn();
        }

        private void SyncRotationTargetToState()
        {
            if (currentState == CardVisualState.Hovered || currentState == CardVisualState.Selected)
                targetRotationZ = 0f;
            else
                targetRotationZ = _layoutRotationZ;
        }

        private void Update()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
            {
                if (!layoutMovementBlocked)
                {
                    _rectTransform.anchoredPosition = Vector2.Lerp(
                        _rectTransform.anchoredPosition,
                        targetLayoutAnchoredPos,
                        Time.deltaTime * layoutLerpSpeed
                    );
                }
            }

            if (visualRoot == null)
                return;

            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                targetScale,
                Time.deltaTime * scaleLerpSpeed
            );

            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                targetLocalPosition,
                Time.deltaTime * moveLerpSpeed
            );

            float currentZ = visualRoot.localEulerAngles.z;
            float newZ = Mathf.LerpAngle(currentZ, targetRotationZ, Time.deltaTime * rotationLerpSpeed);
            visualRoot.localEulerAngles = new Vector3(0f, 0f, newZ);
        }

        public void Bind(CardInstance card)
        {
            boundCard = card;

            if (card?.Data == null)
                return;

            var data = card.Data;

            if (costText != null)
                costText.text = data.ApCost.ToString();

            if (nameText != null)
                nameText.text = data.DisplayName;

            if (typeText != null)
                typeText.text = data.CardType.ToString();

            if (artworkImage != null)
                artworkImage.sprite = data.Artwork;

            if (descriptionText != null)
                descriptionText.text = GetDescription(data);

            ApplyStateVisuals();
        }

        /// <summary>Artwork sprite for flying ghost VFX (presentation only).</summary>
        public Sprite GetArtworkSnapshotForVfx()
        {
            return artworkImage != null ? artworkImage.sprite : null;
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;

            if (button != null)
                button.interactable = value;

            if (!isInteractable)
            {
                isSelected = false;
                currentState = CardVisualState.Disabled;
            }
            else
            {
                currentState = isSelected
                    ? CardVisualState.Selected
                    : (isPointerOver ? CardVisualState.Hovered : CardVisualState.Normal);
            }

            ApplyStateVisuals();
        }

        public void SetClickAction(UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();

            if (action != null)
            {
                button.onClick.AddListener(() =>
                {
                    Select();
                    action.Invoke();
                });
            }
        }

        public void Select()
        {
            if (!isInteractable)
                return;

            isSelected = true;
            currentState = CardVisualState.Selected;
            ApplyStateVisuals();
        }

        public void Deselect()
        {
            isSelected = false;

            if (!isInteractable)
                currentState = CardVisualState.Disabled;
            else if (isPointerOver)
            {
                currentState = CardVisualState.Hovered;
                OnHoverStarted?.Invoke(this);
            }
            else
                currentState = CardVisualState.Normal;

            ApplyStateVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;

            if (!isInteractable || isSelected)
                return;

            OnHoverStarted?.Invoke(this);

            currentState = CardVisualState.Hovered;
            ApplyStateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;

            if (!isInteractable)
                return;

            if (isSelected)
                return;

            OnHoverEnded?.Invoke(this);

            currentState = CardVisualState.Normal;
            ApplyStateVisuals();
        }

        private void ApplyStateVisuals()
        {
            if (visualRoot == null)
                return;

            SyncRotationTargetToState();

            switch (currentState)
            {
                case CardVisualState.Normal:
                    targetScale = baseScale * normalScale;
                    targetLocalPosition = baseLocalPosition;
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Hovered:
                    targetScale = baseScale * hoveredScale;
                    targetLocalPosition = baseLocalPosition + new Vector3(0f, hoveredYOffset, 0f);
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Selected:
                    targetScale = baseScale * selectedScale;
                    targetLocalPosition = baseLocalPosition + new Vector3(0f, selectedYOffset, 0f);
                    SetAlpha(normalAlpha);
                    break;

                case CardVisualState.Disabled:
                    targetScale = baseScale * disabledScale;
                    targetLocalPosition = baseLocalPosition;
                    SetAlpha(disabledAlpha);
                    break;
            }
        }

        private void SetAlpha(float value)
        {
            if (canvasGroup != null)
            {
                if (layoutMovementBlocked && pendingDealFadeIn)
                    canvasGroup.alpha = Mathf.Min(Mathf.Clamp01(value), Mathf.Clamp01(dealSpawnAlpha));
                else
                    canvasGroup.alpha = value;
            }
        }

        private void StartDealFadeIn()
        {
            if (canvasGroup == null)
            {
                pendingDealFadeIn = false;
                return;
            }

            StopDealFadeRoutine();
            dealFadeRoutine = StartCoroutine(CoDealFadeIn(ResolveTargetAlphaForCurrentState()));
        }

        private IEnumerator CoDealFadeIn(float targetAlpha)
        {
            float duration = Mathf.Max(0.01f, dealFadeDuration);
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            float endAlpha = Mathf.Clamp01(targetAlpha);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            pendingDealFadeIn = false;
            dealFadeRoutine = null;
        }

        private float ResolveTargetAlphaForCurrentState()
        {
            return currentState == CardVisualState.Disabled ? disabledAlpha : normalAlpha;
        }

        private void StopDealFadeRoutine()
        {
            if (dealFadeRoutine != null)
            {
                StopCoroutine(dealFadeRoutine);
                dealFadeRoutine = null;
            }
        }

        private string GetDescription(CardData data)
        {
            switch (data.CardType)
            {
                case CardType.Attack:
                    return $"Deal {data.AttackDamage} damage";
                case CardType.Heal:
                    return $"Heal {data.HealAmount}";
                case CardType.Buff:
                    return $"Gain +{data.BuffPotency}";
                case CardType.Defend:
                    return $"Gain {data.BlockAmount} Block";
                default:
                    return "";
            }
        }
    }
}

================================================================================
FILE: HandUIController.cs
PATH: Assets/Scripts/CardBattle/Cards/UI/HandUIController.cs
================================================================================
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