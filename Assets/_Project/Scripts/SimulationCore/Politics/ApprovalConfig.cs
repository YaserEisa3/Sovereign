namespace Sovereign.Core
{
    /// <summary>GDD 14. Every weight, threshold and opening approval, copied from the
    /// ApprovalWeights asset at boot.</summary>
    public struct ApprovalConfig
    {
        public float poorShare, middleShare, wealthyShare;

        public float poorUnemployment, poorInflation, poorWelfare, poorHealthcare, poorMinimumWage;
        public float middleRealWages, middleEmployment, middleHousingCosts, middleIncomeTax, middleEducation, middleConfidence;
        public float wealthyCorporateTax, wealthyCapitalGains, wealthyGrowth, wealthyDebtFiscal, wealthyRegulation, wealthyStability;
        public float leftGini, leftGreenPolicy, leftSocialSpending, leftCorporateTax, leftHealthcare;
        public float centreGrowth, centreInflation, centreUnemployment, centreBudgetBalance;
        public float rightDebtLevel, rightTaxRates, rightDeregulation, rightDefence, rightGrowth;

        public float immigrationRightApproval, immigrationLeftApproval;

        public float unrestApprovalThreshold;
        public int unrestApprovalWeeks;
        public float rapidDeclinePointsPerMonth;
        public float unrestInflationThreshold, unrestUnemploymentThreshold;
        public float unrestBuildPerPointBelow, unrestPerCrisis, unrestDecayPerWeek;

        public float revoltOverallApproval;
        public int revoltOverallWeeks;
        public float revoltSingleClassUnrest;
        public int revoltSingleClassWeeks;
        public float hyperinflationMonthlyThreshold;

        /// <summary>Where each group starts. The scoring is calibrated against these at
        /// boot, so approval moves with CHANGES in conditions from the opening
        /// position rather than with an arbitrary absolute score.</summary>
        public float startPoor, startMiddle, startWealthy, startLeft, startCentre, startRight;

        /// <summary>How much of the gap to its target approval closes each week.</summary>
        public float adjustmentSpeed;
    }

    /// <summary>GDD 14. How each class and faction feels, and how close the street is.</summary>
    public class ApprovalState
    {
        public float poor, middle, wealthy, left, centre, right, overall;

        /// <summary>Calibration offsets, fixed at boot. Indexed poor, middle, wealthy,
        /// left, centre, right.</summary>
        public readonly float[] offsets = new float[6];

        // What the run started with, so spending is judged against it.
        public float baselineWelfare, baselineHealth, baselineEducation, baselineSocial, baselineDefence;
        public float baselineImmigration;

        public float unrestPoor, unrestMiddle, unrestWealthy;
        public int weeksPoorLow, weeksMiddleLow, weeksWealthyLow;
        public int weeksPoorRevolting, weeksMiddleRevolting, weeksWealthyRevolting;
        public int weeksOverallCritical, weeksHyperinflation;

        public bool revolt;
        public string revoltReason = "";

        public float HighestUnrest
        {
            get { return MathUtil.Max(unrestPoor, MathUtil.Max(unrestMiddle, unrestWealthy)); }
        }
    }
}
