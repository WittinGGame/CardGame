using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "EncounterRewardConfig",
        menuName = "Card Battle/Rewards/Encounter Reward Config",
        order = 21)]
    public class EncounterRewardConfig : ScriptableObject
    {
        [SerializeField] private int goldReward = 20;
        [SerializeField] private int cardChoiceCount = 3;
        [SerializeField] private CardRewardPool cardRewardPool;

        public int GoldReward => Mathf.Max(0, goldReward);
        public int CardChoiceCount => Mathf.Max(0, cardChoiceCount);
        public CardRewardPool CardRewardPool => cardRewardPool;
    }
}
