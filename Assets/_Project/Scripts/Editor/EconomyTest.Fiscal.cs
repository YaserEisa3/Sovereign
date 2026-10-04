using System.Collections.Generic;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Infrastructure, debt, the currency, and the policy queue itself.</summary>
    public static partial class EconomyTest
    {
        /// <summary>GDD 16. The lever players under-fund first and regret most.</summary>
        static void TestInfrastructureNeglect(SimulationRunner runner)
        {
            EconomyState neglected = Fresh(runner).State;
            for (int i = 0; i < SpendKeys.Infrastructure.Length; i++)
                runner.Policy.spendingBillions[SpendKeys.Infrastructure[i]] = 0f;
            runner.Step(10 * Year);
            float neglectedHealth = neglected.infrastructureHealth;
            float neglectedGrowth = neglected.Series("gdpGrowth").Average(Year);

            EconomyState maintained = Fresh(runner).State;
            for (int i = 0; i < SpendKeys.Infrastructure.Length; i++)
                runner.Policy.spendingBillions[SpendKeys.Infrastructure[i]] = 60f;
            runner.Step(10 * Year);

            Check(neglectedHealth < maintained.infrastructureHealth - 10f,
                  "infrastructure: ten years of zero spending barely dented health ("
                  + neglectedHealth.ToString("0") + " vs " + maintained.infrastructureHealth.ToString("0") + ")");
            Check(neglectedHealth < runner.Simulator.Config.infrastructure.dragThreshold,
                  "infrastructure: neglect never reached the drag threshold, so the GDP penalty never bites");
            Check(neglectedGrowth < maintained.Series("gdpGrowth").Average(Year),
                  "infrastructure: crumbling infrastructure cost nothing in growth");
        }

        /// <summary>GDD 9.5 and 21. Debt dynamics are non-linear and the market prices them.</summary>
        static void TestDebtSpiral(SimulationRunner runner)
        {
            EconomyState state = Fresh(runner).State;
            float startingDebt = state.DebtToGdp;
            float startingYield = state.bonds.yields.longTermYield;

            List<string> keys = new List<string>(runner.Policy.spendingBillions.Keys);
            foreach (string key in keys) runner.Policy.spendingBillions[key] *= 1.9f;

            runner.Step(12 * Year);

            CheckFinite(state, "debt spiral");
            Check(state.DebtToGdp > startingDebt,
                  "debt spiral: doubling spending for twelve years did not raise debt/GDP");
            Check(state.bonds.creditRating > CreditRating.AA,
                  "debt spiral: the rating never moved below AA (" + state.bonds.creditRating + ")");
            Check(state.bonds.yields.longTermYield > startingYield,
                  "debt spiral: the bond market never charged more for the extra risk");
            Check(state.bonds.riskPremium > 0.5f,
                  "debt spiral: risk premium stayed near zero at debt/GDP "
                  + (state.DebtToGdp * 100f).ToString("0") + "%");
        }

        /// <summary>GDD 6 and 10. A weak currency is a double-edged sword and both
        /// edges have to be real.</summary>
        static void TestWeakCurrency(SimulationRunner runner)
        {
            EconomyState state = Fresh(runner).State;
            float startingCompetitiveness = state.currency.manufacturingCompetitiveness;

            for (int week = 0; week < 8 * Year; week++)
            {
                runner.Policy.currencyIntervention = -120f;
                runner.Step();
            }

            CheckFinite(state, "weak currency");
            Check(state.currency.exchangeRateIndex < 100f,
                  "weak currency: eight years of selling did not weaken it ("
                  + state.currency.exchangeRateIndex.ToString("0.0") + ")");
            Check(state.currency.manufacturingCompetitiveness > startingCompetitiveness,
                  "weak currency: competitiveness did not improve - the upside is missing");
            Check(state.currency.fdiInflowRate > 0f,
                  "weak currency: no FDI followed the cheaper currency");
            Check(state.inflation > runner.Simulator.Config.startingInflation,
                  "weak currency: import inflation never arrived - the downside is missing");
        }

        /// <summary>GDD 4. Policy is queued and lands at the quarter boundary, never
        /// mid-quarter. Without this the whole lag design is decorative.</summary>
        static void TestPolicyIsQueued(SimulationRunner runner)
        {
            Fresh(runner);
            float before = runner.Policy.Tax(TaxKeys.CorporateIncome);
            runner.Policy.QueueTax(TaxKeys.CorporateIncome, before + 10f);

            runner.Step(5);
            Check(runner.Policy.Tax(TaxKeys.CorporateIncome) == before,
                  "queue: a queued tax change took effect mid-quarter");
            Check(runner.Policy.PendingChangeCount == 1, "queue: the pending change was not tracked");

            runner.Step(2 * 13);
            Check(runner.Policy.Tax(TaxKeys.CorporateIncome) == before + 10f,
                  "queue: the queued change never landed at a quarter boundary");
            Check(runner.Policy.PendingChangeCount == 0, "queue: the queue was not cleared after committing");
        }
    }
}
