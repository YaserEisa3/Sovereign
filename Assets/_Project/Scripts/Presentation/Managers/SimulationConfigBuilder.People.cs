using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>Population and approval across the boundary.</summary>
    public static partial class SimulationConfigBuilder
    {
        static PopulationConfig BuildPopulation(GameDatabase database)
        {
            PopulationParameters p = database.Population;
            WarParameters w = database.War;

            return new PopulationConfig
            {
                startingYouth = p.startingYouth,
                startingWorkingAge = p.startingWorkingAge,
                startingRetired = p.startingRetired,
                startingVeterans = p.startingVeterans,
                youthUpperAge = p.youthUpperAge,
                retirementAge = p.retirementAge,
                baseBirthRate = p.baseBirthRate,
                baseDeathRate = p.baseDeathRate,
                healthSpendingDeathElasticity = p.healthSpendingDeathElasticity,
                healthSpendingBirthElasticity = p.healthSpendingBirthElasticity,
                costOfLivingBirthElasticity = p.costOfLivingBirthElasticity,
                childCreditBirthElasticity = p.childCreditBirthElasticity,
                baseParticipationRate = p.baseParticipationRate,
                discouragedWorkerEffect = p.discouragedWorkerEffect,
                skilledShareDefault = p.defaultSkilledShare,
                veteranBenefitCostPerHead = p.veteranBenefitCostPerHead,
                veteranConversionRate = w != null ? w.veteranConversionRate : 0.12f,
                veteranMortality = p.veteranMortality,
                militaryShareOfGovernment = p.militaryShareOfGovernment,
                naturalUnemploymentRate = database.Macro.naturalUnemploymentRate
            };
        }

        static ApprovalConfig BuildApproval(ApprovalWeights a)
        {
            return new ApprovalConfig
            {
                poorShare = a.poorShare, middleShare = a.middleShare, wealthyShare = a.wealthyShare,

                poorUnemployment = a.poorUnemployment, poorInflation = a.poorInflation, poorWelfare = a.poorWelfare,
                poorHealthcare = a.poorHealthcare, poorMinimumWage = a.poorMinimumWage,

                middleRealWages = a.middleRealWages, middleEmployment = a.middleEmployment,
                middleHousingCosts = a.middleHousingCosts, middleIncomeTax = a.middleIncomeTax,
                middleEducation = a.middleEducation, middleConfidence = a.middleConfidence,

                wealthyCorporateTax = a.wealthyCorporateTax, wealthyCapitalGains = a.wealthyCapitalGains,
                wealthyGrowth = a.wealthyGrowth, wealthyDebtFiscal = a.wealthyDebtFiscal,
                wealthyRegulation = a.wealthyRegulation, wealthyStability = a.wealthyStability,

                leftGini = a.leftGini, leftGreenPolicy = a.leftGreenPolicy, leftSocialSpending = a.leftSocialSpending,
                leftCorporateTax = a.leftCorporateTax, leftHealthcare = a.leftHealthcare,

                centreGrowth = a.centreGrowth, centreInflation = a.centreInflation,
                centreUnemployment = a.centreUnemployment, centreBudgetBalance = a.centreBudgetBalance,

                rightDebtLevel = a.rightDebtLevel, rightTaxRates = a.rightTaxRates, rightDeregulation = a.rightDeregulation,
                rightDefence = a.rightDefence, rightGrowth = a.rightGrowth,

                immigrationRightApproval = a.immigrationRightApproval,
                immigrationLeftApproval = a.immigrationLeftApproval,

                unrestApprovalThreshold = a.unrestApprovalThreshold,
                unrestApprovalWeeks = a.unrestApprovalWeeks,
                rapidDeclinePointsPerMonth = a.rapidDeclinePointsPerMonth,
                unrestInflationThreshold = a.unrestInflationThreshold,
                unrestUnemploymentThreshold = a.unrestUnemploymentThreshold,
                unrestBuildPerPointBelow = a.unrestBuildPerPointBelow,
                unrestPerCrisis = a.unrestPerCrisis,
                unrestDecayPerWeek = a.unrestDecayPerWeek,

                revoltOverallApproval = a.revoltOverallApproval,
                revoltOverallWeeks = a.revoltOverallWeeks,
                revoltSingleClassUnrest = a.revoltSingleClassUnrest,
                revoltSingleClassWeeks = a.revoltSingleClassWeeks,
                hyperinflationMonthlyThreshold = a.hyperinflationMonthlyThreshold,

                startPoor = a.startingPoor, startMiddle = a.startingMiddle, startWealthy = a.startingWealthy,
                startLeft = a.startingLeft, startCentre = a.startingCentre, startRight = a.startingRight,
                adjustmentSpeed = a.adjustmentSpeed
            };
        }
    }
}
