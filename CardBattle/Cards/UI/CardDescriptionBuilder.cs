using System.Collections.Generic;

namespace CardBattle.Core
{
    public static class CardDescriptionBuilder
    {
        public static string Build(CardData data)
        {
            if (data == null)
                return string.Empty;

            var lines = new List<string>();
            var effects = data.Effects;

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
