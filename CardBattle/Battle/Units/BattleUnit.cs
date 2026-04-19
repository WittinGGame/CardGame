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

        public event Action<BattleUnit, int> OnDamageTakenEvent;
        public event Action<BattleUnit, int> OnHealedEvent;

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
            OnDamageTakenEvent?.Invoke(this, amount);

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