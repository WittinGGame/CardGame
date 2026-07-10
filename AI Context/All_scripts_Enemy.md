## FILE: EnemyBehaviorType.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/Data/EnemyBehaviorType.cs`
```csharp
namespace CardBattle.Core
{
    public enum EnemyBehaviorType
    {
        /// <summary>Strikes the player once when the player ends their turn (if it has not attacked this round).</summary>
        EndTurnAttacker,

        /// <summary>Countdown drops by 1 each time the player successfully plays a card. At 0, attacks during the player's turn.</summary>
        CountdownAttacker
    }
}
```

## FILE: EnemyData.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/Data/EnemyData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Static template for an enemy. <see cref="EnemyBattleUnit"/> copies values at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Card Battle/Enemy Data", order = 1)]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId;
        [SerializeField] private string displayName;
        [SerializeField] private EnemyBehaviorType behavior = EnemyBehaviorType.EndTurnAttacker;
        [SerializeField] private int maxHp = 12;
        [SerializeField] private int attackDamage = 2;
        [Tooltip("Used when multiple enemies act on the same beat; higher acts first.")]
        [SerializeField] private int speed = 5;
        [Tooltip("Starting countdown for CountdownAttacker; reapplied after each countdown attack.")]
        [SerializeField] private int baseCountdown = 3;
        [Tooltip("If true, a CountdownAttacker that already attacked this player round may also attack again at end turn.")]
        [SerializeField] private bool allowEndTurnAttackAfterCountdownAttackThisRound = false;

        [Header("Action Data")]
        [SerializeField] private EnemyActionData defaultAction;

        [Header("Action Pattern")]
        [SerializeField] private EnemyActionPatternData actionPattern;

        public string EnemyId => string.IsNullOrEmpty(enemyId) ? name : enemyId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public EnemyBehaviorType Behavior => behavior;
        public int MaxHp => Mathf.Max(1, maxHp);
        public int AttackDamage => Mathf.Max(0, attackDamage);
        public int Speed => speed;
        public int BaseCountdown => Mathf.Max(0, baseCountdown);
        public bool AllowEndTurnAttackAfterCountdownAttackThisRound => allowEndTurnAttackAfterCountdownAttackThisRound;
        public EnemyActionData DefaultAction => defaultAction;
        public EnemyActionPatternData ActionPattern => actionPattern;
    }
}
```

## FILE: EnemyActionData.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/Data/EnemyActionData.cs`
```csharp
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
```

## FILE: EnemyActionIntentType.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/Data/EnemyActionIntentType.cs`
```csharp
namespace CardBattle.Core
{
    public enum EnemyActionIntentType
    {
        None,
        Attack,
        Defend,
        Buff,
        Debuff,
        AttackDebuff,
        Special
    }
}
```

## FILE: EnemyActionPatternAdvanceMode.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/Data/EnemyActionPatternAdvanceMode.cs`
```csharp
namespace CardBattle.Core
{
    public enum EnemyActionPatternAdvanceMode
    {
        AfterActionResolved,
        AfterPlayerRoundStart
    }
}
```

## FILE: EnemyActionPatternData.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/Data/EnemyActionPatternData.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "EnemyActionPattern", menuName = "Card Battle/Enemy Action Pattern", order = 3)]
    public class EnemyActionPatternData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string patternId;
        [SerializeField] private string displayName;

        [Header("Actions")]
        [SerializeField] private EnemyActionData[] actions;

        [Header("Pattern")]
        [SerializeField] private bool loop = true;
        [SerializeField] private EnemyActionPatternAdvanceMode advanceMode =
            EnemyActionPatternAdvanceMode.AfterActionResolved;
        [SerializeField] private int startIndex = 0;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        public string PatternId => string.IsNullOrEmpty(patternId) ? name : patternId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? PatternId : displayName;
        public EnemyActionData[] Actions => actions;
        public bool Loop => loop;
        public EnemyActionPatternAdvanceMode AdvanceMode => advanceMode;
        public int StartIndex => startIndex;
        public bool VerboseLogs => verboseLogs;

        public bool HasValidActions()
        {
            if (actions == null || actions.Length == 0)
                return false;

            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] != null)
                    return true;
            }

            return false;
        }

        public int GetSafeStartIndex()
        {
            if (!HasValidActions())
                return 0;

            return Mathf.Clamp(startIndex, 0, actions.Length - 1);
        }

        public EnemyActionData GetActionAt(int index)
        {
            if (!HasValidActions())
                return null;

            int length = actions.Length;
            int start = Mathf.Clamp(index, 0, length - 1);

            for (int i = 0; i < length; i++)
            {
                int idx = (start + i) % length;
                if (actions[idx] != null)
                    return actions[idx];
            }

            return null;
        }

        public int GetNextIndex(int currentIndex)
        {
            if (!HasValidActions())
                return 0;

            int length = actions.Length;
            currentIndex = Mathf.Clamp(currentIndex, 0, length - 1);

            if (!loop)
            {
                for (int i = currentIndex + 1; i < length; i++)
                {
                    if (actions[i] != null)
                        return i;
                }

                int lastValid = FindLastValidIndex();
                return lastValid >= 0 ? lastValid : currentIndex;
            }

            for (int step = 1; step <= length; step++)
            {
                int idx = (currentIndex + step) % length;
                if (actions[idx] != null)
                    return idx;
            }

            return currentIndex;
        }

        private int FindLastValidIndex()
        {
            if (actions == null)
                return -1;

            for (int i = actions.Length - 1; i >= 0; i--)
            {
                if (actions[i] != null)
                    return i;
            }

            return -1;
        }
    }
}
```

## FILE: EnemyTargetHighlight.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/Interaction/EnemyTargetHighlight.cs`
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using CardBattle.Core;

public class EnemyTargetHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private EnemyBattleUnit enemyBattleUnit;
    [SerializeField] private GameObject targetRing;
    [SerializeField] private float hoverMultiplier = 1.2f;

    private bool isSelectable;
    private Vector3 baseScale;

    private void Awake()
    {
        if (targetRing != null)
            baseScale = targetRing.transform.localScale;
    }

    public void Bind(EnemyBattleUnit enemy)
    {
        enemyBattleUnit = enemy;
    }

    public void SetSelectable(bool value)
    {
        bool canSelect = value && enemyBattleUnit != null && enemyBattleUnit.IsAlive;
        isSelectable = canSelect;

        if (targetRing != null)
        {
            targetRing.SetActive(canSelect);
            targetRing.transform.localScale = baseScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelectable || targetRing == null)
            return;

        targetRing.transform.localScale = baseScale * hoverMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelectable || targetRing == null)
            return;

        targetRing.transform.localScale = baseScale;
    }
}
```

## FILE: TargetableEnemy.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/Interaction/TargetableEnemy.cs`
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class TargetableEnemy : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private EnemyBattleUnit enemyBattleUnit;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;

        public void Bind(EnemyBattleUnit enemy, TargetSelectionSystem selectionSystem)
        {
            enemyBattleUnit = enemy;
            targetSelectionSystem = selectionSystem;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (enemyBattleUnit == null || targetSelectionSystem == null)
                return;

            if (!enemyBattleUnit.IsAlive)
                return;

            targetSelectionSystem.ConfirmTarget(enemyBattleUnit);
        }
    }
}
```

## FILE: EnemyIntentUI.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/UI/EnemyIntentUI.cs`
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class EnemyIntentUI : MonoBehaviour
    {
        [SerializeField] private EnemyBattleUnit target;

        [Header("UI")]
        [SerializeField] private GameObject intentRoot;
        [SerializeField] private GameObject countdownRoot;
        [HideInInspector]
        [SerializeField] private TextMeshProUGUI attackValueText;
        [SerializeField] private TextMeshProUGUI countdownValueText;

        [Header("Icon Intent UI")]
        [SerializeField] private Image intentIconImage;
        [SerializeField] private TextMeshProUGUI intentValueText;

        [Header("Intent Icons")]
        [SerializeField] private Sprite attackIcon;
        [SerializeField] private Sprite defendIcon;
        [SerializeField] private Sprite buffIcon;
        [SerializeField] private Sprite debuffIcon;
        [SerializeField] private Sprite attackDebuffIcon;
        [SerializeField] private Sprite specialIcon;
        [SerializeField] private Sprite fallbackAttackIcon;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs;

        private EnemyBattleUnit subscribedTarget;

        private struct IntentVisualData
        {
            public Sprite Icon;
            public string ValueText;
            public string DebugText;
            public bool ShouldShow;
        }

        private void OnEnable()
        {
            SubscribeTarget();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeTarget();
        }

        private void OnDestroy()
        {
            UnsubscribeTarget();
        }

        public void SetTarget(EnemyBattleUnit enemy)
        {
            if (target == enemy)
            {
                Refresh();
                return;
            }

            UnsubscribeTarget();
            target = enemy;
            SubscribeTarget();
            Refresh();
        }

        public void Refresh()
        {
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                !target.IsAlive)
            {
                HideIntent();
                RefreshCountdown();
                return;
            }

            IntentVisualData visual = ResolveIntentVisual(target);

            if (!visual.ShouldShow)
            {
                HideIntent();
                RefreshCountdown();
                return;
            }

            if (intentRoot != null)
                intentRoot.SetActive(true);

            ApplyIntentVisual(visual);
            RefreshCountdown();

            if (verboseLogs)
                Debug.Log($"[EnemyIntentUI] {target.name}: {visual.DebugText}", this);
        }

        private void ApplyIntentVisual(IntentVisualData visual)
        {
            bool useIconLayout = intentIconImage != null;

            if (useIconLayout)
            {
                intentIconImage.sprite = visual.Icon;
                intentIconImage.enabled = visual.Icon != null;

                if (intentValueText != null)
                {
                    bool hasValue = !string.IsNullOrEmpty(visual.ValueText);
                    intentValueText.text = visual.ValueText;
                    intentValueText.gameObject.SetActive(hasValue);
                }
            }
            else if (intentValueText != null)
            {
                intentValueText.gameObject.SetActive(true);
                intentValueText.text = visual.DebugText;
            }
        }

        private void HideIntent()
        {
            if (intentRoot != null)
                intentRoot.SetActive(false);

            ClearIntentVisual();
        }

        private void ClearIntentVisual()
        {
            if (intentIconImage != null)
            {
                intentIconImage.sprite = null;
                intentIconImage.enabled = false;
            }

            if (intentValueText != null)
            {
                intentValueText.text = string.Empty;
                intentValueText.gameObject.SetActive(false);
            }
        }

        private void RefreshCountdown()
        {
            if (target == null)
                return;

            bool showCountdown = target.Behavior == EnemyBehaviorType.CountdownAttacker;

            if (countdownRoot != null)
                countdownRoot.SetActive(showCountdown);

            if (countdownValueText != null)
            {
                countdownValueText.gameObject.SetActive(showCountdown);

                if (showCountdown)
                    countdownValueText.text = target.CurrentCountdown.ToString();
            }
        }

        private void SubscribeTarget()
        {
            if (target == null || subscribedTarget == target)
                return;

            UnsubscribeTarget();
            subscribedTarget = target;
            subscribedTarget.OnPlannedActionChanged += HandlePlannedActionChanged;
        }

        private void UnsubscribeTarget()
        {
            if (subscribedTarget == null)
                return;

            subscribedTarget.OnPlannedActionChanged -= HandlePlannedActionChanged;
            subscribedTarget = null;
        }

        private void HandlePlannedActionChanged(EnemyBattleUnit enemy)
        {
            if (enemy != target)
                return;

            Refresh();
        }

        private IntentVisualData ResolveIntentVisual(EnemyBattleUnit enemy)
        {
            if (enemy == null)
                return HiddenIntentVisual();

            EnemyActionData action = enemy.CurrentPlannedAction;
            if (action == null && enemy.Data != null)
                action = enemy.Data.DefaultAction;

            if (action != null)
                return BuildActionIntentVisual(action);

            return BuildFallbackAttackVisual(enemy);
        }

        private IntentVisualData BuildActionIntentVisual(EnemyActionData action)
        {
            if (action == null)
                return HiddenIntentVisual();

            switch (action.IntentType)
            {
                case EnemyActionIntentType.Attack:
                    return BuildAttackVisual(action, attackIcon);

                case EnemyActionIntentType.AttackDebuff:
                {
                    Sprite icon = attackDebuffIcon != null ? attackDebuffIcon : attackIcon;
                    return BuildAttackVisual(action, icon);
                }

                case EnemyActionIntentType.Debuff:
                    return new IntentVisualData
                    {
                        Icon = debuffIcon,
                        ValueText = string.Empty,
                        DebugText = BuildActionIntentText(action),
                        ShouldShow = true
                    };

                case EnemyActionIntentType.Buff:
                    return new IntentVisualData
                    {
                        Icon = buffIcon,
                        ValueText = string.Empty,
                        DebugText = BuildActionIntentText(action),
                        ShouldShow = true
                    };

                case EnemyActionIntentType.Defend:
                    return new IntentVisualData
                    {
                        Icon = defendIcon,
                        ValueText = action.IntentValue > 0 ? action.IntentValue.ToString() : string.Empty,
                        DebugText = BuildActionIntentText(action),
                        ShouldShow = true
                    };

                case EnemyActionIntentType.Special:
                    return new IntentVisualData
                    {
                        Icon = specialIcon,
                        ValueText = string.Empty,
                        DebugText = BuildActionIntentText(action),
                        ShouldShow = true
                    };

                case EnemyActionIntentType.None:
                default:
                    return HiddenIntentVisual();
            }
        }

        private static IntentVisualData BuildAttackVisual(EnemyActionData action, Sprite icon)
        {
            string valueText = BuildAttackValueText(action);
            string debugText = BuildAttackText(action);

            return new IntentVisualData
            {
                Icon = icon,
                ValueText = valueText,
                DebugText = debugText,
                ShouldShow = true
            };
        }

        private IntentVisualData BuildFallbackAttackVisual(EnemyBattleUnit enemy)
        {
            if (enemy?.Data == null)
                return HiddenIntentVisual();

            int attackDamage = enemy.Data.AttackDamage;
            if (attackDamage <= 0)
            {
                return new IntentVisualData
                {
                    Icon = null,
                    ValueText = string.Empty,
                    DebugText = "None",
                    ShouldShow = false
                };
            }

            Sprite icon = fallbackAttackIcon != null ? fallbackAttackIcon : attackIcon;

            return new IntentVisualData
            {
                Icon = icon,
                ValueText = attackDamage.ToString(),
                DebugText = $"Attack {attackDamage}",
                ShouldShow = true
            };
        }

        private static IntentVisualData HiddenIntentVisual()
        {
            return new IntentVisualData
            {
                Icon = null,
                ValueText = string.Empty,
                DebugText = string.Empty,
                ShouldShow = false
            };
        }

        private static string BuildAttackValueText(EnemyActionData action)
        {
            if (action == null || !action.DealsAttackDamage)
                return string.Empty;

            int damage = action.ResolveDamage();
            if (damage <= 0)
                return string.Empty;

            int hitCount = action.ResolveHitCount();
            if (hitCount <= 1)
                return damage.ToString();

            return $"{damage}x{hitCount}";
        }

        private static string BuildActionIntentText(EnemyActionData action)
        {
            if (action == null)
                return string.Empty;

            switch (action.IntentType)
            {
                case EnemyActionIntentType.Attack:
                    return BuildAttackText(action);

                case EnemyActionIntentType.AttackDebuff:
                    if (action.DealsAttackDamage)
                        return $"{BuildAttackText(action)} + Debuff";

                    return "Debuff";

                case EnemyActionIntentType.Debuff:
                    if (action.ApplyStatusToPlayer)
                    {
                        return BuildStatusIntentLabel(
                            action.PlayerStatusType,
                            action.PlayerStatusDurationType,
                            action.ResolvePlayerStatusDuration());
                    }

                    return "Debuff";

                case EnemyActionIntentType.Buff:
                    if (action.ApplyStatusToSelf)
                        return action.SelfStatusType.ToString();

                    return "Buff";

                case EnemyActionIntentType.Defend:
                    return action.IntentValue > 0
                        ? $"Defend {action.IntentValue}"
                        : "Defend";

                case EnemyActionIntentType.Special:
                    return string.IsNullOrWhiteSpace(action.DisplayName)
                        ? "Special"
                        : action.DisplayName;

                case EnemyActionIntentType.None:
                default:
                    return string.Empty;
            }
        }

        private static string BuildAttackText(EnemyActionData action)
        {
            if (action == null)
                return "Attack";

            if (!action.DealsAttackDamage)
                return "Attack";

            int damage = action.ResolveDamage();
            int hitCount = action.ResolveHitCount();

            if (hitCount <= 1)
                return $"Attack {damage}";

            return $"Attack {damage}x{hitCount}";
        }

        private static string BuildStatusIntentLabel(
            StatusEffectType type,
            StatusDurationType durationType,
            int duration)
        {
            string durationSuffix = BuildStatusDurationSuffix(durationType, duration);
            if (string.IsNullOrEmpty(durationSuffix))
                return type.ToString();

            return $"{type} {durationSuffix}";
        }

        private static string BuildStatusDurationSuffix(StatusDurationType durationType, int duration)
        {
            return durationType switch
            {
                StatusDurationType.Turn => duration == 1 ? "1T" : $"{duration}T",
                StatusDurationType.UseCount => duration == 1 ? "1 use" : $"{duration} uses",
                StatusDurationType.OwnerAction => duration == 1 ? "1 action" : $"{duration} actions",
                StatusDurationType.Encounter => string.Empty,
                _ => string.Empty
            };
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Refresh Intent UI")]
        private void DebugRefreshIntentUI()
        {
            Refresh();
        }
#endif
    }
}
```

## FILE: EnemyStatusUI.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/UI/EnemyStatusUI.cs`
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class EnemyStatusUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private EnemyBattleUnit targetEnemy;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private HpBarUI hpBarUI;

        [Header("Block UI")]
        [SerializeField] private GameObject blockRoot;
        [SerializeField] private TextMeshProUGUI blockValueText;
        [SerializeField] private Image blockIconImage;

        private EnemyBattleUnit subscribedTarget;

        public EnemyBattleUnit TargetEnemy => targetEnemy;

        private void OnEnable()
        {
            SubscribeTarget();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeTarget();
        }

        private void OnDestroy()
        {
            UnsubscribeTarget();
        }

        public void SetTarget(EnemyBattleUnit enemy)
        {
            if (targetEnemy == enemy)
            {
                Refresh();
                return;
            }

            UnsubscribeTarget();
            targetEnemy = enemy;
            SubscribeTarget();
            Refresh();
        }

        public void Refresh()
        {
            if (targetEnemy == null ||
                !targetEnemy.gameObject.activeInHierarchy)
            {
                SetEmptyState();
                gameObject.SetActive(false);
                return;
            }

            if (!targetEnemy.IsAlive)
            {
                RefreshBlock();
                gameObject.SetActive(false);
                return;
            }

            if (enemyNameText != null)
            {
                if (targetEnemy.Data != null)
                    enemyNameText.text = targetEnemy.Data.DisplayName;
                else
                    enemyNameText.text = targetEnemy.name;
            }

            if (hpText != null)
                hpText.text = $"{targetEnemy.CurrentHp}/{targetEnemy.MaxHp}";

            if (hpBarUI != null)
                hpBarUI.SetHp(targetEnemy.CurrentHp, targetEnemy.MaxHp);

            if (countdownText != null)
            {
                if (!targetEnemy.IsAlive)
                {
                    countdownText.text = "-";
                }
                else if (targetEnemy.Behavior == EnemyBehaviorType.CountdownAttacker)
                {
                    countdownText.text = $"{targetEnemy.CurrentCountdown}";
                }
                else
                {
                    countdownText.text = "-";
                }
            }

            RefreshBlock();
        }

        private void RefreshBlock()
        {
            if (blockRoot == null)
                return;

            int block = targetEnemy != null ? targetEnemy.CurrentBlock : 0;
            bool showBlock = targetEnemy != null && targetEnemy.IsAlive && block > 0;

            blockRoot.SetActive(showBlock);

            if (!showBlock)
            {
                if (blockValueText != null)
                    blockValueText.text = string.Empty;

                if (blockIconImage != null)
                    blockIconImage.enabled = false;

                return;
            }

            if (blockValueText != null)
                blockValueText.text = block.ToString();

            if (blockIconImage != null)
                blockIconImage.enabled = blockIconImage.sprite != null;
        }

        private void SetEmptyState()
        {
            if (enemyNameText != null)
                enemyNameText.text = "None";

            if (hpText != null)
                hpText.text = "-";

            if (countdownText != null)
                countdownText.text = "-";

            if (hpBarUI != null)
                hpBarUI.SetHp(0, 1);

            RefreshBlock();
        }

        private void SubscribeTarget()
        {
            if (targetEnemy == null || subscribedTarget == targetEnemy)
                return;

            UnsubscribeTarget();
            subscribedTarget = targetEnemy;
            subscribedTarget.OnBlockChangedEvent += HandleBlockChanged;
        }

        private void UnsubscribeTarget()
        {
            if (subscribedTarget == null)
                return;

            subscribedTarget.OnBlockChangedEvent -= HandleBlockChanged;
            subscribedTarget = null;
        }

        private void HandleBlockChanged(int currentBlock)
        {
            Refresh();
        }
    }
}
```

## FILE: EnemyUIController.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/UI/EnemyUIController.cs`
```csharp
using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyUIController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private EnemyBattleUnit target;

        [Header("UI Parts")]
        [SerializeField] private EnemyStatusUI hpUI;
        [SerializeField] private EnemyIntentUI intentUI;

        [Header("Status Icon UI")]
        [SerializeField] private BattleStatusIconPanelUI statusIconPanelUI;

        [Header("Follow Components")]
        [SerializeField] private WorldToUIFollow hpFollow;
        [SerializeField] private WorldToUIFollow intentFollow;
        [SerializeField] private WorldToUIFollow buffFollow;

        [Header("Options")]
        [SerializeField] private bool verboseLogs;

        public EnemyBattleUnit Target => target;
        public WorldToUIFollow HpFollow => hpFollow;
        public WorldToUIFollow IntentFollow => intentFollow;
        public WorldToUIFollow BuffFollow => buffFollow;

        private EnemyBattleUnit subscribedTarget;

        private void Start()
        {
            if (target != null)
                BindAll();

            RefreshAll();
        }

        private void OnEnable()
        {
            SubscribeTargetEvents();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeTargetEvents();
        }

        public void SetTarget(EnemyBattleUnit enemy)
        {
            UnsubscribeTargetEvents();
            target = enemy;
            BindAll();
            SubscribeTargetEvents();
            RefreshAll();
        }

        public void RefreshNow()
        {
            RefreshAll();
        }

        public void RefreshExternal()
        {
            RefreshAll();
        }

        private void BindAll()
        {
            if (target == null)
            {
                HideAll();
                return;
            }

            if (hpUI != null)
                hpUI.SetTarget(target);

            if (intentUI != null)
                intentUI.SetTarget(target);

            if (statusIconPanelUI != null)
                statusIconPanelUI.SetTarget(target);

            BindFollow();
        }

        private void BindFollow()
        {
            if (target == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[EnemyUIController] Target is null.");

                return;
            }

            if (verboseLogs)
                Debug.Log($"[EnemyUIController] Binding follow for {target.name}");

            if (hpFollow != null && target.UIAnchorHP != null)
            {
                hpFollow.SetTarget(target.UIAnchorHP);

                if (verboseLogs)
                    Debug.Log($"HP Follow -> {target.UIAnchorHP.name}");
            }
            else
            {
                Debug.LogWarning(
                    $"HP Follow missing | hpFollow={(hpFollow != null)} | anchor={(target.UIAnchorHP != null)}");
            }

            if (intentFollow != null && target.UIAnchorIntent != null)
            {
                intentFollow.SetTarget(target.UIAnchorIntent);

                if (verboseLogs)
                    Debug.Log($"Intent Follow -> {target.UIAnchorIntent.name}");
            }
            else
            {
                Debug.LogWarning(
                    $"Intent Follow missing | intentFollow={(intentFollow != null)} | anchor={(target.UIAnchorIntent != null)}");
            }

            if (buffFollow != null && target.UIAnchorBuff != null)
            {
                buffFollow.SetTarget(target.UIAnchorBuff);

                if (verboseLogs)
                    Debug.Log($"Buff Follow -> {target.UIAnchorBuff.name}");
            }
            else
            {
                Debug.LogWarning(
                    $"Buff Follow missing | buffFollow={(buffFollow != null)} | anchor={(target.UIAnchorBuff != null)}");
            }
        }

        private void SubscribeTargetEvents()
        {
            if (target == null || subscribedTarget == target)
                return;

            if (subscribedTarget != null)
                UnsubscribeTargetEvents();

            target.OnHpChangedEvent += HandleHpChanged;
            target.OnBlockChangedEvent += HandleBlockChanged;
            target.OnEnemyStateChanged += HandleEnemyStateChanged;

            if (target.StatusController != null)
                target.StatusController.OnStatusesChanged += HandleStatusesChanged;

            subscribedTarget = target;
        }

        private void UnsubscribeTargetEvents()
        {
            if (subscribedTarget == null)
                return;

            subscribedTarget.OnHpChangedEvent -= HandleHpChanged;
            subscribedTarget.OnBlockChangedEvent -= HandleBlockChanged;
            subscribedTarget.OnEnemyStateChanged -= HandleEnemyStateChanged;

            if (subscribedTarget.StatusController != null)
                subscribedTarget.StatusController.OnStatusesChanged -= HandleStatusesChanged;

            subscribedTarget = null;
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            RefreshAll();
        }

        private void HandleBlockChanged(int currentBlock)
        {
            RefreshAll();
        }

        private void HandleEnemyStateChanged()
        {
            RefreshAll();
        }

        private void HandleStatusesChanged()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (!RefreshVisibility())
                return;

            hpUI?.Refresh();
            intentUI?.Refresh();
            statusIconPanelUI?.Refresh();
        }

        private bool RefreshVisibility()
        {
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                !target.IsAlive)
            {
                if (verboseLogs && target != null && !target.gameObject.activeInHierarchy)
                {
                    Debug.Log(
                        $"[EnemyUIController] Hidden because target is inactive: {target.name}");
                }

                HideAll();
                return false;
            }

            ShowAll();
            return true;
        }

        private void HideAll()
        {
            if (hpUI != null)
                hpUI.gameObject.SetActive(false);

            if (intentUI != null)
                intentUI.gameObject.SetActive(false);

            statusIconPanelUI?.Refresh();

            SetFollowEnabled(false);
        }

        private void ShowAll()
        {
            if (hpUI != null && !hpUI.gameObject.activeSelf)
                hpUI.gameObject.SetActive(true);

            if (intentUI != null && !intentUI.gameObject.activeSelf)
                intentUI.gameObject.SetActive(true);

            SetFollowEnabled(true);
        }

        private void SetFollowEnabled(bool enabled)
        {
            SetFollowComponentEnabled(hpFollow, enabled);
            SetFollowComponentEnabled(intentFollow, enabled);
            SetFollowComponentEnabled(buffFollow, enabled);
        }

        private static void SetFollowComponentEnabled(WorldToUIFollow follow, bool enabled)
        {
            if (follow == null)
                return;

            follow.enabled = enabled;

            if (enabled)
            {
                if (!follow.gameObject.activeSelf)
                    follow.gameObject.SetActive(true);
            }
            else if (follow.gameObject.activeSelf)
            {
                follow.gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Refresh UI")]
        private void DebugRefresh()
        {
            if (target == null)
            {
                Debug.LogWarning("EnemyUIController: No target assigned.");
                return;
            }

            hpUI?.Refresh();
            intentUI?.Refresh();
            statusIconPanelUI?.Refresh();

            Debug.Log($"[EnemyUIController] Refreshed UI for {target.name}");
        }
#endif
    }
}
```

## FILE: EnemyUIManager.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/UI/EnemyUIManager.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyUIManager : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [Header("Scene References")]
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private Camera targetCamera;

        [Header("UI Spawn")]
        [SerializeField] private EnemyUIController enemyUIPrefab;
        [SerializeField] private Transform uiContainer;

        [Header("Options")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool clearBeforeBuild = true;
        [SerializeField] private bool verboseLogs = false;

        private readonly List<EnemyUIController> spawnedUIs = new List<EnemyUIController>();

        private void Start()
        {
            if (buildOnStart)
                RebuildUI();
        }

        [ContextMenu("Rebuild Enemy UI")]
        public void RebuildUI()
        {
            if (!ValidateReferences())
                return;

            if (clearBeforeBuild)
                ClearSpawnedUI();

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null)
                    continue;

                var ui = Instantiate(enemyUIPrefab, uiContainer);
                ui.name = $"EnemyUI_{i}_{enemy.name}";
                ui.SetTarget(enemy);
                SetupWorldToUIFollow(ui);

                spawnedUIs.Add(ui);

                if (verboseLogs)
                    Debug.Log($"[EnemyUIManager] Spawned UI for enemy: {enemy.name}");
            }
        }

        [ContextMenu("Clear Enemy UI")]
        public void ClearSpawnedUI()
        {
            for (int i = 0; i < spawnedUIs.Count; i++)
            {
                if (spawnedUIs[i] != null)
                    Destroy(spawnedUIs[i].gameObject);
            }

            spawnedUIs.Clear();
        }

        public EnemyUIController GetUIForEnemy(EnemyBattleUnit enemy)
        {
            if (enemy == null)
                return null;

            for (int i = 0; i < spawnedUIs.Count; i++)
            {
                if (spawnedUIs[i] == null)
                    continue;

                if (spawnedUIs[i].Target == enemy)
                    return spawnedUIs[i];
            }

            return null;
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (enemyActionSystem == null)
            {
                Debug.LogError("EnemyUIManager: EnemyActionSystem reference is missing.");
                valid = false;
            }

            if (enemyUIPrefab == null)
            {
                Debug.LogError("EnemyUIManager: Enemy UI Prefab reference is missing.");
                valid = false;
            }

            if (uiContainer == null)
            {
                Debug.LogError("EnemyUIManager: UI Container reference is missing.");
                valid = false;
            }

            if (parentCanvas == null)
            {
                Debug.LogError("EnemyUIManager: Parent Canvas reference is missing.");
                valid = false;
            }

            if (targetCamera == null)
            {
                Debug.LogError("EnemyUIManager: Target Camera reference is missing.");
                valid = false;
            }

            return valid;
        }

        private void SetupWorldToUIFollow(EnemyUIController ui)
        {
            if (ui == null)
                return;

            AssignFollow(ui.HpFollow);
            AssignFollow(ui.IntentFollow);
            AssignFollow(ui.BuffFollow);
        }

        private void AssignFollow(WorldToUIFollow follow)
        {
            if (follow == null)
                return;

            follow.SetParentCanvas(parentCanvas);
            follow.SetTargetCamera(targetCamera);
        }
    }
}
```