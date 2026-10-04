namespace Sovereign.Core
{
    /// <summary>GDD 10, term for term, in the document's own order.</summary>
    public partial class EconomySimulator
    {
        void UpdateCurrency(EconomyState state, PolicyState policy)
        {
            MacroConfig m = _config.macro;
            CurrencyState fx = state.currency;

            // Every term is a DEVIATION from the opening equilibrium, not a level.
            // As levels they summed to a constant negative, so the currency drifted
            // one way forever while the player did nothing.
            float rateDifferential = (state.monetary.centralBankRate - (m.neutralRealRate + m.inflationTarget))
                                     * m.currencyRateDifferentialWeight;
            float structuralCurrentAccount = m.netExportShare * 100f;
            float currentAccount = (fx.currentAccountPercentGdp - structuralCurrentAccount) * m.currencyCurrentAccountWeight;
            float inflationDifferential = -(state.inflation - m.inflationTarget) * m.currencyInflationWeight;
            float qeExpansion = -(state.monetary.qeBalanceBillions / MathUtil.Max(1f, state.nominalGdpBillions))
                                * 100f * m.currencyQeWeight;
            float politicalRisk = -(m.startingCredibility - state.monetary.credibility) * 0.02f;
            float fdiFlow = fx.fdiInflowRate * 0.004f;

            // Purchasing power parity: a currency that has run a long way from fair
            // value is pulled back, which is why no pressure compounds forever.
            float parityPull = (100f - fx.exchangeRateIndex) * m.currencyMeanReversion;

            float pressure = rateDifferential + currentAccount + inflationDifferential
                             + qeExpansion + politicalRisk + fdiFlow + parityPull;

            // Defending costs reserves, and a doubted defence costs more for less.
            // Run the reserves down and defence stops being possible at all.
            if (policy.currencyIntervention != 0f)
            {
                // Index points per year per $100B, so $120B a quarter is $480B a year
                // and moves the currency by something a player can actually see.
                float credibilityFactor = 0.4f + state.monetary.credibility / 200f;
                float annualIntervention = policy.currencyIntervention * 4f;
                pressure += annualIntervention / 100f * m.interventionEffectPer100B * credibilityFactor;

                fx.fxReservesBillions = MathUtil.Max(0f, fx.fxReservesBillions - policy.currencyIntervention / 13f);
                // Swap lines are emergency reserves: defence fails only when both run dry.
                if (fx.fxReservesBillions + state.geopolitics.swapBackstop <= 0f && policy.currencyIntervention > 0f) pressure -= 2f;
            }

            // Every term above is an ANNUAL index-point change, applied a week at a time.
            fx.exchangeRateIndex = MathUtil.Clamp(fx.exchangeRateIndex + pressure * Weekly, 10f, 400f);
            fx.realEffectiveExchangeRate = fx.exchangeRateIndex * (1f + (m.inflationTarget - state.inflation) * 0.004f);

            float depreciation = (100f - fx.exchangeRateIndex) * 0.01f;

            float currentAccountTarget = m.netExportShare * 100f
                                         + depreciation * 8f
                                         - MathUtil.Max(0f, state.realGdpGrowth - Potential(state)) * 0.4f;
            fx.currentAccountPercentGdp = MathUtil.Approach(fx.currentAccountPercentGdp, currentAccountTarget, 0.02f);

            // GDD 6 and 10: FDI follows a weak currency and competitive labour costs,
            // and takes years to show up in employment. The lag is the honest part.
            // Unit labour cost: the wage bill's share of output against where it started.
            // Raw wage levels rise with prices and productivity forever, and read as a
            // permanent loss of competitiveness that is not happening.
            float labourShare = state.population.taxableIncomePool / MathUtil.Max(1f, state.nominalGdpBillions);
            float labourCost = state.population.baselineLabourShare <= 0f
                ? 1f : labourShare / state.population.baselineLabourShare;
            float competitivenessTarget = 50f
                                          + depreciation * m.fdiCurrencySensitivity * 60f
                                          - (labourCost - 1f) * 40f
                                          - policy.Tax(TaxKeys.ImportTariff) * 0.3f
                                          - (policy.labourRegulation - 40f) * 0.15f;
            fx.manufacturingCompetitiveness = MathUtil.Clamp(
                MathUtil.Approach(fx.manufacturingCompetitiveness, competitivenessTarget, 0.01f), 0f, 100f);

            float fdiTarget = MathUtil.Max(0f, (fx.manufacturingCompetitiveness - 45f) * 6f);
            fx.fdiInflowRate = MathUtil.Approach(fx.fdiInflowRate, fdiTarget, 0.006f);

            EconomicSector manufacturing = state.GetSector(SectorId.Manufacturing);
            if (manufacturing != null)
                manufacturing.health = MathUtil.Clamp(manufacturing.health + fx.fdiInflowRate * 0.0004f, 0f, 100f);
        }
    }
}
