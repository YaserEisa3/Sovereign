using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 14. Every weight in the approval model, in one asset. ApprovalSystem
    /// reads these - it does not hardcode them. Each block should sum to 1.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ApprovalWeights", menuName = "Sovereign/Parameters/Approval Weights")]
    public class ApprovalWeights : ScriptableObject
    {
        [Header("Population shares (should sum to 1)")]
        [Range(0f, 1f)] public float poorShare = 0.30f;
        [Range(0f, 1f)] public float middleShare = 0.50f;
        [Range(0f, 1f)] public float wealthyShare = 0.20f;

        [Header("Poor - bottom 30%")]
        [Range(0f, 1f)] public float poorUnemployment = 0.30f;
        [Range(0f, 1f)] public float poorInflation = 0.25f;
        [Range(0f, 1f)] public float poorWelfare = 0.20f;
        [Range(0f, 1f)] public float poorHealthcare = 0.15f;
        [Range(0f, 1f)] public float poorMinimumWage = 0.10f;

        [Header("Middle class")]
        [Range(0f, 1f)] public float middleRealWages = 0.25f;
        [Range(0f, 1f)] public float middleEmployment = 0.20f;
        [Range(0f, 1f)] public float middleHousingCosts = 0.15f;
        [Range(0f, 1f)] public float middleIncomeTax = 0.15f;
        [Range(0f, 1f)] public float middleEducation = 0.10f;
        [Range(0f, 1f)] public float middleConfidence = 0.15f;

        [Header("Wealthy - top 20%")]
        [Range(0f, 1f)] public float wealthyCorporateTax = 0.20f;
        [Range(0f, 1f)] public float wealthyCapitalGains = 0.15f;
        [Range(0f, 1f)] public float wealthyGrowth = 0.20f;
        [Range(0f, 1f)] public float wealthyDebtFiscal = 0.20f;
        [Range(0f, 1f)] public float wealthyRegulation = 0.15f;
        [Range(0f, 1f)] public float wealthyStability = 0.10f;

        [Header("Progressive Left")]
        [Range(0f, 1f)] public float leftGini = 0.25f;
        [Range(0f, 1f)] public float leftGreenPolicy = 0.15f;
        [Range(0f, 1f)] public float leftSocialSpending = 0.25f;
        [Range(0f, 1f)] public float leftCorporateTax = 0.20f;
        [Range(0f, 1f)] public float leftHealthcare = 0.15f;

        [Header("Centrist")]
        [Range(0f, 1f)] public float centreGrowth = 0.30f;
        [Range(0f, 1f)] public float centreInflation = 0.25f;
        [Range(0f, 1f)] public float centreUnemployment = 0.20f;
        [Range(0f, 1f)] public float centreBudgetBalance = 0.25f;

        [Header("Conservative Right")]
        [Range(0f, 1f)] public float rightDebtLevel = 0.30f;
        [Range(0f, 1f)] public float rightTaxRates = 0.25f;
        [Range(0f, 1f)] public float rightDeregulation = 0.20f;
        [Range(0f, 1f)] public float rightDefence = 0.15f;
        [Range(0f, 1f)] public float rightGrowth = 0.10f;

        [Header("Immigration - contested, GDD 7.6")]
        [Tooltip("Approval change per million of annual inflow. Conservative falls, Progressive rises. The POOR reach it through wages and housing, not through this line.")]
        public float immigrationRightApproval = -4f;
        public float immigrationLeftApproval = 2.5f;

        [Header("Opening approval - GDD 18 dashboard")]
        [Tooltip("Where each group starts. Scoring is calibrated against these at boot, so approval moves with CHANGES from the opening position.")]
        [Range(0f, 100f)] public float startingPoor = 61f;
        [Range(0f, 100f)] public float startingMiddle = 74f;
        [Range(0f, 100f)] public float startingWealthy = 52f;
        [Range(0f, 100f)] public float startingLeft = 58f;
        [Range(0f, 100f)] public float startingCentre = 72f;
        [Range(0f, 100f)] public float startingRight = 49f;

        [Tooltip("How much of the gap to target approval closes each week. Approval is sticky - people take time to notice.")]
        [Range(0.005f, 0.5f)] public float adjustmentSpeed = 0.04f;

        [Header("Unrest and revolt - GDD 14")]
        [Tooltip("Class approval below this for the window below starts unrest building.")]
        [Range(0f, 100f)] public float unrestApprovalThreshold = 40f;
        [Range(1, 26)] public int unrestApprovalWeeks = 4;
        [Tooltip("Approval lost per month that counts as a rapid decline.")]
        public float rapidDeclinePointsPerMonth = 5f;
        [Range(0f, 100f)] public float unrestInflationThreshold = 15f;
        [Range(0f, 100f)] public float unrestUnemploymentThreshold = 12f;

        [Header("How fast unrest moves")]
        [Tooltip("Unrest added per week for EACH point a class sits below the threshold. At 0.12, a class at 38% simmers (+0.24/week, years to boil) while a class at 15% boils (+3/week, months). A flat rate made 38% as dangerous as 5%.")]
        [Range(0f, 1f)] public float unrestBuildPerPointBelow = 0.12f;
        [Tooltip("Unrest added per week for each crisis condition that holds: a rapid slide, runaway inflation, mass unemployment.")]
        [Range(0f, 5f)] public float unrestPerCrisis = 1f;
        [Tooltip("Unrest shed per week when nothing is feeding it.")]
        [Range(0f, 5f)] public float unrestDecayPerWeek = 0.5f;

        [Header("Revolt triggers")]
        [Range(0f, 100f)] public float revoltOverallApproval = 20f;
        [Range(1, 104)] public int revoltOverallWeeks = 26;
        [Range(0f, 100f)] public float revoltSingleClassUnrest = 90f;
        [Range(1, 52)] public int revoltSingleClassWeeks = 9;
        [Tooltip("Monthly inflation above this for three months is hyperinflation and ends the run.")]
        public float hyperinflationMonthlyThreshold = 50f;
    }
}
