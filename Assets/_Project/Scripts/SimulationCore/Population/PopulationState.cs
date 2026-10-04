namespace Sovereign.Core
{
    public struct PopulationConfig
    {
        public float startingYouth, startingWorkingAge, startingRetired, startingVeterans;  // millions
        public int youthUpperAge, retirementAge;
        public float baseBirthRate, baseDeathRate;                                          // per 1000 per year
        public float healthSpendingDeathElasticity, healthSpendingBirthElasticity;
        public float costOfLivingBirthElasticity, childCreditBirthElasticity;
        public float baseParticipationRate, discouragedWorkerEffect;                        // 0-1
        public float skilledShareDefault;
        public float veteranBenefitCostPerHead;                                             // dollars per year
        public float veteranConversionRate;                                                 // share of military rotating out per year
        public float veteranMortality;                                                      // share of veterans lost per year
        public float militaryShareOfGovernment;                                             // of the Government sector headcount
        public float naturalUnemploymentRate;                                               // from MacroParameters
    }

    /// <summary>
    /// GDD 7. People, in three bands, and the workers they supply. All counts are in
    /// millions; rates are per year.
    /// </summary>
    public class PopulationState
    {
        public float youth, workingAge, retired, veterans;

        public float birthsPerYear, deathsPerYear, netMigrationPerYear;
        public float birthRate, deathRate;           // per 1000

        public float laborForce, employed, unemployed;
        public float militaryHeadcount;
        /// <summary>GDD 7.4. Sum of headcount x wage across the seven sectors, $B per year.</summary>
        public float taxableIncomePool;

        /// <summary>Health spending at the start of the run - what the health elasticities
        /// are measured against.</summary>
        public float baselineHealthSpending;

        /// <summary>Wage bill as a share of GDP at the start - the unit labour cost baseline.</summary>
        public float baselineLabourShare;

        /// <summary>Annual growth of the labour force from births, ageing, migration and
        /// retirement, in percent. What it was at the start is the baseline; any gap
        /// between the two moves potential growth - GDD 21, demography is destiny.</summary>
        public float laborForceGrowth;
        public float baselineLaborForceGrowth;

        /// <summary>Percentage points added to potential growth by demography.</summary>
        public float PotentialGrowthOffset { get { return laborForceGrowth - baselineLaborForceGrowth; } }

        public float Total { get { return youth + workingAge + retired; } }

        /// <summary>Dependants per worker-age adult - the number that decides how heavy
        /// pensions and schools sit on everyone else.</summary>
        public float DependencyRatio { get { return workingAge <= 0f ? 0f : (youth + retired) / workingAge; } }

        public float VeteranBenefitsBillions(float costPerHead) { return veterans * costPerHead * 0.001f; }
    }
}
