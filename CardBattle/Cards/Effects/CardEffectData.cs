using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Designer-facing base class for effect-driven card behavior.
    /// </summary>
    public abstract class CardEffectData : ScriptableObject
    {
        /// <summary>
        /// Execution category used by the sequential effect runner.
        /// Immediate effects apply synchronously; draw/selection yield until complete.
        /// </summary>
        public virtual CardEffectExecutionKind ExecutionKind => CardEffectExecutionKind.Immediate;

        public abstract string GetDescriptionText();
        public abstract void Apply(CardPlayContext context, CardEffectExecutionContext executionContext);
    }
}
