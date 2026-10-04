using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 18. The meter view: every indicator as a bar, on one screen. A bar needs
    /// to know where its scale starts and ends and what counts as a good reading, and
    /// none of that belongs in code - it is authored here, one entry per bar, in the
    /// order they are drawn.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_MeterParameters", menuName = "Sovereign/Parameters/Meter Parameters")]
    public class MeterParameters : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("The recorded series this bar reads, e.g. gdpGrowth or debtToGdp.")]
            public string series = "";
            public string label = "";
            [Tooltip("What this indicator actually tells the player, in plain language. Shown when they hover the bar.")]
            [TextArea(2, 4)] public string explanation = "";
            [Tooltip("Printed after the number: %, M, $B, or nothing for an index.")]
            public string unit = "%";

            [Tooltip("Value at the left end of the bar.")]
            public float min = 0f;
            [Tooltip("Value at the right end of the bar.")]
            public float max = 100f;

            [Tooltip("The reading you are aiming at - drawn as a tick on the track.")]
            public float target = 0f;
            [Tooltip("Distance from target that still counts as healthy. Beyond twice this, the bar turns red.")]
            public float tolerance = 1f;
            [Tooltip("On means more is better (approval); off means less is better (unemployment). For a target like inflation, leave off and let the tolerance do the work.")]
            public bool higherIsBetter = false;
            [Tooltip("On for an indicator judged by distance from its target in EITHER direction, like inflation or the currency.")]
            public bool judgeByDistance = true;
            public int decimals = 1;
        }

        public Entry[] entries = new Entry[0];
    }
}
