using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "Card Battle/Effects/Apply Status")]
    public class ApplyStatusEffectData : CardEffectData
    {
        [SerializeField] private StatusEffectType statusType = StatusEffectType.Vulnerable;
        [SerializeField] private int amount = 1;
        [SerializeField] private StatusDurationType durationType = StatusDurationType.Turn;
        [SerializeField] private int duration = 1;

        [Header("Target Override")]
        [SerializeField] private bool forceApplyToPlayer = false;

        [Header("Turn Timing")]
        [SerializeField] private bool skipNextTurnTick = false;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        public override string GetDescriptionText()
        {
            if (IsSelfTarget())
            {
                return statusType switch
                {
                    StatusEffectType.Strength =>
                        $"Gain <color=#B0966E>Strength {amount}</color>",
                    StatusEffectType.NextAttackBonus =>
                        $"Gain <color=#B0966E>Next Attack +{amount}</color>",
                    _ =>
                        $"Gain <color=#B0966E>{BuildStatusLabel()}</color> {BuildDurationText()}"
                };
            }

            return $"Apply <color=#B0966E>{statusType}</color> {BuildDurationText()}";
        }

        public override void Apply(CardPlayContext context, CardEffectExecutionContext executionContext)
        {
            if (context == null)
                return;

            if (amount <= 0 && statusType != StatusEffectType.Weak && statusType != StatusEffectType.Vulnerable)
                return;

            if (forceApplyToPlayer)
            {
                ApplyToUnit(context.Player);
                return;
            }

            var targetMode = context.Card?.Data != null
                ? context.Card.Data.TargetMode
                : CardTargetMode.None;

            switch (targetMode)
            {
                case CardTargetMode.Self:
                    ApplyToUnit(context.Player);
                    break;

                case CardTargetMode.SingleEnemy:
                case CardTargetMode.AllEnemies:
                    if (executionContext?.EnemyTargets == null)
                        return;

                    for (int i = 0; i < executionContext.EnemyTargets.Count; i++)
                        ApplyToUnit(executionContext.EnemyTargets[i]);
                    break;

                case CardTargetMode.None:
                default:
                    if (verboseLogs)
                    {
                        Debug.LogWarning(
                            $"ApplyStatusEffectData: Target mode {targetMode} cannot apply status {statusType}.",
                            this);
                    }
                    break;
            }
        }

        private void ApplyToUnit(BattleUnit unit)
        {
            if (unit == null || !unit.IsAlive)
                return;

            int resolvedAmount = ResolveAmount();

            if (resolvedAmount <= 0)
                return;

            unit.ApplyStatus(
                statusType,
                resolvedAmount,
                durationType,
                ResolveDuration(),
                skipNextTurnTick);

            if (verboseLogs)
            {
                string skipNote = skipNextTurnTick && durationType == StatusDurationType.Turn
                    ? " (delayed tick)"
                    : string.Empty;

                Debug.Log(
                    $"ApplyStatusEffectData: Applied {statusType} {resolvedAmount} to {unit.name}{skipNote}.",
                    this);
            }
        }
       
        private int ResolveAmount()
        {
            switch (statusType)
            {
                case StatusEffectType.Weak:
                case StatusEffectType.Vulnerable:
                    return Mathf.Max(1, amount);

                default:
                    return Mathf.Max(0, amount);
            }
        }

        private int ResolveDuration()
        {
            if (durationType == StatusDurationType.Encounter)
                return Mathf.Max(0, duration);

            return Mathf.Max(1, duration);
        }

        private bool IsSelfTarget()
        {
            return forceApplyToPlayer
                || statusType == StatusEffectType.Strength
                || statusType == StatusEffectType.NextAttackBonus;
        }

        private string BuildDurationText()
        {
            int resolvedDuration = ResolveDuration();

            return durationType switch
            {
                StatusDurationType.Encounter => "for this encounter",
                StatusDurationType.Turn => resolvedDuration == 1
                    ? "for 1 turn"
                    : $"for {resolvedDuration} turns",
                StatusDurationType.UseCount => resolvedDuration == 1
                    ? "for 1 use"
                    : $"for {resolvedDuration} uses",
                StatusDurationType.OwnerAction => resolvedDuration == 1
                    ? "for 1 action"
                    : $"for {resolvedDuration} actions",
                _ => string.Empty
            };
        }

        private string BuildStatusLabel()
        {
            return statusType switch
            {
                StatusEffectType.Strength => $"Strength {amount}",
                StatusEffectType.NextAttackBonus => $"Next Attack +{amount}",
                _ => statusType.ToString()
            };
        }
    }
}
