namespace CardBattle.Core
{
    public readonly struct StatusDisplayData
    {
        public StatusEffectType Type { get; }
        public int Amount { get; }
        /// <summary>Only describes the aggregate duration when HasMixedDurationTypes is false.</summary>
        public StatusDurationType DurationType { get; }
        public int RemainingDuration { get; }
        public int DisplayNumber { get; }
        public bool IsBuff { get; }
        public bool IsDebuff { get; }
        public bool HasMixedDurationTypes { get; }

        public StatusDisplayData(
            StatusEffectType type,
            int amount,
            StatusDurationType durationType,
            int remainingDuration,
            int displayNumber,
            bool isBuff,
            bool isDebuff)
            : this(type, amount, durationType, remainingDuration, displayNumber, isBuff, isDebuff, false)
        {
        }

        public StatusDisplayData(
            StatusEffectType type,
            int amount,
            StatusDurationType durationType,
            int remainingDuration,
            int displayNumber,
            bool isBuff,
            bool isDebuff,
            bool hasMixedDurationTypes)
        {
            Type = type;
            Amount = amount;
            DurationType = durationType;
            RemainingDuration = remainingDuration;
            DisplayNumber = displayNumber;
            IsBuff = isBuff;
            IsDebuff = isDebuff;
            HasMixedDurationTypes = hasMixedDurationTypes;
        }
    }
}
