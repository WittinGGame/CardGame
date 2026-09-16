using System;
using System.Collections.Generic;

namespace CardBattle.Core
{
    /// <summary>
    /// Runtime card in a pile (deck / hand / graveyard). Holds a reference to static <see cref="CardData"/>
    /// plus optional modifiers for upgrades and temporary effects.
    /// </summary>
    public class CardInstance
    {
        public CardData Data { get; }
        public Guid InstanceId { get; }
        public string RunCardInstanceId { get; }
        private readonly IReadOnlyList<CardEffectData> effectiveEffects;
        public IReadOnlyList<CardEffectData> EffectiveEffects => effectiveEffects ?? Data.Effects;

        private readonly List<ICardModifier> _modifiers = new List<ICardModifier>();

        public IReadOnlyList<ICardModifier> Modifiers => _modifiers;

        public CardInstance(CardData data, Guid? instanceId = null,
            IReadOnlyList<CardEffectData> effectiveEffects = null, string runCardInstanceId = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            InstanceId = instanceId ?? Guid.NewGuid();
            RunCardInstanceId = runCardInstanceId;
            if (effectiveEffects != null)
            {
                var copy = new List<CardEffectData>(effectiveEffects.Count);
                for (int i = 0; i < effectiveEffects.Count; i++) copy.Add(effectiveEffects[i]);
                this.effectiveEffects = copy.AsReadOnly();
            }
        }

        public void AddModifier(ICardModifier modifier)
        {
            if (modifier != null)
                _modifiers.Add(modifier);
        }

        public void ClearModifiers() => _modifiers.Clear();
    }
}
