## FILE: CardData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Data/CardData.cs`
```csharp
using System.Collections.Generic;
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

        [Header("Effect System (Phase 1)")]
        [SerializeField] private CardTargetMode targetMode = CardTargetMode.None;
        [SerializeField] private CardEffectData[] effects;

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
        public CardTargetMode TargetMode => targetMode;
        public IReadOnlyList<CardEffectData> Effects => effects;
        public bool HasEffects => effects != null && effects.Length > 0;
        public Sprite Artwork => artwork;
    }
}
```

## FILE: CardTargetMode.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Data/CardTargetMode.cs`
```csharp
namespace CardBattle.Core
{
    public enum CardTargetMode
    {
        None,
        Self,
        SingleEnemy,
        AllEnemies
    }
}
```

## FILE: CardType.cs

**Path:** `Assets/Scripts/CardBattle/Cards/Data/CardType.cs`

```csharp
namespace CardBattle.Core
{
    /// Primary card families; extend with new enum values or parallel systems as content grows.
    public enum CardType
    {
        Attack,
        Buff,
        Heal,
        Defend
    }
}
```

## FILE: CardEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/CardEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Designer-facing base class for effect-driven card behavior.
    /// </summary>
    public abstract class CardEffectData : ScriptableObject
    {
        public abstract string GetDescriptionText();
        public abstract void Apply(CardPlayContext context, CardEffectExecutionContext executionContext);
    }
}
```

## FILE: AddBlockEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/AddBlockEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "AddBlockEffect", menuName = "Card Battle/Effects/Add Block")]
    public class AddBlockEffectData : CardEffectData
    {
        [SerializeField] private int blockAmount = 5;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, blockAmount);
            return $"Gain <color=#B0966E>{value} Block</color>";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            context.Player.AddBlock(Mathf.Max(0, blockAmount));
        }
    }
}
```

## FILE: DealDamageEffectData.cs

**Path:** `Assets/Scripts/CardBattle/Cards/Effects/DealDamageEffectData.cs`

```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "DealDamageEffect", menuName = "Card Battle/Effects/Deal Damage")]
    public class DealDamageEffectData : CardEffectData
    {
        [SerializeField] private int damage = 3;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, damage);
            return $"Deal <color=#B0966E>{value} damage</color>";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context == null || executionContext == null)
                return;

            int bonus = context.Player != null ? context.Player.ConsumeDamageBonus() : 0;
            int totalDamage = Mathf.Max(0, damage + bonus);
            if (totalDamage <= 0 || executionContext.EnemyTargets == null)
                return;

            for (int i = 0; i < executionContext.EnemyTargets.Count; i++)
            {
                var target = executionContext.EnemyTargets[i];
                if (target == null || !target.IsAlive)
                    continue;

                bool wasAliveBeforeHit = target.IsAlive;
                int hpDamage = target.TakeDamage(totalDamage);

                if (wasAliveBeforeHit)
                {
                    if (!target.IsAlive)
                        target.View?.PlayDead();
                    else if (hpDamage > 0)
                        target.View?.PlayHurt();
                }
            }
        }
    }
}
```

## FILE: HealEffectData.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Effects/HealEffectData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Card Battle/Effects/Heal")]
    public class HealEffectData : CardEffectData
    {
        [SerializeField] private int healAmount = 2;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, healAmount);
            return $"Heal <color=#B0966E>{value}</color>";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            context.Player.Heal(Mathf.Max(0, healAmount));
        }
    }
}
```

## FILE: CardEffectExecutionContext.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/CardEffectExecutionContext.cs`
```csharp
using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>
    /// Runtime execution payload containing resolved targets for one effect application.
    /// </summary>
    public class CardEffectExecutionContext
    {
        public IReadOnlyList<EnemyBattleUnit> EnemyTargets { get; }

        public CardEffectExecutionContext(IReadOnlyList<EnemyBattleUnit> enemyTargets)
        {
            EnemyTargets = enemyTargets;
        }
    }
}
```

## FILE: CardInstance.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/CardInstance.cs`
```csharp
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
```

## FILE: CardPlayContext.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/CardPlayContext.cs`
```csharp
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
```

## FILE: ICardModifier.cs

**Path:** `Assets/Scripts/CardBattle/Cards/Runtime/ICardModifier.cs`

```csharp
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
```

## FILE: CardResolver.cs
**Path:** `Assets/Scripts/CardBattle/Cards/Systems/CardResolver.cs`
```csharp
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

            bool usedEffectsPipeline = false;
            if (context.ApplyBaseCardLogic)
            {
                if (context.Card.Data.HasEffects)
                {
                    ApplyEffectCardLogic(context);
                    usedEffectsPipeline = true;
                }
                else
                {
                    ApplyCoreCardLogic(context);
                }
            }

            foreach (var modifier in context.Card.Modifiers)
                modifier?.PostResolve(context);

            if (logResolution)
            {
                string path = usedEffectsPipeline ? "Effects pipeline" : "Legacy CardType pipeline";
                Debug.Log($"Resolved {context.Card.Data.DisplayName} via {path}.");
            }
        }

        private static void ApplyEffectCardLogic(CardPlayContext context)
        {
            if (context?.Card?.Data == null)
                return;

            var data = context.Card.Data;
            var enemyTargets = TargetResolver.ResolveEnemyTargets(context, data.TargetMode);
            var executionContext = new CardEffectExecutionContext(enemyTargets);

            var effects = data.Effects;
            if (effects == null)
                return;

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                    continue;

                effect.Apply(context, executionContext);
            }
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
```

## FILE: DeckController.cs

**Path:** `Assets/Scripts/CardBattle/Cards/Systems/DeckController.cs`

```csharp
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

        public int GetDeckCount()
        {
            return _deck.Count;
        }

        public int GetGraveyardCount()
        {
            return _graveyard.Count;
        }

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

        /// <summary>
        /// Draws directly from deck to hand without auto-reshuffle.
        /// Use this for presentation-driven two-phase draws.
        /// </summary>
        public List<CardInstance> DrawCardsImmediate(int count)
        {
            var result = new List<CardInstance>();
            int drawCount = Mathf.Max(0, count);

            for (int i = 0; i < drawCount; i++)
            {
                if (_deck.Count == 0)
                    break;

                int index = _deck.Count - 1;
                var card = _deck[index];
                _deck.RemoveAt(index);
                _hand.Add(card);
                result.Add(card);
            }

            NotifyChanged();
            return result;
        }

        /// <summary>Moves all graveyard cards into deck and shuffles. Returns moved card count.</summary>
        public int ReshuffleGraveyardIntoDeckImmediate()
        {
            int moved = _graveyard.Count;
            if (moved <= 0)
                return 0;

            _deck.AddRange(_graveyard);
            _graveyard.Clear();
            ShuffleListInPlace(_deck);
            NotifyChanged();
            return moved;
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
```

## FILE: CardDescriptionBuilder.cs

**Path:** `Assets/Scripts/CardBattle/Cards/UI/CardDescriptionBuilder.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public static class CardDescriptionBuilder
    {
        public static string Build(CardData data)
        {
            if (data == null)
                return string.Empty;

            if (data.HasEffects)
            {
                var lines = new List<string>();
                var effects = data.Effects;

                if (effects != null)
                {
                    for (int i = 0; i < effects.Count; i++)
                    {
                        var effect = effects[i];
                        if (effect == null)
                            continue;

                        string line = effect.GetDescriptionText();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        lines.Add(line);
                    }
                }

                if (lines.Count > 0)
                    return string.Join("\n", lines);
            }

            return BuildLegacy(data);
        }

        private static string BuildLegacy(CardData data)
        {
            if (data == null)
                return string.Empty;

            switch (data.CardType)
            {
                case CardType.Attack:
                    return $"Deal <color=#D4AB6B>{Mathf.Max(0, data.AttackDamage)} damage</color>";
                case CardType.Heal:
                    return $"Heal <color=#D4AB6B>{Mathf.Max(0, data.HealAmount)}</color>";
                case CardType.Buff:
                    return $"Gain <color=#D4AB6B>+{data.BuffPotency}</color>";
                case CardType.Defend:
                    return $"Gain <color=#D4AB6B>{Mathf.Max(0, data.BlockAmount)} Block</color>";
                default:
                    return string.Empty;
            }
        }
    }
}
```

## FILE: HandUIController.cs
**Path:** `Assets/Scripts/CardBattle/Cards/UI/HandUIController.cs`
```csharp
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
        [SerializeField] private float minCardScale = 0.8f; // 200 -> 160
        [SerializeField] private int maxScaleCardCount = 5;
        [SerializeField] private int minScaleCardCount = 10;
        [SerializeField] private bool scaleSpacingWithCard = true;

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

        private float GetResponsiveCardScale(int count)
        {
            float t = Mathf.InverseLerp(maxScaleCardCount, minScaleCardCount, count);
            return Mathf.Lerp(maxCardScale, minCardScale, t);
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

            if (data.HasEffects)
                return data.TargetMode == CardTargetMode.SingleEnemy;

            return data.CardType == CardType.Attack;
        }

        /// <summary>Primary target for immediate <see cref="BattleActionRunner.TryPlayCard"/> when not entering target selection.</summary>
        private EnemyBattleUnit ResolveImmediateDefaultTarget(CardData data)
        {
            if (data == null)
                return null;

            if (data.HasEffects)
                return null;

            if (data.CardType == CardType.Attack)
                return GetDefaultAliveEnemy();

            return null;
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

            EnemyBattleUnit target = ResolveImmediateDefaultTarget(card.Data);
            battleActionRunner.TryPlayCard(card, target);

            if (verboseLogs)
            {
                string targetName = target != null ? target.name : "None";
                string modeNote = card.Data.HasEffects ? $"TargetMode: {card.Data.TargetMode}" : $"CardType: {card.Data.CardType}";
                Debug.Log($"[HandUI] Clicked {card.Data.DisplayName} | Immediate resolve | {modeNote} | Target: {targetName}");
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

        /// <summary>Clears hand UI selection and spawned views before deck rebuild for a new battle.</summary>
        public void ResetHandRuntimeStateForNewBattle()
        {
            if (dealRoutine != null)
                StopDealRoutineAndReleaseLocks();

            DeselectCurrentCard();
            hoveredCardView = null;
            ClearSpawnedCards();
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
```

## FILE: CardTypeBadgeSet.cs
**Path:** `Assets/Scripts/CardBattle/Cards/UI/CardTypeBadgeSet.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "CardTypeBadgeSet", menuName = "Card Battle/Visuals/Card Type Badge Set")]
    public class CardTypeBadgeSet : ScriptableObject
    {
        [Header("Card Type Badges")]
        [SerializeField] private Sprite attackBadge;
        [SerializeField] private Sprite defendBadge;
        [SerializeField] private Sprite healBadge;
        [SerializeField] private Sprite buffBadge;

        public Sprite GetBadge(CardType type)
        {
            switch (type)
            {
                case CardType.Attack:
                    return attackBadge;

                case CardType.Defend:
                    return defendBadge;

                case CardType.Heal:
                    return healBadge;

                case CardType.Buff:
                    return buffBadge;

                default:
                    return null;
            }
        }
    }
}
```