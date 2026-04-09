using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Shared combat stats for any unit on the battlefield (player or enemy).
    /// Extend this type for role-specific logic; keep HP and damage hooks centralized.
    /// </summary>
    public abstract class BattleUnit : MonoBehaviour
    {
        [SerializeField] protected int maxHp = 10;
        [SerializeField] protected int currentHp;

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public bool IsAlive => currentHp > 0;

        protected virtual void Awake()
        {
            if (currentHp <= 0)
                currentHp = maxHp;
        }

        /// <summary>Apply damage after any future mitigation hooks (armor, shields, etc.).</summary>
        public virtual void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentHp = Mathf.Max(0, currentHp - amount);
            OnHpChanged();
            if (currentHp == 0)
                OnDefeated();
        }

        public virtual void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            currentHp = Mathf.Min(maxHp, currentHp + amount);
            OnHpChanged();
        }

        public virtual void SetMaxHp(int value, bool refillToMax = false)
        {
            maxHp = Mathf.Max(1, value);
            if (refillToMax)
                currentHp = maxHp;
            else
                currentHp = Mathf.Min(currentHp, maxHp);
            OnHpChanged();
        }

        protected virtual void OnHpChanged() { }
        protected virtual void OnDefeated() { }
    }
}
