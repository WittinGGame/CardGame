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
