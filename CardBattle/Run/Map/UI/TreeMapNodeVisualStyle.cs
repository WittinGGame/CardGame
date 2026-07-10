using System;
using UnityEngine;

namespace CardBattle.Core
{
    [Serializable]
    public class TreeMapNodeVisualStyle
    {
        [Range(0f, 1f)] public float bgAlpha = 1f;
        [Range(0f, 1f)] public float ringAlpha = 1f;
        [Range(0f, 1f)] public float iconAlpha = 1f;
        public bool glowEnabled;
        public bool completedXEnabled;
        public bool currentMarkerEnabled;

        public static TreeMapNodeVisualStyle CreateLockedDefault()
        {
            return new TreeMapNodeVisualStyle
            {
                bgAlpha = 0.35f,
                ringAlpha = 0.25f,
                iconAlpha = 0.50f,
                glowEnabled = false,
                completedXEnabled = false,
                currentMarkerEnabled = false
            };
        }

        public static TreeMapNodeVisualStyle CreateAvailableDefault()
        {
            return new TreeMapNodeVisualStyle
            {
                bgAlpha = 1.00f,
                ringAlpha = 0.90f,
                iconAlpha = 1.00f,
                glowEnabled = true,
                completedXEnabled = false,
                currentMarkerEnabled = false
            };
        }

        public static TreeMapNodeVisualStyle CreateCurrentDefault()
        {
            return new TreeMapNodeVisualStyle
            {
                bgAlpha = 1.00f,
                ringAlpha = 1.00f,
                iconAlpha = 1.00f,
                glowEnabled = true,
                completedXEnabled = false,
                currentMarkerEnabled = true
            };
        }

        public static TreeMapNodeVisualStyle CreateCompletedDefault()
        {
            return new TreeMapNodeVisualStyle
            {
                bgAlpha = 0.55f,
                ringAlpha = 0.45f,
                iconAlpha = 0.65f,
                glowEnabled = false,
                completedXEnabled = true,
                currentMarkerEnabled = false
            };
        }
    }
}
