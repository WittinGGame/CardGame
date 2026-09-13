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

        private bool ownerCycleActive;

        public void SetOwnerCycleActive(bool active)
        {
            ownerCycleActive = active;
        }

        private BattleActionExecution ownerAction;
        private readonly HashSet<StatusInstance> ownerActionContributions = new();
        private bool ownerActionIsAttack;
        private int attackBonusAmount;
        private readonly HashSet<StatusInstance> attackBonusContributions = new();

        // Called once after validation, before any effects belonging to this owner's action.
        public void BeginOwnerAction(BattleActionExecution execution)
        {
            BeginOwnerAction(execution, false);
        }

        public void BeginOwnerAction(BattleActionExecution execution, bool isAttack)
        {
            if (execution == null || execution.IsComplete || ReferenceEquals(ownerAction, execution))
                return;
            if (ownerAction != null)
                throw new InvalidOperationException("An owner action is already pending.");

            ownerAction = execution;
            ownerActionIsAttack = isAttack;
            attackBonusAmount = 0;
            attackBonusContributions.Clear();
            if (isAttack)
                foreach (var status in statuses)
                    if (status.Type == StatusEffectType.NextAttackBonus && !status.IsExpired)
                    {
                        attackBonusAmount += status.Amount;
                        attackBonusContributions.Add(status);
                    }
            ownerActionContributions.Clear();
            foreach (var status in statuses)
                if (status.DurationType == StatusDurationType.OwnerAction && !status.IsExpired)
                    ownerActionContributions.Add(status);
            execution.Completed += CompleteOwnerAction;
        }

        private void CompleteOwnerAction(BattleActionExecution execution)
        {
            if (!ReferenceEquals(ownerAction, execution))
                return;
            execution.Completed -= CompleteOwnerAction;
            ownerAction = null;
            bool successful = execution.Result == BattleActionResult.Successful;
            // Both lifecycles resolve after every damage packet, from the same result.
            if (successful && ownerActionIsAttack)
                foreach (var status in attackBonusContributions)
                    if (statuses.Contains(status))
                        status.ConsumeUse(); // Only UseCount changes; other duration types are retained.
            ownerActionIsAttack = false;
            attackBonusAmount = 0;
            attackBonusContributions.Clear();
            if (successful)
                foreach (var status in ownerActionContributions)
                    if (statuses.Contains(status))
                        status.TickOwnerAction();
            ownerActionContributions.Clear();
            if (successful)
            {
                RemoveExpiredStatuses();
                NotifyChanged();
            }
        }

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

            // Turn is the serialized name for OwnerCycle. Grants outside the owner's period
            // survive the upcoming boundary, so the opponent gets a full period to interact.
            if (durationType == StatusDurationType.Turn && !ownerCycleActive)
                skipNextTurnTick = true;
            var existing = FindCompatibleContribution(type, durationType, skipNextTurnTick);
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
            return HasActiveStatus(type);
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

            // consumeOnUse is retained for API compatibility. Packets never consume statuses.
            // Preview/debug calls outside an action read the live aggregate without spending it.
            int nextAttackBonus = ownerActionIsAttack
                ? attackBonusAmount : GetTotalAmount(StatusEffectType.NextAttackBonus);
            damage += nextAttackBonus;

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

        // Manual debug compatibility only; gameplay ticks through action completion.
        public void TickOwnerActionDurationStatuses()
        {
            if (ownerAction != null)
                return;
            for (int i = 0; i < statuses.Count; i++)
                statuses[i].TickOwnerAction();

            RemoveExpiredStatuses();
            NotifyChanged();
        }

        public string BuildDebugText()
        {
            if (statuses.Count == 0)
                return "(none)";

            return BuildStatusListText();
        }

        public string BuildStatusDisplayText()
        {
            if (statuses.Count == 0)
                return string.Empty;

            return BuildStatusListText();
        }

        /// <summary>
        /// One UI entry per type. Amounts are summed; homogeneous duration counters use their maximum.
        /// Mixed duration counters are hidden rather than combining incompatible units. Runtime entries stay separate.
        /// </summary>
        public int BuildStatusDisplayData(List<StatusDisplayData> output)
        {
            if (output == null)
                return 0;

            output.Clear();

            for (int i = 0; i < statuses.Count; i++)
            {
                StatusInstance status = statuses[i];
                if (status.IsExpired)
                    continue;

                StatusDisplayData contribution = CreateDisplayData(status);
                int existingIndex = output.FindIndex(entry => entry.Type == status.Type);
                if (existingIndex < 0)
                {
                    output.Add(contribution);
                    continue;
                }

                StatusDisplayData existing = output[existingIndex];
                bool mixedDurationTypes = existing.HasMixedDurationTypes ||
                    existing.DurationType != contribution.DurationType;
                int amount = existing.Amount + contribution.Amount;
                // Never compare turns, actions and uses as though they were the same clock.
                int remainingDuration = mixedDurationTypes ? 0 :
                    Mathf.Max(existing.RemainingDuration, contribution.RemainingDuration);
                bool displaysAmount = status.Type == StatusEffectType.Strength ||
                    status.Type == StatusEffectType.NextAttackBonus;
                int displayNumber = displaysAmount ? amount : remainingDuration;

                output[existingIndex] = new StatusDisplayData(
                    existing.Type, amount, existing.DurationType, remainingDuration,
                    displayNumber, existing.IsBuff, existing.IsDebuff, mixedDurationTypes);
            }

            return output.Count;
        }

        private static StatusDisplayData CreateDisplayData(StatusInstance status)
        {
            int displayNumber;
            bool isBuff;
            bool isDebuff;

            switch (status.Type)
            {
                case StatusEffectType.Strength:
                    displayNumber = status.Amount;
                    isBuff = true;
                    isDebuff = false;
                    break;

                case StatusEffectType.NextAttackBonus:
                    displayNumber = status.Amount;
                    isBuff = true;
                    isDebuff = false;
                    break;

                case StatusEffectType.Weak:
                    displayNumber = status.RemainingDuration;
                    isBuff = false;
                    isDebuff = true;
                    break;

                case StatusEffectType.Vulnerable:
                    displayNumber = status.RemainingDuration;
                    isBuff = false;
                    isDebuff = true;
                    break;

                default:
                    if (status.Amount > 0)
                        displayNumber = status.Amount;
                    else if (status.RemainingDuration > 0)
                        displayNumber = status.RemainingDuration;
                    else
                        displayNumber = 0;

                    isBuff = false;
                    isDebuff = false;
                    break;
            }

            return new StatusDisplayData(
                status.Type,
                status.Amount,
                status.DurationType,
                status.RemainingDuration,
                displayNumber,
                isBuff,
                isDebuff);
        }

        private string BuildStatusListText()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < statuses.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(statuses[i].ToShortText());
            }

            return builder.ToString();
        }

        // Retain existing amount/max-duration refresh for compatible contributions only.
        // A pending skipped Turn tick is a different lifecycle from an immediately ticking contribution.
        private StatusInstance FindCompatibleContribution(
            StatusEffectType type, StatusDurationType durationType, bool skipNextTurnTick)
        {
            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status.Type != type || status.IsExpired || status.DurationType != durationType)
                    continue;
                // Keep bonuses granted during an attack for a future attack, outside this snapshot.
                if (type == StatusEffectType.NextAttackBonus && durationType == StatusDurationType.UseCount &&
                    ownerActionIsAttack && attackBonusContributions.Contains(status))
                    continue;
                // New amounts must not inherit the current action's expiration eligibility.
                if (durationType == StatusDurationType.OwnerAction && ownerAction != null &&
                    ownerActionContributions.Contains(status))
                    continue;
                if (durationType == StatusDurationType.Turn && status.SkipNextTurnTick != skipNextTurnTick)
                    continue;
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
