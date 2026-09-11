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

        private BattleActionRunner actionRunner;

        public void BindActionRunner(BattleActionRunner runner)
        {
            actionRunner = runner;
        }

        private BattleActionRunner ResolveActionRunner()
        {
            if (actionRunner != null && actionRunner.Player == this)
                return actionRunner;
            foreach (var runner in FindObjectsByType<BattleActionRunner>(FindObjectsSortMode.None))
            {
                if (runner.Player != this) continue;
                actionRunner = runner;
                return runner;
            }
            Debug.LogError("Player requires BattleActionRunner; synchronous gameplay bypass is disabled.", this);
            return null;
        }

        /// <summary>Compatibility API: returns whether the asynchronous runner accepted the card.</summary>
        public bool TryPlayCard(CardInstance card, EnemyBattleUnit primaryTarget = null)
        {
            var runner = ResolveActionRunner();
            return runner != null && runner.TryStartCard(card, primaryTarget);
        }

        public void RequestEndTurn()
        {
            ResolveActionRunner()?.TryEndTurn();
        }

        /// <summary>Called by attack effects when applying damage.</summary>
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

        /// <summary>
        /// Gains AP for card effects. May exceed <see cref="ApPerRound"/>;
        /// round start still resets to the per-round value.
        /// </summary>
        /// <returns>The actual amount gained (0 if blocked or amount is non-positive).</returns>
        public int GainAp(int amount)
        {
            if (amount <= 0 || !IsAlive || _turnCommitted)
                return 0;

            int before = CurrentAp;
            long resolved = (long)CurrentAp + amount;
            CurrentAp = resolved >= int.MaxValue ? int.MaxValue : (int)resolved;

            int gained = CurrentAp - before;
            if (gained > 0)
                NotifyApChanged();

            return gained;
        }

        public void CommitEndTurnFromRunner()
        {
            if (!IsAlive || _turnCommitted)
                return;

            _turnCommitted = true;
            NotifyTurnStateChanged();
        }

        /// <summary>Clears transient combat state before a new encounter battle start. Does not change HP.</summary>
        public void ResetBattleRuntimeStateForNewEncounter()
        {
            _turnCommitted = true;
            CurrentAp = 0;
            _pendingAttackBonus = 0;
            ClearBlock();
            ClearStatuses();
            OnDebugBuffChanged?.Invoke(DebugBuffCount);
            NotifyApChanged();
            NotifyTurnStateChanged();
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
