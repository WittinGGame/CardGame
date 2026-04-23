using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public static class CardDescriptionBuilder
    {
        public static string Build(CardData data)
        {
            if (data == null)
                return string.Empty;

            if (data.HasEffects)
            {
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

                        lines.Add(line);
                    }
                }

                if (lines.Count > 0)
                    return string.Join("\n", lines);
            }

            return BuildLegacy(data);
        }

        private static string BuildLegacy(CardData data)
        {
            if (data == null)
                return string.Empty;

            switch (data.CardType)
            {
                case CardType.Attack:
                    return $"Deal <color=#FF6B6B>{Mathf.Max(0, data.AttackDamage)} damage</color>";
                case CardType.Heal:
                    return $"Heal <color=#64D98B>{Mathf.Max(0, data.HealAmount)}</color>";
                case CardType.Buff:
                    return $"Gain <color=#FFD166>+{data.BuffPotency}</color>";
                case CardType.Defend:
                    return $"Gain <color=#6BCBFF>{Mathf.Max(0, data.BlockAmount)} Block</color>";
                default:
                    return string.Empty;
            }
        }
    }
}
