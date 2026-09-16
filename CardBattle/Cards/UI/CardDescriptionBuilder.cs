using System.Collections.Generic;

namespace CardBattle.Core
{
    public static class CardDescriptionBuilder
    {
        public static string Build(CardData data)
        {
            if (data == null)
                return string.Empty;

            return BuildEffects(data, data.Effects);
        }

        public static string BuildForInstance(CardInstance card)
        {
            return card?.Data == null ? string.Empty : BuildEffects(card.Data, card.EffectiveEffects);
        }

        public static string BuildGuaranteedUpgrade(CardData data, CardUpgradeDefinition upgrade)
        {
            return data == null || upgrade == null || upgrade.BaseCard != data || !upgrade.HasValidSequence
                ? string.Empty : BuildEffects(data, upgrade.GuaranteedEffects);
        }

        private static string BuildEffects(CardData data, IReadOnlyList<CardEffectData> effects)
        {
            var lines = new List<string>();

            if (effects != null)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];
                    if (effect == null)
                        continue;

                    string line = effect.GetDescriptionText();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    lines.Add(line.TrimEnd());
                }
            }

            if (data.Retain)
                lines.Add("Retain.");

            if (data.Temporary)
                lines.Add("Temporary.");

            if (data.ExhaustAfterPlay)
                lines.Add("Exhaust.");

            if (lines.Count == 0)
                return string.Empty;

            return string.Join("\n", lines);
        }
    }
}
