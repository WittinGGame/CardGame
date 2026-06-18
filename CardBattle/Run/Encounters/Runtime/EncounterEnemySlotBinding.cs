using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class EncounterEnemySlotBinding
    {
        [SerializeField] private string slotId;
        [SerializeField] private EnemyBattleUnit enemyUnit;

        public string SlotId => slotId;
        public EnemyBattleUnit EnemyUnit => enemyUnit;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(slotId) &&
            enemyUnit != null;
    }
}
