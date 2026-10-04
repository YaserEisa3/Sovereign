using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// The opening position, exposed on SimulationRunner so a scenario can be tuned
    /// in the Inspector rather than in code. The defaults are the GDD 18 dashboard's
    /// numbers: 112% debt, AA rating, inflation a little above target.
    /// </summary>
    [System.Serializable]
    public class StartingConditions
    {
        [Header("Output")]
        [Tooltip("Nominal GDP in billions per year at the start of the run.")]
        public float nominalGdpBillions = 27000f;

        [Tooltip("Trend real growth in percent - what the economy does when nothing is pushing it.")]
        [Range(-2f, 8f)] public float potentialGrowthRate = 2.1f;

        [Tooltip("Growth of the rest of the world, in percent. This is the demand behind your exports.")]
        [Range(-2f, 8f)] public float worldGrowthRate = 2.4f;

        [Header("Prices and labour")]
        [Range(0f, 20f)] public float unemployment = 4.8f;
        [Range(-5f, 30f)] public float inflation = 3.4f;

        [Header("Money and debt")]
        [Range(0f, 20f)] public float centralBankRate = 4f;

        [Tooltip("Sovereign debt as a share of GDP. 1.12 is the 112% on the GDD 18 dashboard.")]
        [Range(0f, 3f)] public float debtToGdp = 1.12f;

        [Tooltip("Weighted average coupon on the existing debt stock, in percent. Below current yields, because it was issued when money was cheaper.")]
        [Range(0f, 20f)] public float averageCoupon = 3.1f;

        [Tooltip("Share of the debt held abroad. 0.28 matches the nation assets' bond holdings.")]
        [Range(0f, 0.9f)] public float foreignHoldingShare = 0.28f;

        [Tooltip("Share of the debt that rolls over within a year. A bailout loan is long-dated; short-dated debt reprices onto today's yields fast.")]
        [Range(0.02f, 0.6f)] public float maturingWithinOneYear = 0.22f;
        [Tooltip("Share maturing in one to five years. The rest is longer than five.")]
        [Range(0.05f, 0.8f)] public float maturingOneToFive = 0.43f;

        [Tooltip("FX reserves in billions - what you have to defend the currency with.")]
        public float fxReservesBillions = 410f;

        [Header("Events")]
        [Tooltip("Seed for the event dice. The same seed and the same decisions replay the same history. 0 picks a new seed every run.")]
        public int randomSeed = 0;

        [Header("Budget")]
        [Tooltip("Revenue at the opening position as a share of GDP. Each tax line is estimated from its own rate and base, then all of them are scaled once so the total lands here - the relative weights stay honest, the macro calibration holds. 0.18 with the default spending gives a deficit of about 4% of GDP.")]
        [Range(0.05f, 0.5f)] public float openingRevenueShareOfGdp = 0.18f;

        [Tooltip("Every spending line is scaled by this at boot. The assets are sized for the GDD 18 economy; a smaller, poorer country cannot fund that budget. 1 leaves the authored amounts alone.")]
        [Range(0.2f, 2f)] public float spendingScale = 1f;

        [Header("Aftermath - an opening that is not business as usual")]
        [Tooltip("How far output starts BELOW potential, in percent. A war leaves idle factories and empty offices; 0 is an economy running at its capacity.")]
        [Range(-40f, 10f)] public float outputGapPercent = 0f;

        [Tooltip("Percentage points taken off potential growth for good - skills lost, people gone, plant destroyed. GDD 17.4 calls this a scar.")]
        [Range(0f, 3f)] public float productivityScar = 0f;

        [Tooltip("Infrastructure health, 0-100, at the start. Below 0 uses the value on SO_InfrastructureParameters.")]
        [Range(-1f, 100f)] public float infrastructureHealth = -1f;

        [Tooltip("A nation that starts hostile - the one you just fought. Empty for none.")]
        public string hostileNation = "";
        [Range(-100f, 100f)] public float hostileRelationship = -60f;
        [Tooltip("The hostile nation starts sanctioning you and refusing your bonds.")]
        public bool hostileSanctions = false;

        [Header("Victory - what this scenario counts as winning")]
        [Tooltip("Years every condition below must hold together. 0 means this scenario has no victory and runs open-ended.")]
        [Range(0f, 10f)] public float victoryYears = 0f;
        public string victoryTitle = "THE COUNTRY IS WHOLE AGAIN";
        [TextArea] public string victorySummary = "";
        [Tooltip("Debt as a share of GDP, at or below.")]
        [Range(0f, 3f)] public float victoryDebtToGdp = 0.6f;
        [Tooltip("Unemployment percent, at or below.")]
        [Range(0f, 25f)] public float victoryUnemployment = 5.5f;
        [Tooltip("Infrastructure out of 100, at or above.")]
        [Range(0f, 100f)] public float victoryInfrastructure = 75f;
        [Tooltip("Real wage index against the opening, at or above.")]
        [Range(50f, 200f)] public float victoryRealWage = 110f;
        [Tooltip("Overall approval percent, at or above.")]
        [Range(0f, 100f)] public float victoryApproval = 50f;

        [Header("Briefing")]
        [Tooltip("Posted to the ticker the moment the run starts - what just happened to this country.")]
        [TextArea] public string[] briefing = new string[0];
    }
}
