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

            // FOREVER MODE: prosperity is a state, not a trophy. Reach it, let the
            // country go, and it must be lost again - a latching win is an ending, and
            // this scenario does not have one.
            EconomyState held = Rebuild(runner, aftermath);
            int reachedAt = -1;
            for (int year = 1; year <= 40 && reachedAt < 0; year++)
            {
                runner.Step(Year);
                if (held.prosperity) reachedAt = year;
            }
            if (Check(reachedAt > 0, "forever: never reached prosperity to test losing it"))
            {
                int milestones = MilestoneModel.Count(held);
                Check(milestones >= 6,
                      "forever: a prosperous country holds only " + milestones + " of "
                      + MilestoneModel.Ladder.Length + " milestones");

                // Stop maintaining the country and it falls apart: infrastructure decays
                // 3.5 points a year with nothing holding it up, and prosperity needs 75.
                foreach (string line in SpendKeys.Infrastructure)
                    runner.Policy.spendingBillions[line] = 0f;
                runner.Step(12 * Year);

                Check(!held.prosperity,
                      "forever: twelve years of letting the country rot and it is still prosperous");
                Check(held.longestProsperityWeeks > 0,
                      "forever: the country was prosperous but no streak was recorded");
                Check(held.timesProsperous >= 1,
                      "forever: reaching prosperity was not counted");
                Check(MilestoneModel.Count(held) < milestones,
                      "forever: milestones cannot be lost - they held at " + MilestoneModel.Count(held)
                      + " through twelve years of neglect");
            }
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
