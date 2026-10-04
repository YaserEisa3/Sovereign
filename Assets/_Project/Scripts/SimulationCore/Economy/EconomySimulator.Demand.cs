namespace Sovereign.Core
{
    /// <summary>
    /// GDP as C + I + G + NX, written as deviations from trend rather than levels:
    /// the model asks "what is pushing growth away from potential?", which keeps it
    /// stable and makes every term readable.
    /// </summary>
    public partial class EconomySimulator
    {
        void UpdateDemand(EconomyState state, PolicyState policy)
        {
            MacroConfig m = _config.macro;

            // Monetary policy works with a lag (GDD 12): what bites today is the real
            // rate set three or four quarters ago, not the one on the dashboard.
            int lagWeeks = (int)(m.monetaryLagQuarters * 13f);
            float transmittedRealRate = state.Series("realRate").Ago(lagWeeks);
            float rateGap = transmittedRealRate - m.neutralRealRate;

            float consumptionTaxBurden =
                policy.Tax(TaxKeys.IncomeMiddle) * 0.45f +
                policy.Tax(TaxKeys.IncomeLower) * 0.25f +
                policy.Tax(TaxKeys.ValueAdded) * 0.20f +
                policy.Tax(TaxKeys.Payroll) * 0.10f;
            // The rate channel, at the strength MacroParameters documents: growth points
            // per point of real rate away from neutral. It is applied ONCE, whole.
            // Splitting it across consumption and investment and then weighting each by
            // its GDP share halved it, and an eight-point rate shock produced a boom-time
            // 0.2% growth rate instead of a recession.
            float rateChannel = -rateGap * m.interestRateSensitivity;

            float consumptionDrag = (consumptionTaxBurden - 20f) * 0.1f * m.consumptionTaxSensitivity;
            float consumption = -consumptionDrag + (state.consumerConfidence - 50f) * 0.03f;

            float investmentDrag = (policy.Tax(TaxKeys.CorporateIncome) - 21f) * 0.1f * m.investmentCorporateTaxSensitivity;
            float investment = -investmentDrag + (state.businessInvestmentIndex - 50f) * 0.04f;

            // The fiscal impulse is the change in what the government has DECIDED to spend,
            // against what the economy has got used to. Measured as a share of GDP it
            // drifted on its own - a growing economy shrinks the state's share every week
            // without anyone deciding anything, and that became a permanent phantom cut.
            float budget = policy.TotalSpendingBillions;
            if (state.settledSpendingBillions < 0f) state.settledSpendingBillions = budget;

            float share = state.settledSpendingBillions / MathUtil.Max(1f, state.nominalGdpBillions);
            float change = budget / MathUtil.Max(1f, state.settledSpendingBillions) - 1f;
            float government = change * share * 100f * m.fiscalMultiplier;
            state.settledSpendingBillions = MathUtil.Approach(state.settledSpendingBillions, budget,
                                                              m.fiscalAdjustmentSpeed);

            float depreciation = (100f - state.currency.exchangeRateIndex) * 0.01f;
            float tariffDrag = policy.Tax(TaxKeys.ImportTariff) * 0.1f * m.tariffTradeSensitivity;
            float netExports = depreciation * m.exportCurrencySensitivity * 10f
                               - tariffDrag
                               + (_config.worldGrowthRate - 2f) * 0.3f
                               + (state.tradeVolumeIndex - 100f) * _config.events.tuning.tradeNetExportWeight
                               + policy.exportSubsidyBillions * 0.01f * _config.geopolitics.tuning.exportSubsidyBoostPer100B;

            // GDD 16: below 70 the infrastructure drag is measurable across everything.
            float infrastructureMultiplier = _config.infrastructure.productivityDrag.Evaluate(state.infrastructureHealth);

            // Spare capacity is cheap to use: an economy below its own potential grows
            // faster than trend until it has caught up. Without this a war's hole never
            // closed - unemployment recovered while output stayed 14% below capacity.
            // ...but only while money allows it. A central bank holding rates far above
            // neutral is deliberately stopping the recovery, and catch-up growth must
            // not quietly undo a recession the player chose to cause.
            float slack = MathUtil.Max(0f, -state.OutputGapPercent);
            float openness = MathUtil.Clamp(1f - MathUtil.Max(0f, rateGap) / MathUtil.Max(0.1f, m.slackRecoveryChokeRate), 0f, 1f);
            float recovery = slack * m.recoveryFromSlack * openness;

            // Each term is recorded as it is computed and the figure is SUMMED from the
            // record, rather than added up separately and explained afterwards: an
            // explanation that is not the arithmetic itself drifts from it eventually.
            // Trend is held out of the list because it is much the largest term and
            // barely moves - ranked, it would own a place in the top three forever.
            DriverBoard board = state.drivers;
            board.growth.Clear();

            float trend = Potential(state);
            float beforeInfrastructure = trend + recovery + rateChannel
                                         + consumption * m.consumptionShare
                                         + investment * m.investmentShare
                                         + government + netExports * 0.5f;

            // A name of one or two words and a note of half a dozen. The first version
            // of this put the reading inside the name and a textbook definition beside
            // it - "Spare capacity 12.3% below potential", "Okun's law: an economy
            // growing faster than its trend takes people on" - and it read as homework.
            // What a player needs here is what is happening NOW, not the mechanism.
            board.growthTrend = trend;
            DriverBoard.Add(board.growth, "Spare capacity", recovery,
                openness < 0.99f
                    ? "idle hands, but money too dear to use them"
                    : "idle workers are cheap to put back to work");
            DriverBoard.Add(board.growth, "Interest rates", rateChannel,
                rateChannel >= 0f ? "money is cheap right now" : "money is dear and slowing things down");
            DriverBoard.Add(board.growth, "Consumer demand", consumption * m.consumptionShare,
                consumption >= 0f ? "people are spending" : "people have stopped spending");
            DriverBoard.Add(board.growth, "Business investment", investment * m.investmentShare,
                investment >= 0f ? "firms are building" : "firms have stopped building");
            DriverBoard.Add(board.growth, "Your spending", government,
                government > 0.01f ? "you are spending more than before"
                : government < -0.01f ? "your cuts are biting"
                : "the budget has settled - only CHANGES move growth");
            DriverBoard.Add(board.growth, "Trade", netExports * 0.5f,
                netExports >= 0f ? "exports are beating imports" : "imports are beating exports");
            DriverBoard.Add(board.growth, "Infrastructure",
                (infrastructureMultiplier - 1f) * beforeInfrastructure,
                state.infrastructureHealth.ToString("0") + "/100 - "
                + (state.infrastructureHealth < 70f ? "too poor to grow through" : "carrying its weight"));

            float targetGrowth = board.growthTrend;
            for (int i = 0; i < board.growth.Count; i++) targetGrowth += board.growth[i].points;
            board.growthTarget = targetGrowth;

            // Output is sticky. An economy does not turn on a sixpence, and neither
            // should the number the player is watching.
            state.realGdpGrowth = MathUtil.Clamp(MathUtil.Approach(state.realGdpGrowth, targetGrowth, 0.06f), -14f, 14f);

            state.realGdpIndex *= 1f + state.realGdpGrowth * 0.01f * Weekly;
            state.potentialGdpIndex *= 1f + Potential(state) * 0.01f * Weekly;

            state.nominalGdpGrowth = state.realGdpGrowth + state.inflation;
            state.nominalGdpBillions *= 1f + state.nominalGdpGrowth * 0.01f * Weekly;

            UpdateConfidence(state, policy, rateGap);
            state.Series("realRate").Record(state.RealInterestRate);
        }

        void UpdateConfidence(EconomyState state, PolicyState policy, float rateGap)
        {
            MacroConfig m = _config.macro;

            // An inverted curve frightens people whether or not the recession ever
            // arrives - GDD 9.1, the market believes it and the belief matters.
            float confidenceTarget = 50f
                                     + (state.realGdpGrowth - Potential(state)) * 6f
                                     - (state.unemployment - m.naturalUnemploymentRate) * 3f
                                     - MathUtil.Max(0f, state.inflation - m.inflationTarget) * 2.5f
                                     + (state.bonds.yields.IsInverted ? -8f : 0f);

            state.consumerConfidence = MathUtil.Clamp(
                MathUtil.Approach(state.consumerConfidence, confidenceTarget, m.confidenceAdjustmentSpeed), 0f, 100f);

            float investmentTarget = 50f
                                     + (state.realGdpGrowth - Potential(state)) * 5f
                                     - rateGap * 4f
                                     - (policy.Tax(TaxKeys.CorporateIncome) - 21f) * 0.4f;

            state.businessInvestmentIndex = MathUtil.Clamp(
                MathUtil.Approach(state.businessInvestmentIndex, investmentTarget, m.confidenceAdjustmentSpeed * 0.8f), 0f, 100f);
        }
    }
}
