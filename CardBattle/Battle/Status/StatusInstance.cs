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
        [SerializeField] private bool skipNextTurnTick;

        public StatusEffectType Type => type;
        public int Amount => amount;
        public StatusDurationType DurationType => durationType;
        public int RemainingDuration => remainingDuration;
        public bool SkipNextTurnTick => skipNextTurnTick;

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

        public void SetSkipNextTurnTick(bool value)
        {
            if (durationType == StatusDurationType.Turn)
                skipNextTurnTick = value;
        }

        public void MarkJustAppliedForTurnTick()
        {
            SetSkipNextTurnTick(true);
        }

        public void TickTurn()
        {
            if (durationType != StatusDurationType.Turn)
                return;

            if (skipNextTurnTick)
            {
                skipNextTurnTick = false;
                return;
            }

            remainingDuration = Mathf.Max(0, remainingDuration - 1);
        }

        public void TickOwnerAction()
        {
            if (durationType != StatusDurationType.OwnerAction)
                return;

            remainingDuration = Mathf.Max(0, remainingDuration - 1);
        }

        public void ConsumeUse()
        {
            if (durationType != StatusDurationType.UseCount)
                return;

            remainingDuration--;
        }

        public bool IsExpired =>
            durationType == StatusDurationType.Encounter && amount <= 0
            || durationType == StatusDurationType.Turn && (amount <= 0 || remainingDuration <= 0)
            || durationType == StatusDurationType.UseCount && (amount <= 0 || remainingDuration <= 0)
            || durationType == StatusDurationType.OwnerAction && (amount <= 0 || remainingDuration <= 0);

        public string ToShortText()
        {
            return durationType switch
            {
                StatusDurationType.Encounter => $"{type} {amount}",
                StatusDurationType.Turn => skipNextTurnTick
                    ? $"{type} {amount} ({remainingDuration}T*)"
                    : $"{type} {amount} ({remainingDuration}T)",
                StatusDurationType.UseCount => $"{type} {amount} ({remainingDuration} use)",
                StatusDurationType.OwnerAction => $"{type} {amount} ({remainingDuration} action)",
                _ => $"{type} {amount}"
            };
        }
    }
}
