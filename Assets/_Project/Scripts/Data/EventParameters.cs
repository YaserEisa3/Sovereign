using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 17. How events hit and how much your preparation softens them. GDD 17's
    /// central rule is that spending levels AT THE MOMENT a crisis strikes decide the
    /// outcome - these are the numbers that turn "spending at the moment" into damage.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_EventParameters", menuName = "Sovereign/Parameters/Event Parameters")]
    public class EventParameters : ScriptableObject
    {
        [Header("Severity")]
        [Tooltip("Each event rolls a severity between these at spawn. 1 is the event as authored.")]
        [Range(0.1f, 2f)] public float severityMin = 0.7f;
        [Range(0.1f, 3f)] public float severityMax = 1.3f;

        [Header("Mitigation - fixed at the moment an event strikes")]
        [Tooltip("No preparation softens more than this.")]
        [Range(0f, 1f)] public float mitigationCap = 0.9f;
        [Tooltip("Disaster Relief Fund level ($B/yr) that absorbs half a disaster. GDD 17.5.")]
        public float disasterFundHalfEffect = 60f;
        [Tooltip("Pandemic mitigation per unit of Public Health spending against its opening level. GDD 17.4.")]
        [Range(0f, 1f)] public float pandemicPublicHealthWeight = 0.3f;
        [Tooltip("Pandemic mitigation per unit of Medicaid+Medicare+Public Health against opening.")]
        [Range(0f, 1f)] public float pandemicHealthWeight = 0.2f;
        [Tooltip("War mitigation per unit of defence spending against its opening level.")]
        [Range(0f, 1f)] public float warDefenceWeight = 0.45f;
        [Tooltip("Bank capital requirement (percent) below which a financial crisis is unmitigated.")]
        public float financialCapitalFloor = 6f;
        [Tooltip("Points of capital requirement above the floor that reach full mitigation.")]
        public float financialCapitalRange = 10f;

        [Header("Pandemic - GDD 17.4")]
        [Tooltip("Strike with mitigation below this and the pandemic leaves a PERMANENT productivity scar.")]
        [Range(0f, 1f)] public float pandemicScarThreshold = 0.4f;
        [Tooltip("Percentage points taken off potential growth, for good, per unit of severity.")]
        [Range(0f, 1f)] public float pandemicScarSize = 0.15f;

        [Header("War - GDD 17.3")]
        [Tooltip("An attacker's strength as a multiple of your OPENING defence budget, before severity.")]
        [Range(0.2f, 3f)] public float enemyStrengthMultiple = 1.1f;
        [Tooltip("Average spending gap (-1 to 1, positive = outspent) at which losing becomes occupation and ends the run.")]
        [Range(0f, 1f)] public float occupationGapThreshold = 0.5f;
        [Tooltip("Weeks between war reports on the ticker.")]
        [Range(1, 13)] public int warReportEveryWeeks = 4;

        [Header("Sovereign default - GDD 17.6")]
        [Tooltip("Debt/GDP above which a debt crisis can end in default.")]
        [Range(1f, 4f)] public float defaultDebtThreshold = 2.2f;
        [Tooltip("Auction cover below which an auction counts as failed.")]
        [Range(0f, 1f)] public float defaultAuctionCover = 0.6f;
        [Tooltip("Consecutive failed auctions during a debt crisis before the treasury misses a payment.")]
        [Range(1, 8)] public int defaultFailedAuctions = 2;

        [Header("World prices")]
        [Tooltip("Share of the gap to normal the oil price closes each week once a shock passes.")]
        [Range(0f, 0.5f)] public float oilReversion = 0.02f;
        [Tooltip("Share of the gap to normal trade volume closes each week.")]
        [Range(0f, 0.5f)] public float tradeReversion = 0.03f;
        [Tooltip("Growth points from net exports per index point of trade volume away from normal.")]
        [Range(0f, 0.2f)] public float tradeNetExportWeight = 0.03f;

        [Header("News - GDD 20 Phase 5")]
        [Tooltip("Weeks between generated headlines on the ticker.")]
        [Range(1, 13)] public int headlineIntervalWeeks = 3;
        [Tooltip("How many recent topics are rested before they can come up again.")]
        [Range(0, 20)] public int headlineTopicRest = 4;

        [Header("Contagion - GDD 17.7")]
        [Tooltip("Market confidence above which a foreign crisis makes you a safe haven rather than a casualty.")]
        [Range(0f, 100f)] public float safeHavenCredibility = 60f;
        [Tooltip("Currency index points per week, up for a safe haven, down for capital flight.")]
        [Range(0f, 2f)] public float safeHavenWeeklyCurrency = 0.05f;
        [Tooltip("Share of foreign holdings sold per week of a bond dumping attack.")]
        [Range(0f, 0.05f)] public float bondDumpingHoldingLoss = 0.002f;
        [Tooltip("Millions of working-age arrivals per week of a refugee event, at severity 1.")]
        [Range(0f, 0.2f)] public float refugeeWorkingAgeWeekly = 0.02f;
    }
}
