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