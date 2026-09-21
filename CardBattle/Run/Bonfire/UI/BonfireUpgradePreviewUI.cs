using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    // Presentation only. All input values come from the existing owned-card resolver/catalog.
    public class BonfireUpgradePreviewUI : MonoBehaviour
    {
        [SerializeField] private UpgradeCardChoiceView currentCard;
        [SerializeField] private UpgradeCardChoiceView guaranteedCard;
        [SerializeField] private TextMeshProUGUI mutationChanceText;

        public void Bind(CardInstance current, CardUpgradeDefinition guaranteed, float mutationChance)
        {
            if (current?.Data == null || guaranteed == null || !guaranteed.HasValidSequence) return;
            if (currentCard != null) currentCard.BindPreview(current.Data,
                CardDescriptionBuilder.BuildForInstance(current), current.EffectiveApCost);
            if (guaranteedCard != null) guaranteedCard.BindPreview(current.Data,
                CardDescriptionBuilder.BuildGuaranteedUpgrade(current.Data, guaranteed), current.Data.ApCost);
            if (mutationChanceText != null) mutationChanceText.text = $"Mutation Chance: {mutationChance:P0}";
        }
    }
}
