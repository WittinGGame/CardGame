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