using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class TargetableEnemy : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private EnemyBattleUnit enemyBattleUnit;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;

        public void Bind(EnemyBattleUnit enemy, TargetSelectionSystem selectionSystem)
        {
            enemyBattleUnit = enemy;
            targetSelectionSystem = selectionSystem;
        }

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