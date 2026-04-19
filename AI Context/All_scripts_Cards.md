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

        private CardInstance boundCard;
        private CardVisualState currentState = CardVisualState.Normal;

        private Vector3 baseScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;

        private Vector3 baseLocalPosition = Vector3.zero;
        private Vector3 targetLocalPosition = Vector3.zero;

        private bool isInteractable = true;
        private bool isSelected = false;

        public CardInstance BoundCard => boundCard;
        public bool IsSelected => isSelected;
        public bool IsInteractable => isInteractable;

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
            }
        }

        private void Update()
        {
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
                currentState = CardVisualState.Normal;
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
            else
                currentState = CardVisualState.Normal;

            ApplyStateVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            currentState = CardVisualState.Hovered;
            ApplyStateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            // ถ้า selected อยู่ ต้องค้าง state เดิม
            if (isSelected)
                return;

            currentState = CardVisualState.Normal;
            ApplyStateVisuals();
        }

        private void ApplyStateVisuals()
        {
            if (visualRoot == null)
                return;

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
                canvasGroup.alpha = value;
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

        [Header("Options")]
        [SerializeField] private bool autoRefreshOnStart = true;
        [SerializeField] private bool disableUnplayableCards = true;
        [SerializeField] private bool verboseLogs = false;

        private readonly List<CardViewUI> spawnedCards = new List<CardViewUI>();
        private CardViewUI selectedView;

        private void OnEnable()
        {
            if (deckController != null)
                deckController.OnPilesChanged += RefreshHandUI;

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
                deckController.OnPilesChanged -= RefreshHandUI;

            if (player != null)
            {
                player.OnApChangedEvent -= HandlePlayerApChanged;
                player.OnTurnStateChanged -= HandlePlayerTurnStateChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged -= HandleBusyStateChanged;
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

            var hand = deckController.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card?.Data == null)
                    continue;

                var view = Instantiate(cardViewPrefab, handContainer);
                spawnedCards.Add(view);

                view.Bind(card);
                SetupCardView(view, card);
            }

            RefreshCardInteractivity();
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

        private void SelectView(CardViewUI view)
        {
            if (view == null)
                return;

            if (selectedView != null && selectedView != view)
                selectedView.Deselect();

            selectedView = view;
            selectedView.Select();
        }

        public void DeselectCurrentCard()
        {
            if (selectedView != null)
            {
                selectedView.Deselect();
                selectedView = null;
            }
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
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    Destroy(spawnedCards[i].gameObject);
            }

            spawnedCards.Clear();
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