using UnityEngine;

namespace CardBattle.Core
{
    [System.Serializable]
    public class EncounterEnemySlotData
    {
        [SerializeField] private EnemyBattleUnit enemyPrefab;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private int slotIndex;
        [SerializeField] private Vector3 localPositionOffset;
        [SerializeField] private Vector3 localEulerOffset;

        public EnemyBattleUnit EnemyPrefab => enemyPrefab;
        public EnemyData EnemyData => enemyData;
        public int SlotIndex => slotIndex;
        public Vector3 LocalPositionOffset => localPositionOffset;
        public Vector3 LocalEulerOffset => localEulerOffset;

        public bool IsValid => enemyData != null;
        public bool HasPrefab => enemyPrefab != null;

        public string EnemyId =>
            enemyData != null ? enemyData.EnemyId : string.Empty;
    }
}
