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
        [SerializeField] private EnemyStatusUI enemyStatusUI1;
        [SerializeField] private EnemyStatusUI enemyStatusUI2;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private HpBarUI playerHpBar;

        private void Start()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnClickEndTurn);
            }

            BindEnemyStatusUI();
            RefreshUI();
        }

        private void Update()
        {
            RefreshUI();
        }

        private void OnClickEndTurn()
        {
            if (battleActionRunner == null)
                return;

            battleActionRunner.TryEndTurn();
        }

        public void RefreshUIExternal()
        {
            RefreshUI();
        }

        private void BindEnemyStatusUI()
        {
            if (enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;

            if (enemyStatusUI1 != null)
            {
                if (enemies.Count > 0 && enemies[0] != null)
                    enemyStatusUI1.SetTarget(enemies[0]);
                else
                    enemyStatusUI1.SetTarget(null);
            }

            if (enemyStatusUI2 != null)
            {
                if (enemies.Count > 1 && enemies[1] != null)
                    enemyStatusUI2.SetTarget(enemies[1]);
                else
                    enemyStatusUI2.SetTarget(null);
            }
        }

        private void RefreshUI()
        {
            if (playerApText != null && player != null)
            {
                //playerApText.text = $"AP: {player.CurrentAp}/{player.ApPerRound}";
                playerApText.text = $"{player.CurrentAp}";
            }

            if (playerHpText != null && player != null)
            {
                playerHpText.text = $"{player.CurrentHp}/{player.MaxHp}";
            }

            if (playerHpBar != null && player != null)
            {
                playerHpBar.SetHp(player.CurrentHp, player.MaxHp);
            }

            var enemies = enemyActionSystem != null ? enemyActionSystem.Enemies : null;
            enemyStatusUI1?.Refresh();
            enemyStatusUI2?.Refresh();

            if (endTurnButton != null && player != null)
            {
                bool canClick = player.CanAct;

                if (battleActionRunner != null)
                    canClick = canClick && battleActionRunner.CanAcceptInput;

                endTurnButton.interactable = canClick;
            }
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