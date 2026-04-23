using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Designer-facing base class for effect-driven card behavior.
    /// </summary>
    public abstract class CardEffectData : ScriptableObject
    {
        public abstract void Apply(CardPlayContext context, CardEffectExecutionContext executionContext);
    }
}
