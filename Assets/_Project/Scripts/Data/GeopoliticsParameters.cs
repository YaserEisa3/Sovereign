using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 13 and 15. How the world answers what you do: how far each diplomatic
    /// choice moves a relationship, how hard a trading partner hits back, and what it
    /// costs you when a creditor stops buying. Per-nation thresholds and personality
    /// live on each NationDefinition; the shared mechanics live here.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_GeopoliticsParameters", menuName = "Sovereign/Parameters/Geopolitics Parameters")]
    public class GeopoliticsParameters : ScriptableObject
    {
        [Header("Relationships")]
        [Tooltip("Share of the gap to its target a relationship closes each week. Diplomacy is slow.")]
        [Range(0.001f, 0.2f)] public float relationshipDrift = 0.02f;
        [Tooltip("Relationship lost per point of tariff you place on a nation.")]
        [Range(0f, 5f)] public float tariffRelationshipPenalty = 1.2f;
        [Tooltip("Relationship gained while a trade agreement is in force.")]
        [Range(0f, 60f)] public float agreementBonus = 15f;
        [Tooltip("Relationship lost while you sanction a nation.")]
        [Range(0f, 150f)] public float sanctionPenalty = 60f;
        [Tooltip("Relationship gained per $10B a year of aid directed to a nation.")]
        [Range(0f, 20f)] public float aidRelationshipPer10B = 3f;
        [Tooltip("Relationship gained while a currency swap line is open.")]
        [Range(0f, 30f)] public float swapLineBonus = 5f;
        [Tooltip("GDD 13: withdrawing from an agreement does PERMANENT damage - this much, for good.")]
        [Range(0f, 80f)] public float withdrawalScar = 25f;

        [Header("Trade")]
        [Tooltip("Trade volume index lost per point of tariff, weighted by that nation's share of your trade.")]
        [Range(0f, 5f)] public float tradeLossPerTariffPoint = 0.8f;
        [Tooltip("Trade volume index gained from an agreement, weighted by that nation's share.")]
        [Range(0f, 60f)] public float agreementTradeBonus = 12f;
        [Tooltip("Trade volume index lost from sanctions, weighted by that nation's share.")]
        [Range(0f, 150f)] public float sanctionTradeLoss = 60f;
        [Tooltip("Share of your tariff a nation matches when it retaliates.")]
        [Range(0f, 2f)] public float retaliationMatch = 1f;
        [Tooltip("Inflation points per point of the trade-weighted tariff you impose on imports.")]
        [Range(0f, 0.2f)] public float tariffInflation = 0.03f;

        [Header("Bonds - GDD 9.3")]
        [Tooltip("Auction demand effect, per unit of foreign issuance share, when creditors are buying.")]
        [Range(0f, 1f)] public float foreignDemandWhenBuying = 0.15f;
        [Tooltip("Auction demand effect, per unit of foreign issuance share, when creditors have stopped.")]
        [Range(-1f, 0f)] public float foreignDemandWhenStopped = -0.35f;

        [Header("Capital flight - GDD 15, Island Finance")]
        [Tooltip("Corporate tax rate above which capital starts leaving for the haven.")]
        [Range(0f, 60f)] public float capitalFlightThreshold = 25f;
        [Tooltip("Share of the corporate and capital gains tax base lost per point above the threshold.")]
        [Range(0f, 0.05f)] public float capitalFlightPerPoint = 0.012f;
        [Tooltip("The most of those bases that can leave.")]
        [Range(0f, 0.8f)] public float capitalFlightCap = 0.35f;

        [Header("AI moves")]
        [Tooltip("Current account surplus (% of GDP) at which Sino-Pacific accuses you of manipulation.")]
        [Range(0f, 10f)] public float manipulationSurplus = 3f;
        [Tooltip("Your currency index points lost when Sino-Pacific devalues - your exports lose ground.")]
        [Range(0f, 30f)] public float rivalDevaluation = 6f;
        [Tooltip("Manufacturing health lost when a rival devalues.")]
        [Range(0f, 30f)] public float devaluationManufacturingHit = 8f;

        [Header("Export subsidies - GDD 13")]
        [Tooltip("Net export growth points per $100B a year of export subsidy.")]
        [Range(0f, 3f)] public float exportSubsidyBoostPer100B = 1f;
        [Tooltip("Subsidy ($B a year) big enough for the export giant to call it a WTO violation.")]
        public float exportSubsidyWtoThreshold = 25f;

        [Header("Swap lines - GDD 13")]
        [Tooltip("Relationship a nation needs before it will open a swap line with you.")]
        [Range(-100f, 100f)] public float swapLineMinimumRelationship = 30f;
        [Tooltip("Emergency dollars each open swap line adds to your defensible reserves, in $B.")]
        public float swapLineCapacityBillions = 120f;
    }
}
