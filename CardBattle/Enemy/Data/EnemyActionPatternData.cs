using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(fileName = "EnemyActionPattern", menuName = "Card Battle/Enemy Action Pattern", order = 3)]
    public class EnemyActionPatternData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string patternId;
        [SerializeField] private string displayName;

        [Header("Actions")]
        [SerializeField] private EnemyActionData[] actions;

        [Header("Pattern")]
        [SerializeField] private bool loop = true;
        [SerializeField] private EnemyActionPatternAdvanceMode advanceMode =
            EnemyActionPatternAdvanceMode.AfterActionResolved;
        [SerializeField] private int startIndex = 0;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        public string PatternId => string.IsNullOrEmpty(patternId) ? name : patternId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? PatternId : displayName;
        public EnemyActionData[] Actions => actions;
        public bool Loop => loop;
        public EnemyActionPatternAdvanceMode AdvanceMode => advanceMode;
        public int StartIndex => startIndex;
        public bool VerboseLogs => verboseLogs;

        public bool HasValidActions()
        {
            if (actions == null || actions.Length == 0)
                return false;

            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] != null)
                    return true;
            }

            return false;
        }

        public int GetSafeStartIndex()
        {
            if (!HasValidActions())
                return 0;

            return Mathf.Clamp(startIndex, 0, actions.Length - 1);
        }

        public EnemyActionData GetActionAt(int index)
        {
            if (!HasValidActions())
                return null;

            int length = actions.Length;
            int start = Mathf.Clamp(index, 0, length - 1);

            for (int i = 0; i < length; i++)
            {
                int idx = (start + i) % length;
                if (actions[idx] != null)
                    return actions[idx];
            }

            return null;
        }

        public int GetNextIndex(int currentIndex)
        {
            if (!HasValidActions())
                return 0;

            int length = actions.Length;
            currentIndex = Mathf.Clamp(currentIndex, 0, length - 1);

            if (!loop)
            {
                for (int i = currentIndex + 1; i < length; i++)
                {
                    if (actions[i] != null)
                        return i;
                }

                int lastValid = FindLastValidIndex();
                return lastValid >= 0 ? lastValid : currentIndex;
            }

            for (int step = 1; step <= length; step++)
            {
                int idx = (currentIndex + step) % length;
                if (actions[idx] != null)
                    return idx;
            }

            return currentIndex;
        }

        private int FindLastValidIndex()
        {
            if (actions == null)
                return -1;

            for (int i = actions.Length - 1; i >= 0; i--)
            {
                if (actions[i] != null)
                    return i;
            }

            return -1;
        }
    }
}
