using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "EnemyAction", menuName = "Card Battle/Enemy Action Data", order = 2)]
    public class EnemyActionData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string actionId;
        [SerializeField] private string displayName;

        [Header("Intent")]
        [SerializeField] private EnemyActionIntentType intentType = EnemyActionIntentType.Attack;
        [SerializeField] private int intentValue = 0;

        [Header("Attack")]
        [SerializeField] private bool dealsAttackDamage = true;
        [SerializeField] private int damage = 0;
        [SerializeField] private int hitCount = 1;
        [SerializeField] private float delayBetweenHits = 0.08f;

        [Header("Apply Status To Player")]
        [SerializeField] private bool applyStatusToPlayer = false;
        [SerializeField] private StatusEffectType playerStatusType = StatusEffectType.Weak;
        [SerializeField] private int playerStatusAmount = 1;
        [SerializeField] private StatusDurationType playerStatusDurationType = StatusDurationType.Turn;
        [SerializeField] private int playerStatusDuration = 1;
        [SerializeField] private bool playerStatusSkipNextTurnTick = true;

        [Header("Apply Status To Self")]
        [SerializeField] private bool applyStatusToSelf = false;
        [SerializeField] private StatusEffectType selfStatusType = StatusEffectType.Strength;
        [SerializeField] private int selfStatusAmount = 1;
        [SerializeField] private StatusDurationType selfStatusDurationType = StatusDurationType.OwnerAction;
        [SerializeField] private int selfStatusDuration = 1;
        [SerializeField] private bool selfStatusSkipNextTurnTick = false;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        public string ActionId => string.IsNullOrEmpty(actionId) ? name : actionId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? ActionId : displayName;
        public EnemyActionIntentType IntentType => intentType;
        public int IntentValue => intentValue;
        public bool DealsAttackDamage => dealsAttackDamage;
        public int Damage => Mathf.Max(0, damage);
        public int HitCount => Mathf.Max(1, hitCount);
        public float DelayBetweenHits => Mathf.Max(0f, delayBetweenHits);
        public bool ApplyStatusToPlayer => applyStatusToPlayer;
        public StatusEffectType PlayerStatusType => playerStatusType;
        public int PlayerStatusAmount => playerStatusAmount;
        public StatusDurationType PlayerStatusDurationType => playerStatusDurationType;
        public int PlayerStatusDuration => playerStatusDuration;
        public bool PlayerStatusSkipNextTurnTick => playerStatusSkipNextTurnTick;
        public bool ApplyStatusToSelf => applyStatusToSelf;
        public StatusEffectType SelfStatusType => selfStatusType;
        public int SelfStatusAmount => selfStatusAmount;
        public StatusDurationType SelfStatusDurationType => selfStatusDurationType;
        public int SelfStatusDuration => selfStatusDuration;
        public bool SelfStatusSkipNextTurnTick => selfStatusSkipNextTurnTick;
        public bool VerboseLogs => verboseLogs;

        public int ResolveDamage() => Damage;

        public int ResolveHitCount() => HitCount;

        public float ResolveDelayBetweenHits() => DelayBetweenHits;

        public int ResolvePlayerStatusAmount() => ResolveStatusAmount(playerStatusType, playerStatusAmount);

        public int ResolvePlayerStatusDuration() => ResolveStatusDuration(playerStatusDurationType, playerStatusDuration);

        public int ResolveSelfStatusAmount() => ResolveStatusAmount(selfStatusType, selfStatusAmount);

        public int ResolveSelfStatusDuration() => ResolveStatusDuration(selfStatusDurationType, selfStatusDuration);

        private static int ResolveStatusAmount(StatusEffectType type, int value)
        {
            if (type == StatusEffectType.Weak || type == StatusEffectType.Vulnerable)
                return Mathf.Max(1, value);

            return Mathf.Max(0, value);
        }

        private static int ResolveStatusDuration(StatusDurationType type, int value)
        {
            if (type == StatusDurationType.Encounter)
                return Mathf.Max(0, value);

            return Mathf.Max(1, value);
        }
    }
}
