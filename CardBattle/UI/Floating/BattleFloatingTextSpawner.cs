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
