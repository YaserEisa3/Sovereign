namespace Sovereign.Core
{
    /// <summary>
    /// What an active event does, every week. Each impact pushes on its target by
    /// its weekly magnitude, scaled by severity and softened by whatever preparation
    /// was in place when it struck. Level targets (growth, inflation, confidence,
    /// yields) are pulled back by their own models each week, so a steady push
    /// settles into a steady offset for as long as the event lasts - and fades when
    /// it ends. Stocks (infrastructure, reserves, oil) accumulate. Rates (births,
    /// deaths, the labour force) are offsets that hold only while the event does.
    /// </summary>
    public partial class EventSystem
    {
        void ApplyImpacts(EconomyState s, PolicyState policy, EventInstance e)
        {
            EventConfig cfg = _c.events[e.configIndex];

            foreach (ImpactConfig impact in cfg.impacts)
            {
                float k = e.severity * (1f - impact.mitigable * e.mitigation);
                float push = impact.weeklyMagnitude * k;

                switch (impact.target)
                {
                    case ImpactTarget.RealGdpGrowth: s.realGdpGrowth += push; break;
                    case ImpactTarget.Inflation: s.inflation += push; break;
                    case ImpactTarget.Unemployment: s.unemployment = MathUtil.Clamp(s.unemployment + push, 0.5f, 40f); break;
                    case ImpactTarget.ConsumerConfidence: s.consumerConfidence = MathUtil.Clamp(s.consumerConfidence + push, 0f, 100f); break;
                    case ImpactTarget.BusinessInvestment: s.businessInvestmentIndex = MathUtil.Clamp(s.businessInvestmentIndex + push, 0f, 100f); break;
                    case ImpactTarget.CurrencyStrength: s.currency.exchangeRateIndex = MathUtil.Clamp(s.currency.exchangeRateIndex + push, 10f, 400f); break;
                    case ImpactTarget.ShortYield: s.bonds.yields.shortTermYield += push; break;
                    case ImpactTarget.LongYield: s.bonds.yields.longTermYield += push; break;
                    case ImpactTarget.OilPrice: s.oilPriceIndex = MathUtil.Max(20f, s.oilPriceIndex + push); break;
                    case ImpactTarget.TradeVolume: s.tradeVolumeIndex = MathUtil.Clamp(s.tradeVolumeIndex + push, 10f, 200f); break;
                    case ImpactTarget.InfrastructureHealth: s.infrastructureHealth = MathUtil.Clamp(s.infrastructureHealth + push, 0f, 100f); break;
                    case ImpactTarget.PopulationDeathRate: s.events.deathRateOffset += push; break;
                    case ImpactTarget.PopulationBirthRate: s.events.birthRateOffset += push; break;
                    case ImpactTarget.LaborForce: s.events.laborForceOffsetPercent += push; break;
                    case ImpactTarget.FxReserves: s.currency.fxReservesBillions = MathUtil.Max(0f, s.currency.fxReservesBillions + push); break;
                    case ImpactTarget.DebtStock: s.bonds.debtStockBillions = MathUtil.Max(0f, s.bonds.debtStockBillions + push); break;
                    case ImpactTarget.Gini: s.giniCoefficient = MathUtil.Clamp(s.giniCoefficient + push, 0.2f, 0.7f); break;
                    case ImpactTarget.MoneySupply: s.monetary.moneySupplyGrowth += push; break;
                    case ImpactTarget.ApprovalOverall: PushApproval(s, push); break;
                    case ImpactTarget.SectorHealth: PushSectors(s, impact, push); break;
                }
            }

            ApplySpecial(s, e, cfg);
        }

        /// <summary>The behaviours an impact list cannot express on its own.</summary>
        void ApplySpecial(EconomyState s, EventInstance e, EventConfig cfg)
        {
            // GDD 17.7, channel five: a foreign crisis sends capital looking for safety.
            // With a credible central bank you are the safe haven and the currency
            // strengthens (hurting manufacturing); without one, capital flees.
            if (cfg.category == EventCategory.ForeignContagion)
            {
                float direction = s.monetary.credibility >= _t.safeHavenCredibility ? 1f : -1f;
                s.currency.exchangeRateIndex = MathUtil.Clamp(
                    s.currency.exchangeRateIndex + direction * _t.safeHavenWeeklyCurrency * e.severity, 10f, 400f);
            }

            // GDD 9.3: a dumping attack is foreign holders actually selling.
            if (cfg.key == "BondDumpingAttack")
                s.bonds.foreignHoldingPercent = MathUtil.Max(0f, s.bonds.foreignHoldingPercent - _t.bondDumpingHoldingLoss * e.severity);

            // Refugees are people, and they stay.
            if (cfg.key == "RefugeeInflow")
                s.population.workingAge += _t.refugeeWorkingAgeWeekly * e.severity;
        }

        static void PushApproval(EconomyState s, float push)
        {
            ApprovalState a = s.approval;
            a.poor = MathUtil.Clamp(a.poor + push, 0f, 100f);
            a.middle = MathUtil.Clamp(a.middle + push, 0f, 100f);
            a.wealthy = MathUtil.Clamp(a.wealthy + push, 0f, 100f);
            a.left = MathUtil.Clamp(a.left + push, 0f, 100f);
            a.centre = MathUtil.Clamp(a.centre + push, 0f, 100f);
            a.right = MathUtil.Clamp(a.right + push, 0f, 100f);
        }

        static void PushSectors(EconomyState s, ImpactConfig impact, float push)
        {
            foreach (EconomicSector sector in s.sectors)
                if (!impact.sectorSpecific || sector.id == impact.sector)
                    sector.health = MathUtil.Clamp(sector.health + push, 0f, 100f);
        }

        /// <summary>
        /// GDD 17: how much of the damage your preparation absorbs, decided once, at
        /// the strike. Money spent after the fact does not reach back.
        /// </summary>
        float Mitigation(EventCategory category, PolicyState policy, EconomyState state)
        {
            float m;
            switch (category)
            {
                case EventCategory.NaturalDisaster:
                {
                    float fund = policy.Spending(SpendKeys.DisasterReliefFund);
                    m = fund / MathUtil.Max(1f, fund + _t.disasterFundHalfEffect);
                    break;
                }
                case EventCategory.Pandemic:
                    m = _t.pandemicPublicHealthWeight * Ratio(policy.Spending(SpendKeys.PublicHealth), state.events.baselinePublicHealth)
                        + _t.pandemicHealthWeight * Ratio(Health(policy), state.events.baselineHealth);
                    break;
                case EventCategory.War:
                    m = _t.warDefenceWeight * Ratio(Defence(policy), state.events.baselineDefence);
                    break;
                case EventCategory.FinancialCrisis:
                    m = (policy.bankingCapitalRequirement - _t.financialCapitalFloor) / MathUtil.Max(0.1f, _t.financialCapitalRange);
                    break;
                default:
                    m = 0f;
                    break;
            }
            return MathUtil.Clamp(m, 0f, _t.mitigationCap);
        }

        static float Ratio(float current, float baseline) { return baseline <= 0f ? 1f : current / baseline; }

        static float Health(PolicyState p)
        {
            return p.Spending(SpendKeys.Medicaid) + p.Spending(SpendKeys.Medicare) + p.Spending(SpendKeys.PublicHealth);
        }

        static float Defence(PolicyState p)
        {
            return p.Spending(SpendKeys.MilitaryPersonnel) + p.Spending("MilitaryEquipment") + p.Spending("MilitaryRnD")
                   + p.Spending(SpendKeys.MilitaryOperations) + p.Spending("HomelandSecurity");
        }
    }
}
