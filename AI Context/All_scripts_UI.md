================================================================================
FILE: BattleHUDController.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/BattleHUDController.cs
================================================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class BattleHUDController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private BattleActionRunner battleActionRunner;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerApText;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private GameObject playerBlockRoot;
        [SerializeField] private TextMeshProUGUI playerBlockText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private HpBarUI playerHpBar;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;

        private void Start()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnClickEndTurn);
            }

            BindEnemyStatusUI();
            if (player != null)
            {
                HandlePlayerHpChanged(player.CurrentHp, player.MaxHp);
                HandlePlayerApChanged(player.CurrentAp, player.ApPerRound);
                HandlePlayerBlockChanged(player.CurrentBlock);
            }
            RefreshUIExternal();
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.OnHpChangedEvent += HandlePlayerHpChanged;
                player.OnApChangedEvent += HandlePlayerApChanged;
                player.OnTurnStateChanged += HandleTurnStateChanged;
                player.OnBlockChangedEvent += HandlePlayerBlockChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged += HandleBusyStateChanged;
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.OnHpChangedEvent -= HandlePlayerHpChanged;
                player.OnApChangedEvent -= HandlePlayerApChanged;
                player.OnTurnStateChanged -= HandleTurnStateChanged;
                player.OnBlockChangedEvent -= HandlePlayerBlockChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged -= HandleBusyStateChanged;
        }

        private void OnClickEndTurn()
        {
            if (battleActionRunner == null)
                return;

            if (targetSelectionSystem != null && targetSelectionSystem.IsSelectingTarget)
                targetSelectionSystem.CancelTargetSelection();

            battleActionRunner.TryEndTurn();
        }

        public void RefreshUIExternal()
        {
            RefreshEndTurnButtonState();
        }

        private void BindEnemyStatusUI()
        {
            if (enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;
        }

        private void HandlePlayerHpChanged(int currentHp, int maxHp)
        {
            if (playerHpText != null)
                playerHpText.text = $"{currentHp}/{maxHp}";

            if (playerHpBar != null)
                playerHpBar.SetHp(currentHp, maxHp);
        }

        private void HandlePlayerApChanged(int currentAp, int maxAp)
        {
            if (playerApText != null)
                playerApText.text = $"{currentAp}";
        }

        private void HandlePlayerBlockChanged(int currentBlock)
        {
            if (playerBlockText != null)
                playerBlockText.text = currentBlock.ToString();

            if (playerBlockRoot != null)
                playerBlockRoot.SetActive(currentBlock > 0);
        }

        private void HandleTurnStateChanged(bool canAct)
        {
            RefreshEndTurnButtonState();
        }

        private void HandleBusyStateChanged(bool isBusy)
        {
            RefreshEndTurnButtonState();
        }

        private void RefreshEndTurnButtonState()
        {
            if (endTurnButton == null || player == null)
                return;

            bool canClick = player.CanAct;

            if (battleActionRunner != null)
                canClick = canClick && battleActionRunner.CanAcceptInput;

            endTurnButton.interactable = canClick;
        }

        private bool HasAliveEnemy()
        {
            if (enemyActionSystem == null)
                return false;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return true;
            }

            return false;
        }
    }
}

================================================================================
FILE: HpBarUI.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/HpBarUI.cs
================================================================================
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class HpBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform colorBarRect;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Bar Size")]
        [SerializeField] private float maxWidth = 620f;

        public void SetHp(int currentHp, int maxHp)
        {
            if (maxHp <= 0)
                maxHp = 1;

            currentHp = Mathf.Clamp(currentHp, 0, maxHp);

            float percent = (float)currentHp / maxHp;
            float width = maxWidth * percent;

            if (colorBarRect != null)
            {
                colorBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            if (hpText != null)
                hpText.text = $"{currentHp}/{maxHp}";
        }
    }
}

================================================================================
FILE: PlayerHpBarBinder.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/PlayerHpBarBinder.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    public class PlayerHpBarBinder : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HpBarUI hpBarUI;

        private void OnEnable()
        {
            if (player != null)
                player.OnHpChangedEvent += HandleHpChanged;
        }

        private void Start()
        {
            RefreshNow();
        }

        private void OnDisable()
        {
            if (player != null)
                player.OnHpChangedEvent -= HandleHpChanged;
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            if (hpBarUI != null)
                hpBarUI.SetHp(currentHp, maxHp);
        }

        [ContextMenu("Refresh Now")]
        public void RefreshNow()
        {
            if (player == null || hpBarUI == null)
                return;

            hpBarUI.SetHp(player.CurrentHp, player.MaxHp);
        }
    }
}

================================================================================
FILE: WorldToUIFollow.cs
PATH: Assets/Scripts/CardBattle/UI/World/WorldToUIFollow.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    public class WorldToUIFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 worldOffset = Vector3.zero;

        [Header("UI References")]
        [SerializeField] private RectTransform uiRectTransform;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private Camera targetCamera;

        [Header("Options")]
        [SerializeField] private bool hideWhenBehindCamera = true;

        private void Reset()
        {
            uiRectTransform = transform as RectTransform;
            parentCanvas = GetComponentInParent<Canvas>();

            if (Camera.main != null)
                targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (target == null || uiRectTransform == null || parentCanvas == null)
                return;

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera == null)
                return;

            Vector3 worldPos = target.position + worldOffset;
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            if (hideWhenBehindCamera && screenPos.z < 0f)
            {
                if (uiRectTransform.gameObject.activeSelf)
                    uiRectTransform.gameObject.SetActive(false);
                return;
            }

            if (!uiRectTransform.gameObject.activeSelf)
                uiRectTransform.gameObject.SetActive(true);

            RectTransform canvasRect = parentCanvas.transform as RectTransform;

            Camera eventCamera = null;
            if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = targetCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPos,
                    eventCamera,
                    out Vector2 localPoint))
            {
                uiRectTransform.localPosition = localPoint;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetParentCanvas(Canvas canvas)
        {
            parentCanvas = canvas;
        }

        public void SetTargetCamera(Camera cam)
        {
            targetCamera = cam;
        }
    }
}

================================================================================
FILE: FloatingTextUI.cs
PATH: Assets/Scripts/CardBattle/UI/Floating/FloatingTextUI.cs
================================================================================
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Simple floating TMP line: rises in canvas space, fades out, then self-destroys.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FloatingTextUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float lifetime = 1.2f;
        [SerializeField] private float riseSpeedPixelsPerSecond = 72f;

        private RectTransform _rect;
        private float _elapsed;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Configure display and reset motion state. Call right after instantiate.</summary>
        public void Play(string text, Color color)
        {
            if (label != null)
            {
                label.text = text;
                label.color = color;
            }

            _elapsed = 0f;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (_rect != null)
                _rect.anchoredPosition += Vector2.up * (riseSpeedPixelsPerSecond * Time.deltaTime);

            _elapsed += Time.deltaTime;
            float t = lifetime > 0f ? Mathf.Clamp01(_elapsed / lifetime) : 1f;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            if (_elapsed >= lifetime)
                Destroy(gameObject);
        }
    }
}


================================================================================
FILE: BattleFloatingTextSpawner.cs
PATH: Assets/Scripts/CardBattle/UI/Floating/BattleFloatingTextSpawner.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Listens for damage/heal on the player and registered enemies; spawns floating text at a world anchor.
    /// </summary>
    public class BattleFloatingTextSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform container;
        [SerializeField] private FloatingTextUI floatingTextPrefab;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [Header("Spawn anchors")]
        [SerializeField] private Transform playerAnchor;
        [SerializeField] private Vector3 defaultWorldOffset = new Vector3(0f, 0.5f, 0f);

        [Header("Screen space offsets")]
        [SerializeField] private Vector2 playerScreenOffset = Vector2.zero;
        [SerializeField] private Vector2 enemyScreenOffset = Vector2.zero;

        [Header("World cameras")]
        [Tooltip("Used for WorldToScreenPoint from world-space units. Falls back to Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Header("Presentation")]
        [SerializeField] private Color damageColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color healColor = new Color(0.45f, 1f, 0.55f, 1f);

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private void OnEnable()
        {
            SubscribePlayer();
            SubscribeEnemies();
        }

        private void OnDisable()
        {
            UnsubscribePlayer();
            UnsubscribeEnemies();
        }

        /// <summary>Call after registering new enemies at runtime so they receive floating text.</summary>
        public void RefreshEnemySubscriptions()
        {
            UnsubscribeEnemies();
            SubscribeEnemies();
        }

        private void SubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent += HandleUnitDamageTaken;
            player.OnHealedEvent += HandleUnitHealed;
        }

        private void UnsubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent -= HandleUnitDamageTaken;
            player.OnHealedEvent -= HandleUnitHealed;
        }

        private void SubscribeEnemies()
        {
            if (enemyActionSystem == null)
                return;

            var list = enemyActionSystem.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var enemy = list[i];
                if (enemy == null)
                    continue;

                enemy.OnDamageTakenEvent += HandleUnitDamageTaken;
                enemy.OnHealedEvent += HandleUnitHealed;
            }
        }

        private void UnsubscribeEnemies()
        {
            if (enemyActionSystem == null)
                return;

            var list = enemyActionSystem.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var enemy = list[i];
                if (enemy == null)
                    continue;

                enemy.OnDamageTakenEvent -= HandleUnitDamageTaken;
                enemy.OnHealedEvent -= HandleUnitHealed;
            }
        }

        private void HandleUnitDamageTaken(BattleUnit unit, int amount)
        {
            SpawnAtUnit(unit, amount, false);
        }

        private void HandleUnitHealed(BattleUnit unit, int amount)
        {
            SpawnAtUnit(unit, amount, true);
        }

        private void SpawnAtUnit(BattleUnit unit, int amount, bool isHeal)
        {
            if (unit == null || floatingTextPrefab == null || container == null)
                return;

            Vector3 worldPos = GetSpawnWorldPosition(unit);
            if (!TryWorldToContainerLocal(worldPos, out Vector2 localPoint))
                return;

            string text = isHeal ? $"+{amount}" : $"-{amount}";
            Color color = isHeal ? healColor : damageColor;

            var instance = Instantiate(floatingTextPrefab, container);
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = localPoint + GetScreenOffset(unit);

            instance.Play(text, color);
        }

        private Vector2 GetScreenOffset(BattleUnit unit)
        {
            if (unit == null)
                return Vector2.zero;

            if (player != null && unit == player)
                return playerScreenOffset;

            if (unit is EnemyBattleUnit)
                return enemyScreenOffset;

            return Vector2.zero;
        }

        private Vector3 GetSpawnWorldPosition(BattleUnit unit)
        {
            if (unit is EnemyBattleUnit enemy)
            {
                if (enemy.UIAnchorDamage != null)
                    return enemy.UIAnchorDamage.position;

                if (enemy.UIAnchorHP != null)
                    return enemy.UIAnchorHP.position;
            }

            if (player != null && unit == player && playerAnchor != null)
                return playerAnchor.position;

            return unit.transform.position + defaultWorldOffset;
        }

        private bool TryWorldToContainerLocal(Vector3 worldPosition, out Vector2 localPoint)
        {
            localPoint = default;

            if (canvas == null)
                canvas = container.GetComponentInParent<Canvas>();
            if (canvas == null)
                return false;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
                return false;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera != null ? canvas.worldCamera : cam;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPoint, eventCamera, out localPoint);
        }
    }
}

