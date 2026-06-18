using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class EncounterEnemyEntry
    {
        [SerializeField] private string slotId;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private int spawnOrder;

        public string SlotId => slotId;
        public EnemyData EnemyData => enemyData;
        public int SpawnOrder => spawnOrder;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(slotId) &&
            enemyData != null;

        public string EnemyId =>
            enemyData != null ? enemyData.EnemyId : string.Empty;
    }
}
