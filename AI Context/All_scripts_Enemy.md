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

## FILE: EnemyBuffUI.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/UI/EnemyBuffUI.cs`
```csharp
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyBuffUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private bool hideWhenEmpty = true;

        private EnemyBattleUnit target;
        private StatusController subscribedStatusController;

        public void SetTarget(EnemyBattleUnit enemy)
        {
            if (target == enemy)
            {
                Refresh();
                return;
            }

            UnsubscribeStatusController();
            target = enemy;
            SubscribeStatusController();
            Refresh();
        }

        public void Refresh()
        {
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                !target.IsAlive ||
                target.StatusController == null)
            {
                ShowEmpty();
                return;
            }

            string displayText = ResolveDisplayText(target.StatusController);
            if (string.IsNullOrEmpty(displayText))
            {
                ShowEmpty();
                return;
            }

            if (statusText != null)
                statusText.text = displayText;

            if (root != null)
                root.SetActive(true);
        }

        private void OnEnable()
        {
            SubscribeStatusController();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeStatusController();
        }

        private void OnDestroy()
        {
            UnsubscribeStatusController();
        }

        private void SubscribeStatusController()
        {
            if (target?.StatusController == null || subscribedStatusController == target.StatusController)
                return;

            UnsubscribeStatusController();
            subscribedStatusController = target.StatusController;
            subscribedStatusController.OnStatusesChanged += HandleStatusesChanged;
        }

        private void UnsubscribeStatusController()
        {
            if (subscribedStatusController == null)
                return;

            subscribedStatusController.OnStatusesChanged -= HandleStatusesChanged;
            subscribedStatusController = null;
        }

        private void HandleStatusesChanged()
        {
            Refresh();
        }

        private void ShowEmpty()
        {
            if (statusText != null)
                statusText.text = string.Empty;

            if (root != null && hideWhenEmpty)
                root.SetActive(false);
        }

        private static string ResolveDisplayText(StatusController controller)
        {
            if (controller == null)
                return string.Empty;

            string displayText = controller.BuildStatusDisplayText();
            if (!string.IsNullOrEmpty(displayText))
                return displayText;

            string debugText = controller.BuildDebugText();
            return debugText == "(none)" ? string.Empty : debugText;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Refresh Enemy Status UI")]
        private void DebugRefreshEnemyStatusUI()
        {
            Refresh();
        }
#endif
    }
}
```

## FILE: EnemyIntentUI.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/UI/EnemyIntentUI.cs`
```csharp
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyIntentUI : MonoBehaviour
    {
        [SerializeField] private EnemyBattleUnit target;

        [Header("UI")]
        [SerializeField] private GameObject intentRoot;
        [SerializeField] private GameObject countdownRoot;
        [SerializeField] private TextMeshProUGUI attackValueText;
        [SerializeField] private TextMeshProUGUI countdownValueText;

        private EnemyBattleUnit subscribedTarget;

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
                if (intentRoot != null)
                    intentRoot.SetActive(false);

                return;
            }

            if (intentRoot != null)
                intentRoot.SetActive(true);

            int damage = ResolveIntentValue(target);

            if (attackValueText != null)
                attackValueText.text = damage.ToString();

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

        private static int ResolveIntentValue(EnemyBattleUnit enemy)
        {
            if (enemy == null)
                return 0;

            EnemyActionData action = enemy.CurrentPlannedAction;
            if (action == null && enemy.Data != null)
                action = enemy.Data.DefaultAction;

            if (action != null)
            {
                if (action.IntentValue > 0)
                    return action.IntentValue;

                if (action.DealsAttackDamage)
                    return action.Damage;

                return 0;
            }

            return enemy.Data != null ? enemy.Data.AttackDamage : 0;
        }
    }
}
```

## FILE: EnemyStatusUI.cs
**Path:** `Assets/Scripts/CardBattle/Enemy/UI/EnemyStatusUI.cs`
```csharp
using TMPro;
using UnityEngine;

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

        public EnemyBattleUnit TargetEnemy => targetEnemy;

        public void SetTarget(EnemyBattleUnit enemy)
        {
            targetEnemy = enemy;
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
        [SerializeField] private EnemyBuffUI buffUI; // optional

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

        // =========================
        // PUBLIC
        // =========================

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

        // =========================
        // BINDING
        // =========================

        private void BindAll()
        {
            if (target == null)
            {
                HideAll();
                return;
            }

            // bind UI data
            if (hpUI != null)
                hpUI.SetTarget(target);

            if (intentUI != null)
                intentUI.SetTarget(target);

            if (buffUI != null)
                buffUI.SetTarget(target);

            // bind follow anchors
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
            {
                return;
            }

            hpUI?.Refresh();
            intentUI?.Refresh();
            buffUI?.Refresh();
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

        // =========================
        // VISIBILITY
        // =========================

        private void HideAll()
        {
            if (hpUI != null)
                hpUI.gameObject.SetActive(false);

            if (intentUI != null)
                intentUI.gameObject.SetActive(false);

            if (buffUI != null)
                buffUI.Refresh();

            SetFollowEnabled(false);
        }

        private void ShowAll()
        {
            if (hpUI != null && !hpUI.gameObject.activeSelf)
                hpUI.gameObject.SetActive(true);

            if (intentUI != null && !intentUI.gameObject.activeSelf)
                intentUI.gameObject.SetActive(true);

            if (buffUI != null && !buffUI.gameObject.activeSelf)
                buffUI.gameObject.SetActive(true);

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

        // =========================
        // DEBUG
        // =========================

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
            buffUI?.Refresh();

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