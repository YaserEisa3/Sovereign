namespace Sovereign.Core
{
    /// <summary>
    /// GDD 7. Births, deaths, ageing, migration, veterans, and the labour force they
    /// add up to. The slow half of the game: a child tax credit passed today changes
    /// the workforce in eighteen years, and most runs will never see it pay back.
    /// </summary>
    public class PopulationModel
    {
        const float Weekly = 1f / 52f;
        readonly PopulationConfig _c;

        public PopulationModel(PopulationConfig config) { _c = config; }

        public PopulationConfig Config { get { return _c; } }

        public void Initialise(EconomyState state, PolicyState policy)
        {
            PopulationState p = state.population;
            p.youth = _c.startingYouth;
            p.workingAge = _c.startingWorkingAge;
            p.retired = _c.startingRetired;
            p.veterans = _c.startingVeterans;
            p.baselineHealthSpending = HealthSpending(policy);

            UpdateRates(state, policy);
            p.baselineLaborForceGrowth = p.laborForceGrowth;
            UpdateEmployment(state, policy);
            p.baselineLabourShare = p.taxableIncomePool / MathUtil.Max(1f, state.nominalGdpBillions);
        }

        public void Tick(EconomyState state, PolicyState policy)
        {
            PopulationState p = state.population;
            UpdateRates(state, policy);

            float youthCohorts = _c.youthUpperAge + 1f;
            float workingCohorts = MathUtil.Max(1f, _c.retirementAge - youthCohorts);

            float births = p.Total * p.birthRate * 0.001f;
            float deaths = p.Total * p.deathRate * 0.001f;
            float ageingIn = p.youth / youthCohorts;
            float retiring = p.workingAge / workingCohorts;
            float migration = policy.immigrationInflowMillions * MigrationPull(state);

            // Deaths fall overwhelmingly on the old - GDD 17.4 will lean on this when
            // a pandemic arrives.
            p.youth += (births - ageingIn - deaths * 0.05f) * Weekly;
            p.workingAge += (ageingIn + migration - retiring - deaths * 0.20f) * Weekly;
            p.retired += (retiring - deaths * 0.75f) * Weekly;

            p.birthsPerYear = births;
            p.deathsPerYear = deaths;
            p.netMigrationPerYear = migration;

            p.veterans = MathUtil.Max(0f, p.veterans
                + (p.militaryHeadcount * _c.veteranConversionRate - p.veterans * _c.veteranMortality) * Weekly);

            UpdateEmployment(state, policy);
        }

        /// <summary>
        /// GDD 17.4. How much of the immigration quota actually turns up. The policy dial
        /// is a door, not a magnet: nobody moves to a country with no work and nowhere
        /// affordable to live, however wide it is opened. Two thirds of the decision is
        /// whether there are jobs, a third whether housing can be paid for.
        ///
        /// A floor remains whatever happens - family, asylum, people with nowhere better
        /// to go - so the door never closes itself completely.
        /// </summary>
        float MigrationPull(EconomyState state)
        {
            float slack = MathUtil.Max(0f, state.unemployment - _c.naturalUnemploymentRate);
            float jobs = MathUtil.Clamp01(1f - slack / MathUtil.Max(0.1f, _c.migrationJobsSpan));

            // Housing measured in real terms against the opening, like everything else the
            // player is judged on: it is what a wage will buy, not the sticker price.
            float realHousing = state.priceLevel <= 0f ? 0f
                              : state.housingCostIndex / state.priceLevel * 100f - 100f;
            float affordable = MathUtil.Clamp01(1f - MathUtil.Max(0f, realHousing)
                                                / MathUtil.Max(1f, _c.migrationHousingSpan));

            float pull = jobs * _c.migrationJobsWeight + affordable * (1f - _c.migrationJobsWeight);
            return MathUtil.Clamp(pull, _c.migrationFloor, 1f);
        }

        void UpdateRates(EconomyState state, PolicyState policy)
        {
            PopulationState p = state.population;

            // Per $100B of health spending above where the run started.
            float healthDelta = (HealthSpending(policy) - p.baselineHealthSpending) * 0.01f;
            float creditThousands = policy.Tax(TaxKeys.ChildCredit) * 0.001f;
            float realHousing = state.priceLevel <= 0f ? 0f : state.housingCostIndex / state.priceLevel * 100f - 100f;

            // GDD 17.4. Whether people start families turns on whether they can see
            // themselves keeping one: work, and what a wage buys. Neither was an input
            // here - a country could sit at 16% unemployment with wages a fifth below
            // pre-war and have babies at exactly the rate it did in peacetime.
            float jobless = MathUtil.Max(0f, state.unemployment - _c.naturalUnemploymentRate);
            float wageShortfall = MathUtil.Max(0f, 100f - state.Series("realWage").Latest);

            // Event offsets (a pandemic, a disaster) hold only while the event does.
            p.birthRate = MathUtil.Max(4f, _c.baseBirthRate
                + healthDelta * _c.healthSpendingBirthElasticity
                + creditThousands * _c.childCreditBirthElasticity
                - MathUtil.Max(0f, realHousing) * _c.costOfLivingBirthElasticity * 0.1f
                - jobless * _c.birthRateJobsElasticity
                - wageShortfall * _c.birthRateWageElasticity
                + state.events.birthRateOffset);

            p.deathRate = MathUtil.Max(4f, _c.baseDeathRate - healthDelta * _c.healthSpendingDeathElasticity
                + state.events.deathRateOffset);

            float youthCohorts = _c.youthUpperAge + 1f;
            float workingCohorts = MathUtil.Max(1f, _c.retirementAge - youthCohorts);
            float flow = p.youth / youthCohorts + policy.immigrationInflowMillions
                         - p.workingAge / workingCohorts - p.Total * p.deathRate * 0.001f * 0.20f;
            p.laborForceGrowth = p.workingAge <= 0f ? 0f : flow / p.workingAge * 100f;
        }

        /// <summary>
        /// GDD 7.3 and 7.4. Working age times participation is the labour force; the
        /// economy's unemployment rate decides how much of it works; the seven sectors
        /// split the workers by their share and health, and headcount times wage is
        /// the taxable income pool.
        /// </summary>
        void UpdateEmployment(EconomyState state, PolicyState policy)
        {
            PopulationState p = state.population;

            float slack = MathUtil.Clamp01((state.unemployment - _c.naturalUnemploymentRate) / 10f);
            float participationTarget = _c.baseParticipationRate * 100f * (1f - _c.discouragedWorkerEffect * slack);
            state.participationRate = MathUtil.Approach(state.participationRate, participationTarget, 0.01f);

            // A pandemic keeps the sick and their carers out of work while it lasts.
            p.laborForce = p.workingAge * state.participationRate * 0.01f
                           * MathUtil.Max(0.5f, 1f + state.events.laborForceOffsetPercent * 0.01f);
            p.employed = p.laborForce * (1f - state.unemployment * 0.01f);
            p.unemployed = p.laborForce - p.employed;

            float weightTotal = 0f;
            for (int i = 0; i < state.sectors.Count; i++)
                weightTotal += state.sectors[i].employmentShare * (0.7f + state.sectors[i].health * 0.006f);

            float pool = 0f;
            for (int i = 0; i < state.sectors.Count; i++)
            {
                EconomicSector sector = state.sectors[i];
                float weight = sector.employmentShare * (0.7f + sector.health * 0.006f);
                sector.headcount = weightTotal <= 0f ? 0f : p.employed * weight / weightTotal;
                pool += sector.WageBillBillions;
                if (sector.id == SectorId.GovernmentMilitary)
                    p.militaryHeadcount = sector.headcount * _c.militaryShareOfGovernment;
            }
            p.taxableIncomePool = pool;
        }

        static float HealthSpending(PolicyState policy)
        {
            return policy.Spending(SpendKeys.Medicaid) + policy.Spending(SpendKeys.Medicare) + policy.Spending(SpendKeys.PublicHealth);
        }
    }
}
