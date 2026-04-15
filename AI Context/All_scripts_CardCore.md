========================================
FILE: BattleTestBootstrap.cs
PATH: Assets/Scripts/CardBattle/Core/BattleTestBootstrap.cs
========================================
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

========================================
FILE: BattleUnit.cs
PATH: Assets/Scripts/CardBattle/Core/BattleUnit.cs
========================================
using System;
using UnityEngine;

namespace CardBattle.Core
{
    public abstract class BattleUnit : MonoBehaviour
    {
        [SerializeField] protected int maxHp = 10;
        [SerializeField] protected int currentHp;

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public bool IsAlive => currentHp > 0;

        public event Action<int, int> OnHpChangedEvent;

        protected virtual void Awake()
        {
            if (currentHp <= 0)
                currentHp = maxHp;

            NotifyHpChanged();
        }

        public virtual void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentHp = Mathf.Max(0, currentHp - amount);
            OnHpChanged();
            NotifyHpChanged();

            if (currentHp == 0)
                OnDefeated();
        }

        public virtual void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentHp = Mathf.Min(maxHp, currentHp + amount);
            OnHpChanged();
            NotifyHpChanged();
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

========================================
FILE: CardData.cs
PATH: Assets/Scripts/CardBattle/Core/CardData.cs
========================================
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

        [Header("Visuals")]
        [SerializeField] private Sprite artwork;

        public string CardId => string.IsNullOrEmpty(cardId) ? name : cardId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public CardType CardType => cardType;
        public int ApCost => Mathf.Max(0, apCost);
        public int AttackDamage => Mathf.Max(0, attackDamage);
        public int HealAmount => Mathf.Max(0, healAmount);
        public int BuffPotency => buffPotency;
        public Sprite Artwork => artwork;
    }
}


========================================
FILE: CardInstance.cs
PATH: Assets/Scripts/CardBattle/Core/CardInstance.cs
========================================
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

========================================
FILE: CardPlayContext.cs
PATH: Assets/Scripts/CardBattle/Core/CardPlayContext.cs
========================================
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

========================================
FILE: CardResolver.cs
PATH: Assets/Scripts/CardBattle/Core/CardResolver.cs
========================================
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

            target.TakeDamage(total);

            if (wasAliveBeforeHit)
            {
                if (target.IsAlive)
                    target.View?.PlayHurt();
                else
                    target.View?.PlayDead();
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

========================================
FILE: CardType.cs
PATH: Assets/Scripts/CardBattle/Core/CardType.cs
========================================
namespace CardBattle.Core
{
    /// <summary>Primary card families; extend with new enum values or parallel systems as content grows.</summary>
    public enum CardType
    {
        Attack,
        Buff,
        Heal
    }
}

========================================
FILE: DeckController.cs
PATH: Assets/Scripts/CardBattle/Core/DeckController.cs
========================================
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

========================================
FILE: EnemyActionSystem.cs
PATH: Assets/Scripts/CardBattle/Core/EnemyActionSystem.cs
========================================
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
            if (player == null)
            {
                Debug.LogError("EnemyActionSystem requires a PlayerBattleUnit reference.");
                return;
            }

            foreach (var enemy in enemies)
                enemy?.ResetRoundCombatFlags();

            player.BeginRoundState();

            if (player.DeckController != null)
                player.DeckController.DrawCards(player.DrawPerRound);
            else
                Debug.LogError("Player is missing a DeckController.");
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

========================================
FILE: EnemyBattleUnit.cs
PATH: Assets/Scripts/CardBattle/Core/EnemyBattleUnit.cs
========================================
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
        public BattleUnitView View => battleUnitView;

        private int _countdown;
        private bool _hasAttackedThisPlayerRound;
        private bool waitingForHit;
        private bool waitingForFinish;
        private PlayerBattleUnit pendingTarget;
        private bool attackInProgress;

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
        }

        /// <summary>Countdown attackers lose one tick after each successful player card.</summary>
        public void StepCountdownAfterPlayerCard()
        {
            if (!IsAlive || Behavior != EnemyBehaviorType.CountdownAttacker)
                return;

            if (_countdown > 0)
                _countdown--;
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

            pendingTarget.TakeDamage(damage);

            if (wasAliveBeforeHit)
            {
                if (pendingTarget.IsAlive)
                    pendingTarget.View?.PlayHurt();
                else
                    pendingTarget.View?.PlayDead();
            }

            _hasAttackedThisPlayerRound = true;
        }
    }
}

========================================
FILE: EnemyBehaviorType.cs
PATH: Assets/Scripts/CardBattle/Core/EnemyBehaviorType.cs
========================================
namespace CardBattle.Core
{
    public enum EnemyBehaviorType
    {
        /// <summary>Strikes the player once when the player ends their turn (if it has not attacked this round).</summary>
        EndTurnAttacker,

        /// <summary>Countdown drops by 1 each time the player successfully plays a card. At 0, attacks during the player's turn.</summary>
        CountdownAttacker
    }
}

========================================
FILE: EnemyData.cs
PATH: Assets/Scripts/CardBattle/Core/EnemyData.cs
========================================
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Static template for an enemy. <see cref="EnemyBattleUnit"/> copies values at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Card Battle/Enemy Data", order = 1)]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId;
        [SerializeField] private string displayName;
        [SerializeField] private EnemyBehaviorType behavior = EnemyBehaviorType.EndTurnAttacker;
        [SerializeField] private int maxHp = 12;
        [SerializeField] private int attackDamage = 2;
        [Tooltip("Used when multiple enemies act on the same beat; higher acts first.")]
        [SerializeField] private int speed = 5;
        [Tooltip("Starting countdown for CountdownAttacker; reapplied after each countdown attack.")]
        [SerializeField] private int baseCountdown = 3;
        [Tooltip("If true, a CountdownAttacker that already attacked this player round may also attack again at end turn.")]
        [SerializeField] private bool allowEndTurnAttackAfterCountdownAttackThisRound = false;

        public string EnemyId => string.IsNullOrEmpty(enemyId) ? name : enemyId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public EnemyBehaviorType Behavior => behavior;
        public int MaxHp => Mathf.Max(1, maxHp);
        public int AttackDamage => Mathf.Max(0, attackDamage);
        public int Speed => speed;
        public int BaseCountdown => Mathf.Max(0, baseCountdown);
        public bool AllowEndTurnAttackAfterCountdownAttackThisRound => allowEndTurnAttackAfterCountdownAttackThisRound;
    }
}

========================================
FILE: ICardModifier.cs
PATH: Assets/Scripts/CardBattle/Core/ICardModifier.cs
========================================
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

========================================
FILE: PlayerBattleUnit.cs
PATH: Assets/Scripts/CardBattle/Core/PlayerBattleUnit.cs
========================================
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
        public BattleUnitView View => battleUnitView;

        private int _pendingAttackBonus;
        private bool _turnCommitted;

        public int CurrentAp { get; private set; }
        public int ApPerRound => apPerRound;
        public int DrawPerRound => drawPerRound;
        public bool HasCommittedTurn => _turnCommitted;
        public bool CanAct => !_turnCommitted && IsAlive;

        public DeckController DeckController => deckController;

        /// <summary>True when the player may attempt to spend AP on a card.</summary>
        public bool CanSpendAp(int amount) => CanAct && CurrentAp >= amount;

        /// <summary>Reset AP, unlock input, and clear transient modifiers at the start of the player's round.</summary>
        public void BeginRoundState()
        {
            _turnCommitted = false;
            CurrentAp = Mathf.Max(0, apPerRound);
            _pendingAttackBonus = 0;
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
            deckController.DiscardEntireHand();
            enemyActionSystem.ResolveEndTurnAttacks();
        }

        /// <summary>Buff cards add to the next attack's damage; consumed when an attack card resolves.</summary>
        public void ApplyBuffFromCard(CardData data)
        {
            if (data == null)
                return;

            _pendingAttackBonus += Mathf.Max(0, data.BuffPotency);
        }

        /// <summary>Called by <see cref="CardResolver"/> when applying attack damage.</summary>
        public int ConsumeDamageBonus()
        {
            var bonus = _pendingAttackBonus;
            _pendingAttackBonus = 0;
            return bonus;
        }

        public void SpendApFromRunner(int amount)
        {
            if (amount <= 0)
                return;

            CurrentAp = Mathf.Max(0, CurrentAp - amount);
        }

        public void CommitEndTurnFromRunner()
        {
            if (!IsAlive || _turnCommitted)
                return;

            _turnCommitted = true;

            if (deckController != null)
                deckController.DiscardEntireHand();
        }
    }
}

========================================
FILE: BattleHUDController.cs
PATH: Assets/Scripts/CardBattle/BattleHUDController.cs
========================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class BattleHUDController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private BattleActionRunner battleActionRunner;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerApText;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private TextMeshProUGUI enemy1Text;
        [SerializeField] private TextMeshProUGUI enemy2Text;
        [SerializeField] private EnemyStatusUI enemyStatusUI1;
        [SerializeField] private EnemyStatusUI enemyStatusUI2;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private HpBarUI playerHpBar;

        private void Start()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnClickEndTurn);
            }

            BindEnemyStatusUI();
            RefreshUI();
        }

        private void Update()
        {
            RefreshUI();
        }

        private void OnClickEndTurn()
        {
            if (battleActionRunner == null)
                return;

            battleActionRunner.TryEndTurn();
        }

        public void RefreshUIExternal()
        {
            RefreshUI();
        }

        private void BindEnemyStatusUI()
        {
            if (enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;

            if (enemyStatusUI1 != null)
            {
                if (enemies.Count > 0 && enemies[0] != null)
                    enemyStatusUI1.SetTarget(enemies[0]);
                else
                    enemyStatusUI1.SetTarget(null);
            }

            if (enemyStatusUI2 != null)
            {
                if (enemies.Count > 1 && enemies[1] != null)
                    enemyStatusUI2.SetTarget(enemies[1]);
                else
                    enemyStatusUI2.SetTarget(null);
            }
        }

        private void RefreshUI()
        {
            if (playerApText != null && player != null)
            {
                //playerApText.text = $"AP: {player.CurrentAp}/{player.ApPerRound}";
                playerApText.text = $"{player.CurrentAp}";
            }

            if (playerHpText != null && player != null)
            {
                playerHpText.text = $"{player.CurrentHp}/{player.MaxHp}";
            }

            if (playerHpBar != null && player != null)
            {
                playerHpBar.SetHp(player.CurrentHp, player.MaxHp);
            }

            var enemies = enemyActionSystem != null ? enemyActionSystem.Enemies : null;
            enemyStatusUI1?.Refresh();
            enemyStatusUI2?.Refresh();

            if (enemy1Text != null)
            {
                enemy1Text.text = BuildEnemyText(enemies, 0);
            }

            if (enemy2Text != null)
            {
                enemy2Text.text = BuildEnemyText(enemies, 1);
            }

            if (endTurnButton != null && player != null)
            {
                bool canClick = player.CanAct;

                if (battleActionRunner != null)
                    canClick = canClick && battleActionRunner.CanAcceptInput;

                endTurnButton.interactable = canClick;
            }
        }

        private string BuildEnemyText(System.Collections.Generic.IReadOnlyList<EnemyBattleUnit> enemies, int index)
        {
            if (enemies == null || index < 0 || index >= enemies.Count || enemies[index] == null)
                return $"Enemy {index + 1}: None";

            var enemy = enemies[index];
            return
                $"{enemy.name}\n" +
                $"HP: {enemy.CurrentHp}/{enemy.MaxHp}\n" +
                $"Type: {enemy.Behavior}\n" +
                $"CD: {enemy.CurrentCountdown}\n" +
                $"SPD: {enemy.Speed}";
        }

        private bool HasAliveEnemy()
        {
            if (enemyActionSystem == null)
                return false;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return true;
            }

            return false;
        }
    }
}

========================================
FILE: BattleUnitView.cs
PATH: Assets/Scripts/CardBattle/BattleUnitView.cs
========================================
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

========================================
FILE: HandUIController.cs
PATH: Assets/Scripts/CardBattle/HandUIController.cs
========================================
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
        }

        private void OnDisable()
        {
            if (deckController != null)
                deckController.OnPilesChanged -= RefreshHandUI;
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
        }

        private void SetupCardView(CardViewUI view, CardInstance card)
        {
            if (view == null || card?.Data == null)
                return;

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

            RefreshHandUI();
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

========================================
FILE: TargetableEnemy.cs
PATH: Assets/Scripts/CardBattle/TargetableEnemy.cs
========================================
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class TargetableEnemy : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private EnemyBattleUnit enemyBattleUnit;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (enemyBattleUnit == null || targetSelectionSystem == null)
                return;

            targetSelectionSystem.ConfirmTarget(enemyBattleUnit);
        }
    }
}

========================================
FILE: TargetSelectionSystem.cs
PATH: Assets/Scripts/CardBattle/TargetSelectionSystem.cs
========================================
using UnityEngine;

namespace CardBattle.Core
{
    public class TargetSelectionSystem : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleActionRunner battleActionRunner;

        public bool IsSelectingTarget => _pendingCard != null;

        private CardInstance _pendingCard;

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
        }

        public void BeginTargetSelection(CardInstance card)
        {
            if (card?.Data == null)
                return;

            _pendingCard = card;
            Debug.Log($"Selecting target for card: {card.Data.DisplayName}");
        }

        public void CancelTargetSelection()
        {
            if (_pendingCard == null)
                return;

            Debug.Log("Target selection cancelled.");

            _pendingCard = null;

            if (handUIController != null)
                handUIController.DeselectCurrentCard();
        }

        public void ConfirmTarget(EnemyBattleUnit target)
        {
            if (_pendingCard == null || battleActionRunner == null || target == null || !target.IsAlive)
                return;

            battleActionRunner.TryPlayCard(_pendingCard, target);
            _pendingCard = null;

            if (handUIController != null)
                handUIController.RefreshHandUI();
        }
    }
}

========================================
FILE: BattleActionRunner.cs
PATH: Assets/Scripts/CardBattle/BattleActionRunner.cs
========================================
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

        [Header("Fallback / Non-Attack Timing")]
        [SerializeField] private float nonAttackResolvePause = 0.05f;
        [SerializeField] private float endTurnPause = 0.2f;
        [SerializeField] private float enemyResolveSafetyPause = 0.1f;

        public bool IsBusy { get; private set; }
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

            IsBusy = true;
            RefreshExternalUI();

            int cost = card.Data.ApCost;
            player.SpendApFromRunner(cost);
            deckController.PlayCardFromHand(card);

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

            if (enemyActionSystem.IsResolvingEnemyActions)
            {
                yield return new WaitUntil(() => !enemyActionSystem.IsResolvingEnemyActions);
            }
            else
            {
                yield return new WaitForSeconds(enemyResolveSafetyPause);
            }

            if (player != null && player.IsAlive && HasAliveEnemy())
                enemyActionSystem.StartPlayerRound();

            RefreshExternalUI();
            IsBusy = false;
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
            handUIController?.RefreshHandUI();
            battleHUDController?.RefreshUIExternal();
        }

        private void OnDisable()
        {
            CleanupPlayerAttackState();
            IsBusy = false;
        }
    }
}

========================================
FILE: CardViewUI.cs
PATH: Assets/Scripts/UI/CardViewUI.cs
========================================
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
                default:
                    return "";
            }
        }
    }
}

========================================
FILE: EnemyStatusUI.cs
PATH: Assets/Scripts/UI/EnemyStatusUI.cs
========================================
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyStatusUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private EnemyBattleUnit targetEnemy;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private HpBarUI hpBarUI;

        public EnemyBattleUnit TargetEnemy => targetEnemy;

        public void SetTarget(EnemyBattleUnit enemy)
        {
            targetEnemy = enemy;
            Refresh();
        }

        public void Refresh()
        {
            if (targetEnemy == null)
            {
                SetEmptyState();
                return;
            }

            if (!targetEnemy.IsAlive)
            {
                gameObject.SetActive(false);
                return;
            }

            if (enemyNameText != null)
            {
                if (targetEnemy.Data != null)
                    enemyNameText.text = targetEnemy.Data.DisplayName;
                else
                    enemyNameText.text = targetEnemy.name;
            }

            if (hpText != null)
                hpText.text = $"{targetEnemy.CurrentHp}/{targetEnemy.MaxHp}";

            if (hpBarUI != null)
                hpBarUI.SetHp(targetEnemy.CurrentHp, targetEnemy.MaxHp);

            if (countdownText != null)
            {
                if (!targetEnemy.IsAlive)
                {
                    countdownText.text = "-";
                }
                else if (targetEnemy.Behavior == EnemyBehaviorType.CountdownAttacker)
                {
                    countdownText.text = $"{targetEnemy.CurrentCountdown}";
                }
                else
                {
                    countdownText.text = "-";
                }
            }
        }

        private void SetEmptyState()
        {
            if (enemyNameText != null)
                enemyNameText.text = "None";

            if (hpText != null)
                hpText.text = "-";

            if (countdownText != null)
                countdownText.text = "-";

            if (hpBarUI != null)
                hpBarUI.SetHp(0, 1);
        }
    }
}

========================================
FILE: HpBarUI.cs
PATH: Assets/Scripts/UI/HpBarUI.cs
========================================
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class HpBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform colorBarRect;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Bar Size")]
        [SerializeField] private float maxWidth = 620f;

        public void SetHp(int currentHp, int maxHp)
        {
            if (maxHp <= 0)
                maxHp = 1;

            currentHp = Mathf.Clamp(currentHp, 0, maxHp);

            float percent = (float)currentHp / maxHp;
            float width = maxWidth * percent;

            Debug.Log($"SetHp called -> HP: {currentHp}/{maxHp}, width: {width}");

            if (colorBarRect != null)
            {
                colorBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                Debug.Log($"Applied to: {colorBarRect.name}, new width: {colorBarRect.rect.width}");
            }

            if (hpText != null)
                hpText.text = $"{currentHp}/{maxHp}";
        }
    }
}

========================================
FILE: PlayerHpBarBinder.cs
PATH: Assets/Scripts/UI/PlayerHpBarBinder.cs
========================================
using UnityEngine;

namespace CardBattle.Core
{
    public class PlayerHpBarBinder : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HpBarUI hpBarUI;

        private void OnEnable()
        {
            if (player != null)
                player.OnHpChangedEvent += HandleHpChanged;
        }

        private void Start()
        {
            RefreshNow();
        }

        private void OnDisable()
        {
            if (player != null)
                player.OnHpChangedEvent -= HandleHpChanged;
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            if (hpBarUI != null)
                hpBarUI.SetHp(currentHp, maxHp);
        }

        [ContextMenu("Refresh Now")]
        public void RefreshNow()
        {
            if (player == null || hpBarUI == null)
                return;

            hpBarUI.SetHp(player.CurrentHp, player.MaxHp);
        }
    }
}

========================================
FILE: WorldToUIFollow.cs
PATH: Assets/Scripts/UI/WorldToUIFollow.cs
========================================
using UnityEngine;

namespace CardBattle.Core
{
    public class WorldToUIFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 worldOffset = Vector3.zero;

        [Header("UI References")]
        [SerializeField] private RectTransform uiRectTransform;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private Camera targetCamera;

        [Header("Options")]
        [SerializeField] private bool hideWhenBehindCamera = true;

        private void Reset()
        {
            uiRectTransform = transform as RectTransform;
            parentCanvas = GetComponentInParent<Canvas>();

            if (Camera.main != null)
                targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (target == null || uiRectTransform == null || parentCanvas == null)
                return;

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera == null)
                return;

            Vector3 worldPos = target.position + worldOffset;
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            if (hideWhenBehindCamera && screenPos.z < 0f)
            {
                if (uiRectTransform.gameObject.activeSelf)
                    uiRectTransform.gameObject.SetActive(false);
                return;
            }

            if (!uiRectTransform.gameObject.activeSelf)
                uiRectTransform.gameObject.SetActive(true);

            RectTransform canvasRect = parentCanvas.transform as RectTransform;

            Camera eventCamera = null;
            if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = targetCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPos,
                    eventCamera,
                    out Vector2 localPoint))
            {
                uiRectTransform.localPosition = localPoint;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}