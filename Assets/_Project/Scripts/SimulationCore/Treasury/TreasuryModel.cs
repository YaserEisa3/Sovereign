using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>GDD 8. Where the money came from and where it went, line by line.</summary>
    public class TreasuryState
    {
        public readonly Dictionary<string, float> revenueByLine = new Dictionary<string, float>();
        public readonly Dictionary<string, float> spendingByLine = new Dictionary<string, float>();
        public float totalRevenue;
        public float totalSpending;
        public float debtService;
        /// <summary>Scale applied to every revenue estimate - see TreasuryConfig.</summary>
        public float calibration = 1f;
    }

    /// <summary>
    /// GDD 8. Revenue is rate x base x collection, line by line, with the base
    /// shrinking as the rate climbs (the Laffer term in each TaxDefinition). The
    /// bases move with the cycle, so revenue falls in a recession whether or not
    /// you touch a rate - one of the two automatic stabilisers of GDD 8.3. The other
    /// is spending: benefit lines rise with unemployment on their own.
    /// </summary>
    public class TreasuryModel
    {
        readonly TreasuryConfig _config;
        readonly float _naturalUnemployment;
        readonly float _potentialGrowth;

        public TreasuryModel(TreasuryConfig config, float naturalUnemployment, float potentialGrowth)
        {
            _config = config;
            _naturalUnemployment = naturalUnemployment;
            _potentialGrowth = potentialGrowth;
        }

        public TreasuryConfig Config { get { return _config; } }

        /// <summary>Fixes the calibration against the opening position. Called once.</summary>
        public void Calibrate(EconomyState state, PolicyState policy)
        {
            state.treasury.calibration = 1f;
            float raw = RawRevenue(state, policy);
            float target = state.nominalGdpBillions * _config.openingRevenueShareOfGdp;
            state.treasury.calibration = raw <= 0f ? 1f : target / raw;
        }

        public void Update(EconomyState state, PolicyState policy)
        {
            TreasuryState t = state.treasury;
            t.revenueByLine.Clear();
            t.spendingByLine.Clear();

            float revenue = 0f;
            for (int i = 0; i < _config.taxes.Length; i++)
            {
                TaxLineConfig line = _config.taxes[i];
                float amount = LineRevenue(line, policy.Tax(line.key), state) * t.calibration;
                t.revenueByLine[line.key] = amount;
                revenue += amount;
            }

            // GDD 13: per-nation tariffs raise money too, on that nation's share of imports.
            float nationTariffs = state.nominalGdpBillions * _config.bases.importShare
                                  * (state.currency.exchangeRateIndex * 0.01f)
                                  * state.geopolitics.importTariffWeighted * 0.01f * 0.85f * t.calibration;
            t.revenueByLine["NationTariffs"] = nationTariffs;
            revenue += nationTariffs;

            float priceIndex = state.priceLevel * 0.01f;
            float slack = MathUtil.Max(0f, state.unemployment - _naturalUnemployment);
            float spending = 0f;

            for (int i = 0; i < _config.spending.Length; i++)
            {
                SpendLineConfig line = _config.spending[i];
                if (line.key == SpendKeys.DebtService) continue;

                float amount = policy.Spending(line.key);

                // GDD 22.4: veterans benefits are the veteran count times the cost of one,
                // not a number anyone sets - the bill a war sends years after it ends.
                if (line.key == SpendKeys.VeteransBenefits && _config.veteranBenefitCostPerHead > 0f)
                    amount = state.population.veterans * _config.veteranBenefitCostPerHead * 0.001f;

                // GDD 8.3: benefit claims rise with unemployment whatever you decide.
                if (line.isAutomaticStabiliser) amount += line.stabiliserSensitivity * slack;
                amount *= priceIndex;

                t.spendingByLine[line.key] = amount;
                spending += amount;
            }

            // GDD 13: export subsidies are real money.
            float subsidies = policy.exportSubsidyBillions * priceIndex;
            t.spendingByLine["ExportSubsidies"] = subsidies;
            spending += subsidies;

            t.debtService = state.bonds.AnnualDebtServiceBillions;
            t.spendingByLine[SpendKeys.DebtService] = t.debtService;

            t.totalRevenue = revenue;
            t.totalSpending = spending + t.debtService;
        }

        /// <summary>
        /// What one line would raise at a given rate, today. This is the number the
        /// Fiscal drawer shows BEFORE a change is queued - GDD 18's live preview.
        /// </summary>
        public float EstimateLine(string key, float rate, EconomyState state)
        {
            for (int i = 0; i < _config.taxes.Length; i++)
                if (_config.taxes[i].key == key)
                    return LineRevenue(_config.taxes[i], rate, state) * state.treasury.calibration;
            return 0f;
        }

        /// <summary>
        /// What a tax line is actually charged ON, in $B - the number "5%" is 5% of.
        /// A rate with no visible base is a number the player cannot reason about.
        /// </summary>
        public float LineBaseBillions(string key, EconomyState state)
        {
            for (int i = 0; i < _config.taxes.Length; i++)
                if (_config.taxes[i].key == key)
                    return BaseSize(_config.taxes[i].revenueBase, state) * _config.taxes[i].baseShare;
            return 0f;
        }

        /// <summary>The imports one nation's tariff is charged on, in $B.</summary>
        public float NationImportBaseBillions(EconomyState state, float tradeShare)
        {
            return state.nominalGdpBillions * _config.bases.importShare
                   * (state.currency.exchangeRateIndex * 0.01f) * tradeShare * 0.85f * state.treasury.calibration;
        }

        /// <summary>And what a given rate on that nation would raise, in $B a year.</summary>
        public float NationTariffRevenue(EconomyState state, float tradeShare, float ratePercent)
        {
            return NationImportBaseBillions(state, tradeShare) * ratePercent * 0.01f;
        }

        float RawRevenue(EconomyState state, PolicyState policy)
        {
            float total = 0f;
            for (int i = 0; i < _config.taxes.Length; i++)
                total += LineRevenue(_config.taxes[i], policy.Tax(_config.taxes[i].key), state);
            return total;
        }

        float LineRevenue(TaxLineConfig line, float rate, EconomyState state)
        {
            float baseSize = BaseSize(line.revenueBase, state) * line.baseShare;

            // The base shrinks as the rate rises above where it started: avoidance,
            // relocation, less activity. Normalised by the control's range so a percent
            // tax and a dollars-per-ton tax shrink on the same scale.
            //
            // The response is CONVEX, because avoidance is. A few points above the
            // going rate costs you almost nothing; doubling the rate moves the money,
            // the people and the activity somewhere else. Linear shrinkage let a
            // government tax every line to its ceiling and clear a 172% debt in five
            // years, which is not a decision - it is a free lunch with a button on it.
            float range = MathUtil.Max(0.0001f, line.maximumValue - line.minimumValue);
            float excess = (rate - line.defaultValue) / range;
            float shrink = excess <= 0f
                ? 1f - line.baseElasticity * excess
                : 1f - line.baseElasticity * (excess + _config.avoidanceCurvature * excess * excess);
            baseSize *= MathUtil.Clamp(shrink, 0.05f, 1.5f);

            float amount;
            switch (line.unit)
            {
                case PolicyUnit.DollarsPerTon:  amount = rate * baseSize; break;           // $/t x Gt = $B
                case PolicyUnit.DollarsPerUnit: amount = rate * baseSize * 0.001f; break;  // $ x millions = $B/1000
                default:                        amount = rate * 0.01f * baseSize; break;
            }

            amount *= line.collectionEfficiency;
            return line.isCredit ? -amount : amount;
        }

        /// <summary>The base each tax is levied on, moving with the cycle.</summary>
        float BaseSize(RevenueBase kind, EconomyState state)
        {
            TaxBaseConfig b = _config.bases;
            float gdp = state.nominalGdpBillions;

            // Wages fall with employment; profits and gains swing harder than wages.
            float employment = (100f - state.unemployment) / MathUtil.Max(1f, 100f - _naturalUnemployment);
            float cycle = 1f + (state.realGdpGrowth - _potentialGrowth) * 0.04f;

            switch (kind)
            {
                // GDD 7.4: the real taxable income pool - headcount times wage across the
                // seven sectors - once the population model has produced one. It already
                // moves with employment, so it is not scaled again here.
                case RevenueBase.TaxableIncomePool:
                    return state.population.taxableIncomePool > 0f
                        ? state.population.taxableIncomePool
                        : gdp * b.taxableIncomeShare * employment;
                case RevenueBase.Payroll:
                    return state.population.taxableIncomePool > 0f
                        ? state.population.taxableIncomePool
                        : gdp * b.payrollShare * employment;
                // GDD 15: whatever has fled to the haven is not here to be taxed.
                case RevenueBase.CorporateProfits:
                    return gdp * b.corporateProfitShare * MathUtil.Max(0.2f, cycle * cycle) * (1f - state.geopolitics.capitalFlight);
                case RevenueBase.CapitalGains:
                    return gdp * b.capitalGainsShare * MathUtil.Max(0.1f, cycle * cycle * cycle) * (1f - state.geopolitics.capitalFlight);
                case RevenueBase.ConsumerSpending: return gdp * b.consumerSpendingShare * (0.7f + state.consumerConfidence * 0.006f);
                case RevenueBase.Imports: return gdp * b.importShare * (state.currency.exchangeRateIndex * 0.01f);
                case RevenueBase.Estates: return gdp * b.estateShare;
                case RevenueBase.FinancialTransactions: return gdp * b.financialTurnoverMultiple * MathUtil.Max(0.2f, cycle);
                case RevenueBase.CarbonEmissions: return b.carbonEmissionsGigatonnes;
                case RevenueBase.PerChildCredit: return b.childrenMillions;
                default: return 0f;
            }
        }
    }
}
