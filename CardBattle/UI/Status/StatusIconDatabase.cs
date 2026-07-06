using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "StatusIconDatabase",
        menuName = "Card Battle/UI/Status Icon Database",
        order = 50)]
    public class StatusIconDatabase : ScriptableObject
    {
        [Serializable]
        public class StatusIconEntry
        {
            public StatusEffectType statusType;
            public Sprite icon;
            public string displayName;
            public bool isDebuff;
        }

        [SerializeField] private List<StatusIconEntry> entries = new();

        [Header("Direction Icons")]
        [SerializeField] private Sprite buffArrowIcon;
        [SerializeField] private Sprite debuffArrowIcon;

        public Sprite BuffArrowIcon => buffArrowIcon;
        public Sprite DebuffArrowIcon => debuffArrowIcon;

        public bool TryGet(StatusEffectType type, out StatusIconEntry entry)
        {
            entry = null;

            if (entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                StatusIconEntry candidate = entries[i];
                if (candidate == null || candidate.statusType != type)
                    continue;

                entry = candidate;
                return true;
            }

            return false;
        }
    }
}
