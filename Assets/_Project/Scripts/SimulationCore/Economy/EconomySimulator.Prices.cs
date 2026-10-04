namespace Sovereign.Core
{
    /// <summary>
    /// The labour market, wages and prices. This is where GDD 12's inflation trap
    /// lives: once expectations unanchor, wages chase prices chase wages, and the
    /// only way out is a recession you chose.
    /// </summary>
    public partial class EconomySimulator
    {
        void UpdateLabour(EconomyState state, PolicyState policy)
        {
            MacroConfig m = _config.macro;

            // Okun as a FLOW, not a level: unemployment INTEGRATES the growth gap, so a
            // long recession keeps pushing it up instead of parking at a fixed target.
            // As a level it peaked near 7% in a six-year depression, which nobody would
            // believe - the 1980-82 episode reached 10.8%.
            float growthGap = state.realGdpGrowth - Potential(state);
            float previous = state.drivers.unemploymentAfterLastWeek;
            float shocks = previous < 0f ? 0f : state.unemployment - previous;
            state.unemployment -= growthGap * m.okunCoefficient * Weekly;

            // and hysteresis pulls it back toward the natural rate once growth returns.
            float beforeReversion = state.unemployment;
            state.unemployment = MathUtil.Clamp(
                MathUtil.Approach(state.unemployment, m.naturalUnemploymentRate, m.unemploymentReversionSpeed), 0.5f, 40f);

            // The same three movements, stated in points per year at this week's rate -
            // weekly changes are far too small to read, and a rate is what the player is
            // deciding about. Anything that moved unemployment between last week's tick
            // and this one was an event or a shock, so it is named rather than hidden in
            // a residual nobody can see.
            DriverBoard board = state.drivers;
            board.unemployment.Clear();
            DriverBoard.Add(board.unemployment,
                "Growth " + state.realGdpGrowth.ToString("0.0") + "% against potential " + Potential(state).ToString("0.0") + "%",
                -growthGap * m.okunCoefficient,
                "Okun's law: an economy growing faster than its trend takes people on, and one growing slower lets them go.");
            DriverBoard.Add(board.unemployment,
                "Pull toward the natural rate " + m.naturalUnemploymentRate.ToString("0.0") + "%",
                (state.unemployment - beforeReversion) * WeeksPerYear,
                "Matching: people and jobs find each other over time, which drags the rate back toward its floor whatever growth does.");
            if (MathUtil.Abs(shocks) > 0.0001f)
                DriverBoard.Add(board.unemployment, "Shocks and events",
                    shocks * WeeksPerYear,
                    "Wars, disasters and crises put people out of work directly, without waiting for growth to do it.");

            board.unemploymentAfterLastWeek = state.unemployment;
        }

        /// <summary>GDD 22.5. Quarterly, because wages are negotiated, not continuous.</summary>
        void UpdateWages(EconomyState state, PolicyState policy)
        {
            MacroConfig m = _config.macro;
            float passThrough = state.monetary.expectationsUnanchored
                ? m.unanchoredInflationPassThrough
                : m.baseInflationPassThrough;

            float slack = state.unemployment - m.naturalUnemploymentRate;

            // The wage Phillips curve: expected inflation, plus productivity, plus a SHARE
            // of any inflation surprise, less what slack lets employers hold back. Anchored,
            // real wages grow with productivity; hit by a surprise, they fall; unanchored,
            // expectations chase prices and the spiral of GDD 12 appears on its own.
            // Passing through 45% of ALL inflation, as this once did, cut real wages every
            // year forever and shrank the income tax base out from under the treasury.
            float productivity = MathUtil.Max(0f, Potential(state) - state.population.laborForceGrowth);
            float expected = state.monetary.inflationExpectations;
            float surprise = state.inflation - expected;
            float immigrationShift = policy.immigrationInflowMillions - state.approval.baselineImmigration;

            float totalWage = 0f, totalWeight = 0f;

            for (int i = 0; i < state.sectors.Count; i++)
            {
                EconomicSector sector = state.sectors[i];

                float demandedGrowth = expected + productivity + surprise * passThrough
                                       - slack * sector.config.unemploymentWageSensitivity
                                       - immigrationShift * sector.config.immigrationWageSuppression * 0.3f;

                // Nominal wages are rigid downward: people accept a smaller rise far
                // more readily than a cut. This keeps deflation from feeding on itself.
                if (demandedGrowth < 0f) demandedGrowth *= m.downwardWageRigidity;

                // A quarter of the annual demand, on the target path.
                sector.targetWage *= 1f + demandedGrowth * 0.01f * 0.25f;
                float moved = MathUtil.Approach(sector.averageWage, sector.targetWage, sector.config.wageStickiness);

                float maxDrift = sector.averageWage * m.maxQuarterlyWageDriftPercent * 0.01f;
                moved = MathUtil.Clamp(moved, sector.averageWage - maxDrift, sector.averageWage + maxDrift);
                sector.averageWage = MathUtil.Max(1000f, moved);

                totalWage += sector.averageWage * sector.employmentShare;
                totalWeight += sector.employmentShare;
            }

            if (totalWeight > 0f)
            {
                float weightedWage = totalWage / totalWeight;
                float baseWeighted = 0f;
                for (int i = 0; i < state.sectors.Count; i++)
                    baseWeighted += state.sectors[i].baseWage * state.sectors[i].employmentShare;
                baseWeighted /= totalWeight;
                state.averageWageIndex = baseWeighted <= 0f ? 100f : weightedWage / baseWeighted * 100f;
            }
        }

        void UpdatePrices(EconomyState state, PolicyState policy)
        {
            MacroConfig m = _config.macro;

            // Phillips, asymmetric. A tight labour market raises prices quickly; a slack
            // one lowers them only grudgingly. As a straight line, 20% unemployment
            // demanded minus-5% inflation and the model dived to the clamp.
            float labourGap = m.naturalUnemploymentRate - state.unemployment;
            float phillips = labourGap * m.phillipsCoefficient;
            if (labourGap < 0f) phillips *= m.phillipsSlackDamping;

            // Money: excess M2 growth above what real growth absorbs, with a lag, and
            // damped by credibility - GDD 9.4, each QE dollar moves less when nobody
            // believes you.
            int qeLagWeeks = (int)(m.qeInflationLagQuarters * 13f);
            float laggedMoneyGrowth = state.Series("moneySupplyGrowth").Ago(qeLagWeeks);
            float excessMoney = laggedMoneyGrowth - state.realGdpGrowth - m.inflationTarget;
            float monetary = excessMoney * m.monetaryMultiplier * 0.25f;

            // Imported: a weaker currency raises the price of everything bought abroad.
            float depreciation = (100f - state.currency.exchangeRateIndex) * 0.01f;
            float imported = depreciation * m.importedInflationSensitivity * 10f;
            float oil = (state.oilPriceIndex - 100f) * 0.01f * 1.5f;

            // Taxes that sit directly in the price: VAT, carbon, tariffs.
            float taxPush = state.geopolitics.importTariffWeighted * _config.geopolitics.tuning.tariffInflation
                            + policy.Tax(TaxKeys.ValueAdded) * 0.02f
                            + policy.Tax(TaxKeys.Carbon) * 0.002f
                            + policy.Tax(TaxKeys.ImportTariff) * 0.02f;

            float coreTarget = state.monetary.inflationExpectations + phillips + monetary + taxPush * 0.4f;
            state.coreInflation = MathUtil.Clamp(MathUtil.Approach(state.coreInflation, coreTarget, 0.08f), -8f, 200f);

            float headlineTarget = state.coreInflation + imported + oil + taxPush * 0.6f;
            state.inflation = MathUtil.Clamp(MathUtil.Approach(state.inflation, headlineTarget, 0.12f), -6f, 300f);

            UpdateExpectations(state);

            // The cumulative price level. Fixed nominal budgets erode against this.
            state.priceLevel *= 1f + state.inflation * 0.01f * Weekly;
            state.housingCostIndex *= 1f + (state.inflation * 0.01f + (2f - state.RealInterestRate) * 0.004f) * Weekly;
        }

        /// <summary>
        /// GDD 12: above the threshold for long enough, expectations unanchor and
        /// inflation becomes self-fulfilling. Re-anchoring takes sustained proof, not
        /// a single good quarter - which is exactly why the Volcker moment hurts.
        /// </summary>
        void UpdateExpectations(EconomyState state)
        {
            MacroConfig m = _config.macro;

            if (state.inflation > m.expectationsUnanchorThreshold) state.monetary.weeksAboveUnanchorThreshold++;
            else state.monetary.weeksAboveUnanchorThreshold = (int)MathUtil.Max(0f, state.monetary.weeksAboveUnanchorThreshold - 2);

            int unanchorWeeks = (int)(m.expectationsUnanchorYears * WeeksPerYear);
            if (state.monetary.weeksAboveUnanchorThreshold > unanchorWeeks) state.monetary.expectationsUnanchored = true;
            else if (state.monetary.weeksAboveUnanchorThreshold == 0 && state.monetary.credibility > 70f)
                state.monetary.expectationsUnanchored = false;

            // Anchored, expectations are pulled back toward the target by the central
            // bank's credibility. Unanchored, they simply follow whatever prices did.
            float anchorPull = state.monetary.expectationsUnanchored ? 0.02f : 0.06f * (state.monetary.credibility / 100f);
            float anchorTarget = state.monetary.expectationsUnanchored
                ? state.inflation
                : MathUtil.Lerp(state.inflation, m.inflationTarget, state.monetary.credibility / 100f);

            state.monetary.inflationExpectations =
                MathUtil.Clamp(MathUtil.Approach(state.monetary.inflationExpectations, anchorTarget, anchorPull),
                               m.expectationsFloor, 200f);
        }
    }
}
