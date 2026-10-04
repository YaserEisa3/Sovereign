namespace Sovereign.Core
{
    /// <summary>
    /// GDD 20 Phase 1. The weekly economy. Pure C#, no Unity, and deterministic -
    /// given the same policy the same run repeats, which is what makes the lags
    /// legible instead of noise.
    ///
    /// Everything works in annualised percent and is applied a week at a time.
    /// </summary>
    public partial class EconomySimulator
    {
        public const float WeeksPerYear = 52f;
        const float Weekly = 1f / WeeksPerYear;

        readonly SimulationConfig _config;
        readonly TreasuryModel _treasury;
        readonly PopulationModel _population;
        readonly ApprovalModel _approval;
        readonly EventSystem _events;
        readonly AdvisorModel _advisor;
        readonly GeopoliticsModel _geopolitics;
        readonly HeadlineGenerator _headlines;
        readonly AchievementModel _achievements;
        readonly VictoryModel _victory;
        readonly MilestoneModel _milestones = new MilestoneModel();

        public EconomySimulator(SimulationConfig config)
        {
            _config = config;
            _treasury = new TreasuryModel(config.treasury, config.macro.naturalUnemploymentRate, config.potentialGrowthRate);
            _population = new PopulationModel(config.population);
            _approval = new ApprovalModel(config.approval);
            _events = new EventSystem(config.events);
            _advisor = new AdvisorModel(config.macro.inflationTarget);
            _geopolitics = new GeopoliticsModel(config.geopolitics);
            _headlines = new HeadlineGenerator(config.headlines, config.headlineIntervalWeeks, config.headlineTopicRest);
            _achievements = new AchievementModel(config.achievements);
            _victory = new VictoryModel(config.victory);
        }

        public HeadlineGenerator Headlines { get { return _headlines; } }
        public AchievementModel Achievements { get { return _achievements; } }
        public VictoryModel Victory { get { return _victory; } }
        public MilestoneModel Milestones { get { return _milestones; } }

        public TreasuryModel Treasury { get { return _treasury; } }
        public PopulationModel Population { get { return _population; } }
        public ApprovalModel Approval { get { return _approval; } }
        public EventSystem Events { get { return _events; } }
        public GeopoliticsModel Geopolitics { get { return _geopolitics; } }

        /// <summary>Trend growth plus whatever demography adds or takes away - an ageing
        /// workforce slows it, immigration lifts it (GDD 21).</summary>
        public float Potential(EconomyState state)
        {
            return _config.potentialGrowthRate + state.population.PotentialGrowthOffset - state.events.productivityScar;
        }

        /// <summary>
        /// Runs once both state and policy exist: calibrates the treasury against the
        /// opening budget and fills the first set of line items, so the Fiscal drawer
        /// has numbers before the first week has ticked.
        /// </summary>
        public void Prepare(EconomyState state, PolicyState policy)
        {
            // Order matters: people before the tax base, the tax base before the budget,
            // the budget before anyone forms an opinion of it.
            state.participationRate = _config.population.baseParticipationRate * 100f;
            _population.Initialise(state, policy);

            _treasury.Calibrate(state, policy);
            _treasury.Update(state, policy);
            state.revenueBillions = state.treasury.totalRevenue;
            state.spendingBillions = state.treasury.totalSpending;
            state.budgetBalancePercentGdp = (state.revenueBillions - state.spendingBillions)
                                            / MathUtil.Max(1f, state.nominalGdpBillions) * 100f;

            // The budget the economy starts out used to. Set HERE rather than on the first
            // tick: left lazy, a cut made before the first week became the baseline
            // itself, and austerity cost nothing at all.
            state.settledSpendingBillions = policy.TotalSpendingBillions;

            _approval.Initialise(state, policy);
            _events.Initialise(state, policy);
            _geopolitics.Initialise(state, policy);
            _advisor.Reset(state);
            state.events.newsRandom = new DeterministicRandom(_config.events.seed ^ 0x5bd1e995u);

            // The opening sample is taken HERE, not in CreateState: before this runs there
            // is no population and no budget, and a zero in week one wrecked every chart's
            // scale.
            ApplyOpening(state);
            // Captured after the opening, because a scenario can change the population.
            // Pensions and old-age health are measured against this, so what the country
            // promised its retired on day one is what it goes on paying for.
            state.baselineRetired = state.population.retired;
            state.Series("realRate").Record(state.RealInterestRate);
            state.RecordWeek();
        }

        public SimulationConfig Config { get { return _config; } }

        public EconomyState CreateState()
        {
            EconomyState state = new EconomyState();

            state.nominalGdpBillions = _config.startingNominalGdp;
            state.realGdpGrowth = _config.potentialGrowthRate;
            state.nominalGdpGrowth = _config.potentialGrowthRate + _config.startingInflation;
            state.inflation = _config.startingInflation;
            state.coreInflation = _config.startingInflation;
            state.unemployment = _config.startingUnemployment;
            state.participationRate = 63f;
            state.infrastructureHealth = _config.startingInfrastructureHealth;

            state.monetary.centralBankRate = _config.startingBaseRate;
            state.monetary.credibility = _config.macro.startingCredibility;
            state.monetary.inflationExpectations = _config.macro.inflationTarget;
            state.monetary.moneySupplyM2Billions = _config.startingNominalGdp * 0.9f;
            state.monetary.moneySupplyGrowth = _config.startingInflation + _config.potentialGrowthRate;

            state.bonds.debtStockBillions = _config.startingNominalGdp * _config.startingDebtToGdp;
            state.bonds.averageCoupon = _config.startingAverageCoupon;
            state.bonds.foreignHoldingPercent = _config.startingForeignHolding;

            // How much of the debt reprices soon. A reconstruction loan is long and fixed;
            // a stock of short paper drags the average coupon onto today's yields within
            // a couple of years, whatever the government does.
            state.bonds.maturingWithinOneYear = _config.startingMaturingWithinOneYear;
            state.bonds.maturingOneToFive = _config.startingMaturingOneToFive;
            state.bonds.maturingBeyondFive = MathUtil.Max(0.05f,
                1f - _config.startingMaturingWithinOneYear - _config.startingMaturingOneToFive);
            // Start at the rating the opening position actually deserves. Hardcoding AA
            // against 112% debt triggered a downgrade cascade in the first three years
            // for no reason the player could see. (GDD 18's dashboard shows AA at 112%,
            // which its own 9.5 table would score BBB - the table wins.)
            state.bonds.creditRating = DeservedRating(_config.startingDebtToGdp, 4f, _config.potentialGrowthRate);

            state.currency.fxReservesBillions = _config.startingFxReserves;
            // Starting at zero made the deviation term an instant appreciation shock.
            state.currency.currentAccountPercentGdp = _config.macro.netExportShare * 100f;

            for (int i = 0; i < _config.sectors.Length; i++)
                state.sectors.Add(new EconomicSector(_config.sectors[i]));

            // A country that has just been wrecked starts below its own capacity, and
            // carries the loss of skills and plant as a permanent scar (GDD 17.4).
            state.potentialGdpIndex = state.realGdpIndex - _config.startingOutputGapPercent;
            state.events.productivityScar = _config.startingProductivityScar;

            UpdateYieldCurve(state, null, true);
            return state;
        }

        /// <summary>
        /// One week. The order matters: demand sets output, output sets the labour
        /// market, wages and money set prices, and only then do the markets price
        /// what all of it means for your debt.
        /// </summary>
        public void Tick(EconomyState state, PolicyState policy)
        {
            // A country that has had its revolution does not keep running the economy.
            if (state.IsGameOver) return;

            state.week++;

            _geopolitics.TickWeek(state, policy);
            RevertWorldPrices(state);
            UpdateDemand(state, policy);
            UpdateLabour(state, policy);
            UpdatePrices(state, policy);
            UpdateSectors(state, policy);
            UpdateInfrastructure(state, policy);
            _population.Tick(state, policy);
            UpdateMoney(state, policy);
            UpdateYieldCurve(state, policy);
            UpdateCurrency(state, policy);
            UpdateFiscal(state, policy);
            _approval.Tick(state, policy);

            // The world acts last, on top of everything the week has already settled.
            state.bankCapitalRequirement = policy.bankingCapitalRequirement;
            _events.TickWeek(state, policy);
            _advisor.TickWeek(state, policy);
            _headlines.TickWeek(state, policy, NewsDesk.Gather(state, _config.geopolitics));
            _achievements.TickWeek(state);
            _victory.TickWeek(state);
            _milestones.TickWeek(state);

            state.RecordWeek();
        }

        /// <summary>GDD 4: queued policy lands here and nowhere else. Auctions, wage
        /// resets and credibility scoring are quarterly too.</summary>
        public void TickQuarter(EconomyState state, PolicyState policy)
        {
            policy.CommitQueued();
            RunBuyback(state, policy);
            RunBondAuction(state, policy);
            UpdateWages(state, policy);
            UpdateCredibility(state, policy);
            _events.TickQuarter(state, policy);
            _geopolitics.TickQuarter(state, policy, _events);
        }

        /// <summary>Oil and trade drift back to normal once a shock passes, so an event
        /// leaves a dent that heals rather than a permanent new world.</summary>
        void RevertWorldPrices(EconomyState state)
        {
            EventTuning t = _config.events.tuning;
            state.oilPriceIndex = MathUtil.Approach(state.oilPriceIndex, 100f, t.oilReversion);
            // Trade settles where tariffs, deals and sanctions leave it, not at 100.
            state.tradeVolumeIndex = MathUtil.Approach(state.tradeVolumeIndex, state.geopolitics.tradeTarget, t.tradeReversion);
        }

        /// <summary>GDD 4 and 9.5: the annual rating review.</summary>
        public void TickYear(EconomyState state, PolicyState policy)
        {
            ReviewCreditRating(state, policy);
        }
    }
}
