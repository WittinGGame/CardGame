using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Scene anchor for spawning encounter enemies by slot index.
    /// </summary>
    public class EnemySpawnSlot : MonoBehaviour
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private Transform spawnRoot;

        public int SlotIndex => slotIndex;
        public Transform SpawnRoot => spawnRoot != null ? spawnRoot : transform;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (spawnRoot == null)
                spawnRoot = transform;
        }
#endif
    }
}
