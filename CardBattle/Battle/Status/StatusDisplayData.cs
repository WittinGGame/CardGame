namespace CardBattle.Core
{
    public readonly struct StatusDisplayData
    {
        public StatusEffectType Type { get; }
        public int Amount { get; }
        public StatusDurationType DurationType { get; }
        public int RemainingDuration { get; }
        public int DisplayNumber { get; }
        public bool IsBuff { get; }
        public bool IsDebuff { get; }

        public StatusDisplayData(
            StatusEffectType type,
            int amount,
            StatusDurationType durationType,
            int remainingDuration,
            int displayNumber,
            bool isBuff,
            bool isDebuff)
        {
            Type = type;
            Amount = amount;
            DurationType = durationType;
            RemainingDuration = remainingDuration;
            DisplayNumber = displayNumber;
            IsBuff = isBuff;
            IsDebuff = isDebuff;
        }
    }
}
