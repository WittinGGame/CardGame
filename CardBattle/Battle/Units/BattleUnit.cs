using System;
using UnityEngine;

namespace CardBattle.Core
{
    public abstract class BattleUnit : MonoBehaviour
    {
        [SerializeField] protected int maxHp = 10;
        [SerializeField] protected int currentHp;
        [SerializeField] private StatusController statusController;
        protected int currentBlock;
        private int protectedBlock;
        private bool ownerCycleActive;

        // Called at the owner's turn/phase start, never at an individual action boundary.
        public void BeginOwnerCycle(bool tickStatuses = true)
        {
            if (ownerCycleActive)
                return;
            ownerCycleActive = true;
            int previousBlock = currentBlock;
            currentBlock = protectedBlock;
            protectedBlock = 0;
            statusController?.SetOwnerCycleActive(true);
            if (tickStatuses)
                TickStatusTurnDuration();
            if (currentBlock != previousBlock)
                NotifyBlockChanged();
        }

        public void EndOwnerCycle()
        {
            ownerCycleActive = false;
            statusController?.SetOwnerCycleActive(false);
        }

        public void ResetOwnerCycleState()
        {
            EndOwnerCycle();
            ClearBlock();
        }

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public int CurrentBlock => currentBlock;
        public bool IsAlive => currentHp > 0;
        public StatusController StatusController => statusController;

        public event Action<int, int> OnHpChangedEvent;
        public event Action<int> OnBlockChangedEvent;

        public event Action<BattleUnit, int> OnDamageTakenEvent;
        public event Action<BattleUnit, int> OnBlockAbsorbedEvent;
        public event Action<BattleUnit, int> OnHealedEvent;
        public event Action<BattleUnit> OnDefeatedEvent;

        protected virtual void Awake()
        {
            if (statusController == null)
                statusController = GetComponent<StatusController>();

            if (statusController == null)
                statusController = gameObject.AddComponent<StatusController>();

            statusController.SetOwner(this);

            if (currentHp <= 0)
                currentHp = maxHp;

            NotifyHpChanged();
        }

        /// <returns>Damage applied to HP after block (not raw incoming amount).</returns>
        public virtual int TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return 0;

            bool wasAliveBeforeDamage = IsAlive;
            int remaining = amount;

            if (currentBlock > 0)
            {
                int absorbed = Mathf.Min(currentBlock, remaining);
                currentBlock -= absorbed;
                // Spend older Block first; any protected remainder keeps its original expiry.
                protectedBlock = Mathf.Min(protectedBlock, currentBlock);
                remaining -= absorbed;
                NotifyBlockChanged();

                if (absorbed > 0)
                    OnBlockAbsorbedEvent?.Invoke(this, absorbed);
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

            if (wasAliveBeforeDamage && currentHp == 0)
            {
                OnDefeated();
                OnDefeatedEvent?.Invoke(this);
            }

            return hpDamage;
        }

        public virtual void AddBlock(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentBlock += amount;
            if (!ownerCycleActive)
                protectedBlock += amount;
            NotifyBlockChanged();
        }

        public virtual void ClearBlock()
        {
            protectedBlock = 0;
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

        /// <summary>
        /// Initializes runtime max/current HP from external run data without combat side effects.
        /// </summary>
        public virtual void InitializeVitals(int newMaxHp, int newCurrentHp)
        {
            maxHp = Mathf.Max(1, newMaxHp);
            currentHp = Mathf.Clamp(newCurrentHp, 0, maxHp);
            ResetOwnerCycleState();
            ClearStatuses();

            OnHpChanged();
            NotifyHpChanged();
        }

        public virtual void ApplyStatus(StatusEffectType type, int amount, StatusDurationType durationType, int duration)
        {
            ApplyStatus(type, amount, durationType, duration, false);
        }

        public virtual void ApplyStatus(
            StatusEffectType type,
            int amount,
            StatusDurationType durationType,
            int duration,
            bool skipNextTurnTick)
        {
            if (!IsAlive)
                return;

            statusController?.AddStatus(type, amount, durationType, duration, skipNextTurnTick);
        }

        public virtual void ClearStatuses()
        {
            statusController?.ClearAllStatuses();
        }

        public virtual void TickStatusTurnDuration()
        {
            statusController?.TickTurnDurationStatuses();
        }

        public virtual void TickStatusOwnerActionDuration()
        {
            statusController?.TickOwnerActionDurationStatuses();
        }

        public virtual int CalculateOutgoingAttackDamage(int baseDamage, bool consumeOnUse = true)
        {
            if (statusController == null)
                return Mathf.Max(0, baseDamage);

            return statusController.ModifyOutgoingAttackDamage(baseDamage, consumeOnUse);
        }

        public virtual int CalculateIncomingAttackDamage(int incomingDamage)
        {
            if (statusController == null)
                return Mathf.Max(0, incomingDamage);

            return statusController.ModifyIncomingAttackDamage(incomingDamage);
        }

        /// <summary>Read-only incoming attack strength, before Block/HP. No action or status mutation.</summary>
        public int ProjectAttackDamageAgainst(BattleUnit defender, int baseDamage)
        {
            // Current attack effects skip packets whose base damage is zero.
            if (baseDamage <= 0)
                return 0;
            int outgoing = statusController != null
                ? statusController.ProjectOutgoingAttackDamage(baseDamage) : Mathf.Max(0, baseDamage);
            return defender != null ? defender.CalculateIncomingAttackDamage(outgoing) : outgoing;
        }

        public virtual int TakeAttackDamage(BattleUnit attacker, int baseDamage)
        {
            int outgoingDamage = baseDamage;
            if (attacker != null)
                outgoingDamage = attacker.CalculateOutgoingAttackDamage(baseDamage, true);

            int finalDamage = CalculateIncomingAttackDamage(outgoingDamage);
            return TakeDamage(finalDamage);
        }

        protected virtual void OnHpChanged() { }
        protected virtual void OnDefeated() { }

        private void NotifyHpChanged()
        {
            OnHpChangedEvent?.Invoke(currentHp, maxHp);
        }
    }
}