using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 17.3. War outcomes are driven by the spending gap, not by a dice roll.
    /// Outspend the enemy and the war is short and cheap; underspend and the bill
    /// arrives as casualties, infrastructure damage and, years later, veterans.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_WarParameters", menuName = "Sovereign/Parameters/War Parameters")]
    public class WarParameters : ScriptableObject
    {
        [Header("Casualties")]
        [Tooltip("X = enemy strength minus your military budget, normalised -1 to 1 (negative means you outspend them). Y = weekly KIA.")]
        public AnimationCurve casualtyCurve = AnimationCurve.Linear(-1f, 20f, 1f, 1200f);

        [Tooltip("Weekly percentage of the equipment stock lost while a war is active.")]
        [Range(0f, 10f)] public float equipmentDegradationPercentPerWeek = 0.4f;

        [Tooltip("Weekly reduction in infrastructure health while a war is active, in points.")]
        [Range(0f, 5f)] public float infrastructureDamagePerWeek = 0.15f;

        [Tooltip("Share of surviving personnel rotating out to veteran status each year, 0-1. GDD 22.4 - the visible counter that explains the benefits bill years later.")]
        [Range(0f, 1f)] public float veteranConversionRate = 0.12f;

        [Header("War economy")]
        [Tooltip("Forced minimum defence spending while at war, in $B/yr. The player cannot go below this.")]
        public float wartimeMinimumDefenceBillions = 900f;

        [Tooltip("Boost to manufacturing sector health while at war - the war economy of GDD 17.3.")]
        [Range(0f, 50f)] public float defenceManufacturingBoost = 12f;

        [Tooltip("Business investment chill while a war is active, in percent.")]
        [Range(0f, 50f)] public float businessInvestmentChill = 8f;

        [Header("Approval - GDD 14")]
        [Tooltip("Rally-round-the-flag bonus the week war breaks out.")]
        public float rallyApprovalBonus = 5f;

        [Tooltip("Approval lost per week for the first stretch of the war.")]
        public float weeklyApprovalDrain = 0.3f;

        [Tooltip("Approval lost per week once the war passes the threshold below.")]
        public float lateWarApprovalDrain = 0.8f;

        [Tooltip("Weeks before the drain accelerates. GDD 14 says 18 months.")]
        [Range(26, 260)] public int lateWarThresholdWeeks = 78;

        [Tooltip("Approval swing on a clear victory.")]
        public float victoryApprovalBonus = 12f;

        [Tooltip("Approval swing on defeat or an unresolved stalemate.")]
        public float defeatApprovalPenalty = -25f;

        [Header("Reconstruction")]
        [Tooltip("Cost in $B to repair one point of war-damaged infrastructure. Lumpy, and on top of ordinary maintenance.")]
        public float reconstructionCostPerPoint = 18f;
    }
}
