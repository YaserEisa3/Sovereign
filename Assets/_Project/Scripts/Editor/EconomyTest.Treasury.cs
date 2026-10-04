using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 8: revenue line by line, and both automatic stabilisers.</summary>
    public static partial class EconomyTest
    {
        static void TestTreasury(SimulationRunner runner)
        {
            EconomyState state = Fresh(runner).State;
            float gdp = state.nominalGdpBillions;

            float openingShare = state.treasury.totalRevenue / gdp;
            Check(System.Math.Abs(openingShare - 0.18f) < 0.005f,
                  "treasury: opening revenue is " + (openingShare * 100f).ToString("0.0")
                  + "% of GDP - calibration did not land on the target");

            // Every line the fiscal drawer shows must exist and raise something.
            Check(state.treasury.revenueByLine.Count == runner.Simulator.Treasury.Config.taxes.Length + 1,
                  "treasury: not every tax asset (plus per-nation tariffs) produced a revenue line");
            Check(state.treasury.revenueByLine[TaxKeys.ChildCredit] < 0f,
                  "treasury: the child tax credit raised money instead of costing it");

            // The live preview has to agree in sign with what actually happens.
            float corporate = runner.Policy.Tax(TaxKeys.CorporateIncome);
            float preview = runner.Simulator.Treasury.EstimateLine(TaxKeys.CorporateIncome, corporate + 10f, state)
                            - runner.Simulator.Treasury.EstimateLine(TaxKeys.CorporateIncome, corporate, state);
            Check(preview > 0f, "treasury: previewing a 10pt corporate tax rise showed no extra revenue");

            float before = state.treasury.revenueByLine[TaxKeys.CorporateIncome];
            runner.Policy.QueueTax(TaxKeys.CorporateIncome, corporate + 10f);
            runner.Step(13);
            runner.Step(1);
            Check(state.treasury.revenueByLine[TaxKeys.CorporateIncome] > before,
                  "treasury: the queued corporate tax rise landed but raised nothing");

            // The Laffer term: at an absurd rate, the base must shrink.
            float moderate = runner.Simulator.Treasury.EstimateLine(TaxKeys.CapitalGains, 20f, state);
            float extreme = runner.Simulator.Treasury.EstimateLine(TaxKeys.CapitalGains, 40f, state);
            Check(extreme < moderate * 2f,
                  "treasury: doubling capital gains tax doubled the take - the base never shrinks");

            // GDD 8.3: a recession cuts revenue and raises benefit spending, on its own.
            EconomyState recession = Fresh(runner).State;
            float calmRevenue = recession.treasury.totalRevenue / recession.nominalGdpBillions;
            float calmBenefits = recession.treasury.spendingByLine[SpendKeys.UnemploymentInsurance];
            runner.Policy.centralBankRate = 11f;
            runner.Step(4 * Year);

            Check(recession.treasury.totalRevenue / recession.nominalGdpBillions < calmRevenue,
                  "treasury: revenue as a share of GDP did not fall in a recession - no automatic stabiliser");
            Check(recession.treasury.spendingByLine[SpendKeys.UnemploymentInsurance] > calmBenefits * 1.5f,
                  "treasury: unemployment insurance did not rise with unemployment");
        }
    }
}
