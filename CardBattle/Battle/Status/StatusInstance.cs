using System;
using UnityEngine;

namespace CardBattle.Core
{
    [Serializable]
    public class StatusInstance
    {
        [SerializeField] private StatusEffectType type;
        [SerializeField] private int amount;
        [SerializeField] private StatusDurationType durationType;
        [SerializeField] private int remainingDuration;

        public StatusEffectType Type => type;
        public int Amount => amount;
        public StatusDurationType DurationType => durationType;
        public int RemainingDuration => remainingDuration;

        public StatusInstance(StatusEffectType type, int amount, StatusDurationType durationType, int duration)
        {
            this.type = type;
            this.amount = amount;
            this.durationType = durationType;
            remainingDuration = duration;
        }

        public void AddAmount(int value)
        {
            amount += value;
        }

        public void SetRemainingDurationToMax(int value)
        {
            remainingDuration = Mathf.Max(remainingDuration, value);
        }

        public void TickTurn()
        {
            if (durationType != StatusDurationType.Turn)
                return;

            remainingDuration--;
        }

        public void ConsumeUse()
        {
            if (durationType != StatusDurationType.UseCount)
                return;

            remainingDuration--;
        }

        public bool IsExpired =>
            durationType == StatusDurationType.Turn && remainingDuration <= 0
            || durationType == StatusDurationType.UseCount && remainingDuration <= 0;

        public string ToShortText()
        {
            return durationType switch
            {
                StatusDurationType.Encounter => $"{type} {amount}",
                StatusDurationType.Turn => $"{type} {amount} ({remainingDuration}T)",
                StatusDurationType.UseCount => $"{type} {amount} ({remainingDuration}U)",
                _ => $"{type} {amount}"
            };
        }
    }
}
