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

        private bool isBound = false;

        public EnemyBattleUnit Target => target;
        public WorldToUIFollow HpFollow => hpFollow;
        public WorldToUIFollow IntentFollow => intentFollow;
        public WorldToUIFollow BuffFollow => buffFollow;

        private void Start()
        {
            if (target != null)
            {
                BindAll();
            }
        }

        private void Update()
        {
            if (!isBound || target == null)
                return;

            if (!target.IsAlive)
            {
                HideAll();
                return;
            }

            ShowAll();

            // fallback update (phase นี้ยังใช้ได้)
            hpUI?.Refresh();
            intentUI?.Refresh();
            buffUI?.Refresh();
        }

        // =========================
        // PUBLIC
        // =========================

        public void SetTarget(EnemyBattleUnit enemy)
        {
            target = enemy;
            BindAll();
        }

        // =========================
        // BINDING
        // =========================

        private void BindAll()
        {
            if (target == null)
                return;

            // bind UI data
            if (hpUI != null)
                hpUI.SetTarget(target);

            if (intentUI != null)
                intentUI.SetTarget(target);

            if (buffUI != null)
                buffUI.SetTarget(target);

            // bind follow anchors
            BindFollow();

            isBound = true;
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