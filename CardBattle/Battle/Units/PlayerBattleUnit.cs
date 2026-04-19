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
        public event System.Action<int, int> OnApChangedEvent;
        public event System.Action<bool> OnTurnStateChanged;

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
