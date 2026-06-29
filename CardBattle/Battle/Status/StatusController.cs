using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    public class StatusController : MonoBehaviour
    {
        [SerializeField] private BattleUnit owner;
        [SerializeField] private float weakDamageMultiplier = 0.75f;
        [SerializeField] private float vulnerableDamageMultiplier = 1.5f;
        [SerializeField] private List<StatusInstance> statuses = new();

        public event Action OnStatusesChanged;

        public void SetOwner(BattleUnit value)
        {
            owner = value;
        }

        public void AddStatus(StatusEffectType type, int amount, StatusDurationType durationType, int duration)
        {
            AddStatus(type, amount, durationType, duration, false);
        }

        public void AddStatus(
            StatusEffectType type,
            int amount,
            StatusDurationType durationType,
            int duration,
            bool skipNextTurnTick)
        {
            if (amount <= 0 && type != StatusEffectType.Weak && type != StatusEffectType.Vulnerable)
                return;

            var existing = FindStatus(type);
            if (existing != null)
            {
                if (amount > 0)
                    existing.AddAmount(amount);

                if (durationType != StatusDurationType.Encounter)
                    existing.SetRemainingDurationToMax(duration);

                existing.SetSkipNextTurnTick(skipNextTurnTick);
            }
            else
            {
                var created = new StatusInstance(type, amount, durationType, duration);
                created.SetSkipNextTurnTick(skipNextTurnTick);
                statuses.Add(created);
            }

            RemoveExpiredStatuses();
            NotifyChanged();
        }

        public void ClearAllStatuses()
        {
            if (statuses.Count == 0)
                return;

            statuses.Clear();
            NotifyChanged();
        }

        public bool HasStatus(StatusEffectType type)
        {
            return GetTotalAmount(type) > 0 || FindStatus(type) != null;
        }

        public int GetTotalAmount(StatusEffectType type)
        {
            int total = 0;
            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status.Type == type && !status.IsExpired)
                    total += status.Amount;
            }

            return total;
        }

        public int ModifyOutgoingAttackDamage(int baseDamage, bool consumeOnUse)
        {
            int damage = baseDamage + GetTotalAmount(StatusEffectType.Strength);

            int nextAttackBonus = GetTotalAmount(StatusEffectType.NextAttackBonus);
            if (nextAttackBonus > 0)
            {
                damage += nextAttackBonus;

                if (consumeOnUse)
                {
                    for (int i = statuses.Count - 1; i >= 0; i--)
                    {
                        var status = statuses[i];
                        if (status.Type != StatusEffectType.NextAttackBonus)
                            continue;

                        status.ConsumeUse();
                    }
                }
            }

            if (HasActiveStatus(StatusEffectType.Weak))
                damage = Mathf.FloorToInt(damage * weakDamageMultiplier);

            RemoveExpiredStatuses();
            NotifyChanged();
            return Mathf.Max(0, damage);
        }

        public int ModifyIncomingAttackDamage(int incomingDamage)
        {
            int damage = incomingDamage;

            if (HasActiveStatus(StatusEffectType.Vulnerable))
                damage = Mathf.CeilToInt(damage * vulnerableDamageMultiplier);

            return Mathf.Max(0, damage);
        }

        public void TickTurnDurationStatuses()
        {
            for (int i = 0; i < statuses.Count; i++)
                statuses[i].TickTurn();

            RemoveExpiredStatuses();
            NotifyChanged();
        }

        public void TickOwnerActionDurationStatuses()
        {
            for (int i = 0; i < statuses.Count; i++)
                statuses[i].TickOwnerAction();

            RemoveExpiredStatuses();
            NotifyChanged();
        }

        public string BuildDebugText()
        {
            if (statuses.Count == 0)
                return "(none)";

            var builder = new StringBuilder();
            for (int i = 0; i < statuses.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(statuses[i].ToShortText());
            }

            return builder.ToString();
        }

        private StatusInstance FindStatus(StatusEffectType type)
        {
            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status.Type == type && !status.IsExpired)
                    return status;
            }

            return null;
        }

        private bool HasActiveStatus(StatusEffectType type)
        {
            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status.Type == type && !status.IsExpired)
                    return true;
            }

            return false;
        }

        private void RemoveExpiredStatuses()
        {
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                if (statuses[i].IsExpired)
                    statuses.RemoveAt(i);
            }
        }

        private void NotifyChanged()
        {
            OnStatusesChanged?.Invoke();
        }
    }
}
