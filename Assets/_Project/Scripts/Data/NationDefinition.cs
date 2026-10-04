using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 15. One asset per nation. Map position is NOT here - that lives on the
    /// Nation_* GameObject's transform in the scene, placed by hand (GDD 3.3).
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Nation_", menuName = "Sovereign/Nation Definition")]
    public class NationDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Nation";
        public NationArchetype archetype = NationArchetype.LargeDiversified;
        [TextArea(2, 4)] public string briefing = "";
        public Color nationColor = Color.grey;
        [Tooltip("Tick this on the player's own nation. Exactly one asset should have it.")]
        public bool isPlayerNation = false;

        [Header("Starting position")]
        [Tooltip("Relationship with the player, -100 hostile to +100 allied.")]
        [Range(-100f, 100f)] public float startingRelationship = 20f;

        [Tooltip("Share of the player's sovereign debt this nation holds, 0-1. The bond weapon of GDD 9.3.")]
        [Range(0f, 1f)] public float bondHoldingPercent = 0.05f;

        [Tooltip("Share of the player's total trade running through this nation, 0-1. Drives trade contagion exposure.")]
        [Range(0f, 1f)] public float tradeVolumePercent = 0.1f;

        [Tooltip("Cross-border bank exposure, 0-1. Drives financial contagion exposure.")]
        [Range(0f, 1f)] public float financialLinkage = 0.1f;

        [Header("Economy")]
        [Tooltip("This nation's trend growth rate in percent.")]
        [Range(-5f, 15f)] public float trendGrowthRate = 2f;

        [Tooltip("Chance per year this nation falls into recession, 0-1. Feeds ForeignRecession events.")]
        [Range(0f, 1f)] public float recessionProneness = 0.15f;

        [Header("AI behaviour thresholds - GDD 15")]
        [Tooltip("Player tariff rate in percent above which this nation retaliates.")]
        [Range(0f, 50f)] public float tariffRetaliationThreshold = 20f;

        [Tooltip("Relationship below which this nation quietly stops buying your bonds at auction.")]
        [Range(-100f, 0f)] public float quietBondReductionRelationship = -40f;

        [Tooltip("Relationship below which this nation actively dumps your bonds. The crisis trigger.")]
        [Range(-100f, 0f)] public float activeBondDumpingRelationship = -70f;

        [Tooltip("Relationship below which this nation joins a sanctions coalition against you.")]
        [Range(-100f, 0f)] public float sanctionsRelationship = -55f;

        [Tooltip("Years between this nation's signature move - Sino-Pacific devaluation, Petro-Gulf supply cut, Emerging South restructuring. 0 means it has no periodic move.")]
        [Range(0f, 15f)] public float signatureMoveIntervalYears = 0f;

        [Tooltip("How aggressively this nation escalates once provoked, 0-1.")]
        [Range(0f, 1f)] public float aggression = 0.4f;
    }
}
