using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 3.5 / Phase 1. The economy's behavioural constants. Nothing in
    /// SimulationCore may hardcode these - they are copied into plain structs at
    /// boot, and every one of them is tunable here.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_MacroParameters", menuName = "Sovereign/Parameters/Macro Parameters")]
    public class MacroParameters : ScriptableObject
    {
        [Header("Core relationships")]
        [Tooltip("Okun's law. Points of unemployment per point of GDP growth shortfall against trend. 0.5 = a 2pt growth miss costs 1pt of employment.")]
        [Range(0f, 2f)] public float okunCoefficient = 0.5f;

        [Tooltip("Phillips curve. Points of inflation per point of unemployment below the natural rate. A low value is a flat curve: inflation barely responds to a hot labour market.")]
        [Range(0f, 2f)] public float phillipsCoefficient = 0.3f;

        [Tooltip("How much a 1% expansion of M2 feeds through into prices, before credibility effects.")]
        [Range(0f, 3f)] public float monetaryMultiplier = 0.8f;

        [Tooltip("GDP response to a 1pt move in the base rate, at full transmission.")]
        [Range(0f, 2f)] public float interestRateSensitivity = 0.4f;

        [Header("Currency and external")]
        [Tooltip("FDI inflow response to currency depreciation. Feeds the manufacturing competitiveness boost (GDD 6).")]
        [Range(0f, 2f)] public float fdiCurrencySensitivity = 0.6f;

        [Tooltip("Contagion channel 1: how hard a partner's recession hits you through trade exposure.")]
        [Range(0f, 2f)] public float tradeContagionMultiplier = 0.5f;

        [Tooltip("Contagion channel 2: how hard a partner's financial stress hits you through bank linkage.")]
        [Range(0f, 2f)] public float financialContagionMultiplier = 0.7f;

        [Header("Debt")]
        [Tooltip("X = debt/GDP ratio (1.2 is 120%). Y = extra yield in percentage points the market demands. Rises sharply above 1.2 - GDD 21, debt dynamics are non-linear.")]
        public AnimationCurve debtRiskPremiumCurve = AnimationCurve.Linear(0f, 0f, 2f, 8f);

        [Header("Transmission lags (quarters) - GDD 12")]
        [Tooltip("Quarters before a rate change reaches full effect on output. First effects show after one.")]
        [Range(1, 8)] public int monetaryLagQuarters = 4;

        [Tooltip("Quarters before QE's inflation effect lands. Its yield effect is immediate.")]
        [Range(1, 8)] public int qeInflationLagQuarters = 3;

        [Header("Wages and expectations - GDD 22.5, floating wages")]
        [Tooltip("Unemployment rate at which wage pressure is neutral (the natural rate).")]
        [Range(0f, 15f)] public float naturalUnemploymentRate = 4.5f;

        [Tooltip("Share of recent inflation that feeds into next quarter's wage demands while expectations are anchored.")]
        [Range(0f, 1f)] public float baseInflationPassThrough = 0.45f;

        [Tooltip("Pass-through once expectations unanchor. The Volcker problem exists only because this number is close to 1.")]
        [Range(0f, 1.5f)] public float unanchoredInflationPassThrough = 0.95f;

        [Tooltip("Inflation rate which, sustained for the window below, unanchors expectations.")]
        [Range(0f, 30f)] public float expectationsUnanchorThreshold = 6f;

        [Tooltip("Years above that threshold before expectations unanchor. GDD 12 says 2.")]
        [Range(1, 6)] public int expectationsUnanchorYears = 2;

        [Tooltip("How much of the gap to target wages closes each quarter. Lower is stickier.")]
        [Range(0.01f, 1f)] public float wageStickiness = 0.25f;

        [Tooltip("Hard cap on how far an average wage can move in one quarter, in percent. Stops the spiral going vertical in a single tick.")]
        [Range(0.5f, 10f)] public float maxQuarterlyWageDriftPercent = 2f;

        [Header("Central bank credibility - GDD 9.4")]
        [Tooltip("Starting credibility, 0-100. Shown to the player only as Market Confidence.")]
        [Range(0f, 100f)] public float startingCredibility = 75f;

        [Tooltip("Credibility lost per quarter of QE run while core inflation is above target.")]
        [Range(0f, 20f)] public float credibilityLossPerQeAtHighInflation = 4f;

        [Tooltip("Credibility gained per year of hitting the inflation target.")]
        [Range(0f, 20f)] public float credibilityGainPerYearOnTarget = 3f;

        [Tooltip("The inflation target the central bank is judged against, in percent.")]
        [Range(0f, 10f)] public float inflationTarget = 2f;

        [Header("Demand composition (shares of GDP, should sum to 1)")]
        [Range(0f, 1f)] public float consumptionShare = 0.68f;
        [Range(0f, 1f)] public float investmentShare = 0.18f;
        [Range(0f, 1f)] public float governmentShare = 0.17f;
        [Tooltip("Net exports. Negative for a trade deficit - the usual starting position.")]
        [Range(-0.3f, 0.3f)] public float netExportShare = -0.03f;

        [Header("Demand responses")]
        [Tooltip("Growth points lost per 10 points of income and consumption tax. The consumption channel.")]
        [Range(0f, 3f)] public float consumptionTaxSensitivity = 0.35f;
        [Tooltip("Investment growth points lost per 10 points of corporate tax.")]
        [Range(0f, 3f)] public float investmentCorporateTaxSensitivity = 0.30f;
        [Tooltip("How fast confidence moves toward what conditions justify, per week.")]
        [Range(0.01f, 0.5f)] public float confidenceAdjustmentSpeed = 0.08f;
        [Tooltip("Export response to a 10% currency depreciation, in growth points.")]
        [Range(0f, 3f)] public float exportCurrencySensitivity = 0.25f;
        [Tooltip("Trade volume lost per 10 points of tariff.")]
        [Range(0f, 3f)] public float tariffTradeSensitivity = 0.20f;
        [Tooltip("Consumer price response to a 10% currency depreciation - imported inflation.")]
        [Range(0f, 3f)] public float importedInflationSensitivity = 0.45f;

        [Header("Stabilisers - what stops a spiral running away")]
        [Tooltip("How fast unemployment is pulled back toward the natural rate, per week. Unemployment INTEGRATES the growth gap, so without this a long recession has no floor.")]
        [Range(0.001f, 0.05f)] public float unemploymentReversionSpeed = 0.005f;
        [Tooltip("The gap above the natural rate at which self-healing runs at FULL speed. Below it the pull weakens with the gap, so a country deep in unemployment still recovers on its own while the last points above the natural rate have to be earned by growing.")]
        [Range(1f, 20f)] public float reversionReferenceGap = 9f;

        [Tooltip("Points off the natural rate for doubling what the country spends educating and training people. The floor is not a law of nature - it is how well matched people are to the work there is - and this is the only lever the player has on it.")]
        [Range(0f, 4f)] public float trainingNaturalRateEffect = 1.5f;
        [Tooltip("The most the natural rate can be moved either way by schooling, so it can never be driven to zero.")]
        [Range(0f, 4f)] public float naturalRateRange = 2f;
        [Tooltip("How fast the natural rate moves toward what the schooling budget earns, per week. 0.006 is about three years - an investment a government makes for its successor.")]
        [Range(0.0005f, 0.05f)] public float naturalRateAdjustmentSpeed = 0.006f;

        [Tooltip("Growth points added per point of output below potential. Spare factories and idle workers are cheap to put back to work, which is why post-war recoveries run hot. 0 makes a hole permanent.")]
        [Range(0f, 0.5f)] public float recoveryFromSlack = 0.03f;

        [Tooltip("Growth points per point of GDP the state grows or shrinks by. Around 0.8 is the textbook multiplier: cut spending by ten points of GDP and output falls by eight while it happens.")]
        [Range(0f, 2f)] public float fiscalMultiplier = 0.8f;
        [Tooltip("How fast the economy gets used to a new size of state, per week. 0.004 is about three years.")]
        [Range(0.0005f, 0.05f)] public float fiscalAdjustmentSpeed = 0.02f;

        [Tooltip("How fast output moves toward where the model says it is heading, per week. 0.06 is about four months to half-close the gap, which reads as a dead control; higher makes a decision show up in the figure sooner.")]
        [Range(0.01f, 0.4f)] public float growthAdjustmentSpeed = 0.10f;

        [Tooltip("How much faster a tax base runs away as the rate climbs above the going rate. 0 is linear - and linear lets a government tax every line to its ceiling and clear the national debt in five years.")]
        [Range(0f, 6f)] public float avoidanceCurvature = 2.5f;

        [Header("How the market reads your direction")]
        [Tooltip("Premium points per point of debt/GDP added or removed over the last year. The market charges for the direction of travel, not only the level.")]
        [Range(0f, 0.5f)] public float debtTrendPremium = 0.12f;
        [Tooltip("Most the premium can fall for a debt path that is improving.")]
        [Range(0f, 6f)] public float debtTrendCredit = 3f;
        [Tooltip("Most the premium can rise for one that is not.")]
        [Range(0f, 10f)] public float debtTrendPenalty = 5f;

        [Tooltip("Points of real interest rate above neutral that shut catch-up growth off completely. Spare capacity is only cheap to use while money is available; a central bank holding rates high is deliberately stopping the recovery.")]
        [Range(1f, 20f)] public float slackRecoveryChokeRate = 5f;

        [Header("Bond buybacks - GDD 9")]
        [Tooltip("Years to maturity assumed for each bucket of the debt profile. A longer book moves further in price on the same yield gap.")]
        public float buybackShortYears = 0.5f;
        public float buybackMediumYears = 3.5f;
        public float buybackLongYears = 12f;
        [Tooltip("Cents on the dollar the price rises per $100B bought in one quarter - you are the buyer, and a big order lifts the bid against you.")]
        [Range(0f, 0.2f)] public float buybackExecutionPremiumPer100B = 0.015f;
        [Tooltip("Floor and ceiling on the price of your own debt.")]
        [Range(0.1f, 1f)] public float buybackMinPrice = 0.35f;
        [Range(1f, 2f)] public float buybackMaxPrice = 1.3f;
        [Tooltip("Share of the bonds you buy back that come out of foreign hands.")]
        [Range(0f, 1f)] public float buybackForeignSellerShare = 0.5f;
        [Tooltip("Most of the debt stock one quarter's operation can retire.")]
        [Range(0.01f, 0.5f)] public float buybackMaxShareOfStock = 0.2f;

        [Tooltip("Pull of purchasing power parity on the currency, per week. Without it any persistent pressure drifts the currency to infinity.")]
        [Range(0f, 2f)] public float currencyMeanReversion = 0.15f;

        [Tooltip("How much of a wage CUT actually happens. Wages are rigid downward - this is what stops deflation spiralling.")]
        [Range(0f, 1f)] public float downwardWageRigidity = 0.25f;

        [Tooltip("Floor under inflation expectations. Nobody plans for permanent deflation.")]
        [Range(-5f, 0f)] public float expectationsFloor = -1f;

        [Tooltip("How much of the Phillips effect survives when there is SLACK rather than shortage. Real curves are flat on the downside: a hot labour market raises prices far faster than a cold one lowers them.")]
        [Range(0f, 1f)] public float phillipsSlackDamping = 0.35f;

        [Header("Markets")]
        [Tooltip("The neutral real policy rate. Above it money is tight, below it loose.")]
        [Range(-2f, 5f)] public float neutralRealRate = 1f;
        [Tooltip("Extra yield the market wants for lending long rather than short.")]
        [Range(0f, 4f)] public float termPremium = 0.8f;
        [Tooltip("Percentage points yields fall per $100B of QE, at full credibility.")]
        [Range(0f, 1f)] public float qeYieldEffectPer100B = 0.06f;
        [Tooltip("How hard weak auction demand pushes yields up.")]
        [Range(0f, 3f)] public float auctionDemandSensitivity = 0.5f;
        [Tooltip("Bid-to-cover the market considers comfortable. Below it the risk premium creeps up CONTINUOUSLY - a creditor walking away shows as yields drifting up long before an auction fails outright (GDD 9.3).")]
        [Range(1f, 3f)] public float comfortableAuctionCover = 1.4f;

        [Header("Currency - the GDD 10 terms, in order")]
        [Range(0f, 3f)] public float currencyRateDifferentialWeight = 0.6f;
        [Range(0f, 3f)] public float currencyCurrentAccountWeight = 0.4f;
        [Range(0f, 3f)] public float currencyInflationWeight = 0.5f;
        [Range(0f, 3f)] public float currencyQeWeight = 0.3f;
        [Tooltip("Index points moved per $100B of intervention. Reserves buy less when confidence is low.")]
        [Range(0f, 5f)] public float interventionEffectPer100B = 1.2f;

        [Header("Tax bases - what each tax is levied on, as a share of nominal GDP")]
        [Range(0f, 1f)] public float taxableIncomeShare = 0.50f;
        [Range(0f, 1f)] public float corporateProfitShare = 0.10f;
        [Range(0f, 1f)] public float capitalGainsShare = 0.03f;
        [Range(0f, 1f)] public float payrollShare = 0.50f;
        [Range(0f, 1f)] public float consumerSpendingShare = 0.68f;
        [Range(0f, 1f)] public float importShare = 0.14f;
        [Range(0f, 0.1f)] public float estateShare = 0.004f;
        [Tooltip("Financial turnover is a MULTIPLE of GDP - trading volume dwarfs output.")]
        [Range(0f, 20f)] public float financialTurnoverMultiple = 3f;
        [Tooltip("Emissions in gigatonnes. A dollars-per-ton carbon tax times this is billions of dollars.")]
        [Range(0f, 20f)] public float carbonEmissionsGigatonnes = 5f;

        [Header("Credit rating yield premiums - GDD 9.5")]
        public float premiumAAA = 0f;
        public float premiumAA = 0.3f;
        public float premiumA = 0.7f;
        public float premiumBBB = 1.5f;
        public float premiumBB = 3.5f;
        public float premiumB = 6f;
        public float premiumCCC = 10f;
    }
}
