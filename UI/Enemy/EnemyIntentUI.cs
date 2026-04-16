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
                intentRoot.SetActive(false);
                return;
            }

            intentRoot.SetActive(true);

            // Attack Value
            int damage = target.Data != null ? target.Data.AttackDamage : 0;
            attackValueText.text = damage.ToString();

            // Countdown
            if (target.Behavior == EnemyBehaviorType.CountdownAttacker)
            {
                countdownValueText.gameObject.SetActive(true);
                countdownValueText.text = target.CurrentCountdown.ToString();
            }
            else
            {
                countdownValueText.gameObject.SetActive(false);
            }
        }
    }
}