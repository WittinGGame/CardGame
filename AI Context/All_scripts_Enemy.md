================================================================================
FILE: EnemyBehaviorType.cs
PATH: Assets/Scripts/CardBattle/Enemy/Data/EnemyBehaviorType.cs
================================================================================
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

================================================================================
FILE: EnemyData.cs
PATH: Assets/Scripts/CardBattle/Enemy/Data/EnemyData.cs
================================================================================
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

        public string EnemyId => string.IsNullOrEmpty(enemyId) ? name : enemyId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public EnemyBehaviorType Behavior => behavior;
        public int MaxHp => Mathf.Max(1, maxHp);
        public int AttackDamage => Mathf.Max(0, attackDamage);
        public int Speed => speed;
        public int BaseCountdown => Mathf.Max(0, baseCountdown);
        public bool AllowEndTurnAttackAfterCountdownAttackThisRound => allowEndTurnAttackAfterCountdownAttackThisRound;
    }
}

================================================================================
FILE: EnemyTargetHighlight.cs
PATH: Assets/Scripts/CardBattle/Enemy/Interaction/EnemyTargetHighlight.cs
================================================================================
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

================================================================================
FILE: TargetableEnemy.cs
PATH: Assets/Scripts/CardBattle/Enemy/Interaction/TargetableEnemy.cs
================================================================================
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

================================================================================
FILE: EnemyBuffUI.cs
PATH: Assets/Scripts/CardBattle/Enemy/UI/EnemyBuffUI.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    public class EnemyBuffUI : MonoBehaviour
    {
        private EnemyBattleUnit target;

        public void SetTarget(EnemyBattleUnit enemy)
        {
            target = enemy;
        }

        public void Refresh()
        {
            // TODO: ทำจริงทีหลัง
        }
    }
}

================================================================================
FILE: EnemyIntentUI.cs
PATH: Assets/Scripts/CardBattle/Enemy/UI/EnemyIntentUI.cs
================================================================================
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

        public void SetTarget(EnemyBattleUnit enemy)
        {
            target = enemy;
            Refresh();
        }

        public void Refresh()
        {
            if (target == null || !target.IsAlive)
            {
                if (intentRoot != null)
                    intentRoot.SetActive(false);

                return;
            }

            if (intentRoot != null)
                intentRoot.SetActive(true);

            int damage = target.Data != null ? target.Data.AttackDamage : 0;

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
    }
}

================================================================================
FILE: EnemyStatusUI.cs
PATH: Assets/Scripts/CardBattle/Enemy/UI/EnemyStatusUI.cs
================================================================================
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
            if (targetEnemy == null)
            {
                SetEmptyState();
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

================================================================================
FILE: EnemyUIController.cs
PATH: Assets/Scripts/CardBattle/Enemy/UI/EnemyUIController.cs
================================================================================
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
                Debug.LogWarning("[EnemyUIController] Target is null.");
                return;
            }

            Debug.Log($"[EnemyUIController] Binding follow for {target.name}");

            if (hpFollow != null && target.UIAnchorHP != null)
            {
                hpFollow.SetTarget(target.UIAnchorHP);
                Debug.Log($"HP Follow -> {target.UIAnchorHP.name}");
            }
            else
            {
                Debug.LogWarning($"HP Follow missing | hpFollow={(hpFollow != null)} | anchor={(target.UIAnchorHP != null)}");
            }

            if (intentFollow != null && target.UIAnchorIntent != null)
            {
                intentFollow.SetTarget(target.UIAnchorIntent);
                Debug.Log($"Intent Follow -> {target.UIAnchorIntent.name}");
            }
            else
            {
                Debug.LogWarning($"Intent Follow missing | intentFollow={(intentFollow != null)} | anchor={(target.UIAnchorIntent != null)}");
            }

            if (buffFollow != null && target.UIAnchorBuff != null)
            {
                buffFollow.SetTarget(target.UIAnchorBuff);
                Debug.Log($"Buff Follow -> {target.UIAnchorBuff.name}");
            }
            else
            {
                Debug.LogWarning($"Buff Follow missing | buffFollow={(buffFollow != null)} | anchor={(target.UIAnchorBuff != null)}");
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
            subscribedTarget = target;
        }

        private void UnsubscribeTargetEvents()
        {
            if (subscribedTarget == null)
                return;

            subscribedTarget.OnHpChangedEvent -= HandleHpChanged;
            subscribedTarget.OnBlockChangedEvent -= HandleBlockChanged;
            subscribedTarget.OnEnemyStateChanged -= HandleEnemyStateChanged;
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
            if (target == null || !target.IsAlive)
            {
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
                buffUI.gameObject.SetActive(false);
        }

        private void ShowAll()
        {
            if (hpUI != null && !hpUI.gameObject.activeSelf)
                hpUI.gameObject.SetActive(true);

            if (intentUI != null && !intentUI.gameObject.activeSelf)
                intentUI.gameObject.SetActive(true);

            if (buffUI != null && !buffUI.gameObject.activeSelf)
                buffUI.gameObject.SetActive(true);
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

================================================================================
FILE: EnemyUIManager.cs
PATH: Assets/Scripts/CardBattle/Enemy/UI/EnemyUIManager.cs
================================================================================
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