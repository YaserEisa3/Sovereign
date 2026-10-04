using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 20. The scenario has to be winnable and the win has to be earned: a
    /// government that governs well gets there in decades, one that does nothing
    /// never does, and one that tries to tax and cut its way there falls first.
    /// </summary>
    public static partial class EconomyTest
    {
        static void TestProsperity(SimulationRunner runner)
        {
            ScenarioDefinition aftermath = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ProjectBuilder.PostwarScenarioAsset);
            if (!Check(aftermath != null, "prosperity: the aftermath scenario is missing")) return;

            // Governed well: taxes raised a quarter of the way to their ceilings, the
            // social contract left alone, the loan spent on infrastructure, money cheap.
            EconomyState state = Rebuild(runner, aftermath);
            int wonAt = -1;
            for (int year = 1; year <= 40 && wonAt < 0; year++)
            {
                runner.Step(Year);
                if (state.prosperity) wonAt = year;
            }

            Debug.Log("Prosperity: won in year " + wonAt + " - debt " + (state.DebtToGdp * 100f).ToString("0")
                      + "%, unemployment " + state.unemployment.ToString("0.0")
                      + "%, infra " + state.infrastructureHealth.ToString("0")
                      + ", real wages " + state.Series("realWage").Latest.ToString("0")
                      + ", approval " + state.approval.overall.ToString("0") + "%");

            Check(wonAt > 0, "prosperity: forty years of sound government never won the aftermath - "
                  + runner.Simulator.Victory.Outstanding(state));
            Check(wonAt >= 10, "prosperity: the aftermath was won in year " + wonAt + " - it is meant to be a long road");

            // Doing nothing never gets there.
            runner.Scenario = aftermath;
            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            runner.Simulator.Approval.RevoltEnabled = false;
            runner.State.events.random = new DeterministicRandom(TestSeed);
            runner.Step(40 * Year);
            Check(!runner.State.prosperity, "prosperity: a government that did nothing at all still won");

            // And the conditions have to HOLD - winning is not a moment you touch once.
            Check(runner.Simulator.Victory.Config.years >= 1f,
                  "prosperity: the victory can be claimed in under a year of good conditions");
        }

        static EconomyState Rebuild(SimulationRunner runner, ScenarioDefinition aftermath)
        {
            runner.Scenario = aftermath;
            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            runner.Simulator.Approval.RevoltEnabled = false;
            runner.State.events.random = new DeterministicRandom(TestSeed);

            foreach (TaxDefinition tax in runner.Database.Taxes)
            {
                if (tax.displayName.Contains("Credit")) continue;
                string key = PolicyBootstrap.KeyFor(tax, "SO_Tax_");
                runner.Policy.taxRates[key] = tax.defaultValue + (tax.maximumValue - tax.defaultValue) * 0.25f;
            }
            foreach (string line in SpendKeys.Infrastructure)
                if (runner.Policy.spendingBillions.ContainsKey(line))
                    runner.Policy.spendingBillions[line] = runner.Policy.spendingBillions[line] * 3f;
            runner.Policy.centralBankRate = 2.5f;

            return runner.State;
        }
    }
}
