using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 18 / 3.7. Bloomberg terminal meets classified war room. Every colour and
    /// font in the game comes from here - nothing is set inline in a controller.
    /// The .uss files hold the same values as CSS variables; this asset is what the
    /// charts and map read, since they cannot use USS.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_UITheme", menuName = "Sovereign/Parameters/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Surfaces")]
        public Color background = new Color32(0x0a, 0x0e, 0x14, 0xff);
        public Color panelBackground = new Color32(0x11, 0x16, 0x1f, 0xff);
        public Color panelBorder = new Color32(0x1e, 0x27, 0x35, 0xff);
        public Color gridLine = new Color32(0x1a, 0x22, 0x2e, 0xff);

        [Header("Text")]
        public Color primaryText = new Color32(0xe8, 0xe8, 0xe8, 0xff);
        public Color secondaryText = new Color32(0x8b, 0x97, 0xa8, 0xff);
        public Color mutedText = new Color32(0x55, 0x60, 0x70, 0xff);

        [Header("Data - GDD 18")]
        public Color growth = new Color32(0x00, 0xd0, 0x84, 0xff);
        public Color warning = new Color32(0xf5, 0xa6, 0x23, 0xff);
        public Color danger = new Color32(0xff, 0x44, 0x44, 0xff);
        public Color neutralData = new Color32(0x7a, 0x9c, 0xc5, 0xff);

        [Header("Map")]
        public Color mapOcean = new Color32(0x08, 0x0c, 0x12, 0xff);
        public Color homeNation = new Color32(0x7a, 0x9c, 0xc5, 0xff);
        public Color allyNation = new Color32(0x00, 0xd0, 0x84, 0xff);
        public Color neutralNation = new Color32(0x55, 0x60, 0x70, 0xff);
        public Color hostileNation = new Color32(0xff, 0x44, 0x44, 0xff);
        public Color tradeRoute = new Color32(0x2c, 0x3e, 0x52, 0xff);

        [Header("Fonts")]
        [Tooltip("Roboto Mono or similar - every number in the game. Drop the font asset here once it is imported.")]
        public Font monoFont;
        [Tooltip("Inter or similar - labels and body text.")]
        public Font bodyFont;

        [Header("Spacing tokens (px)")]
        public int spacingTight = 4;
        public int spacingBase = 8;
        public int spacingLoose = 16;
        public int panelRadius = 2;

        [Header("Type scale (px)")]
        public int fontSizeKpi = 28;
        public int fontSizeHeading = 14;
        public int fontSizeBody = 12;
        public int fontSizeCaption = 10;

        /// <summary>Growth / warning / danger for a value against a threshold, so every
        /// panel colours numbers the same way without repeating the rule.</summary>
        public Color ValueColor(float value, float warnAbove, float dangerAbove)
        {
            if (value >= dangerAbove) return danger;
            if (value >= warnAbove) return warning;
            return growth;
        }
    }
}
