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