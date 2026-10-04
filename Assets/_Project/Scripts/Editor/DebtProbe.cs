using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Can the debt actually be paid off? Runs the aftermath scenario under the
    /// hardest austerity the controls allow - every tax at its maximum, every
    /// discretionary line at its minimum, rates cut to let the economy breathe - and
    /// prints what happens to the stock for thirty years.
    ///
    /// Menu: Sovereign -> Probe Debt Payoff.
    /// </summary>
    public static class DebtProbe
    {
        [MenuItem("Sovereign/Probe Debt Payoff", false, 48)]
        public static void Run()
        {
            // Batch mode starts with no scene loaded, so open it the way the tests do.
            SimulationRunner runner = null;
            UnityEngine.SceneManagement.Scene scene =
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectBuilder.ScenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SimulationRunner found = root.GetComponentInChildren<SimulationRunner>(true);
                if (found != null) { runner = found; break; }
            }
            if (runner == null)
            {
                Debug.LogError("Sovereign: no SimulationRunner in the scene.");
                return;
            }

            GameDatabase database = runner.Database;
            Report("MAXIMUM AUSTERITY", runner, database, Plan.Austerity);
            Report("LEFT ALONE", runner, database, Plan.Nothing);
            Report("DISCIPLINED REBUILD", runner, database, Plan.Rebuild);
        }

        enum Plan { Nothing, Austerity, Rebuild }

        static void Report(string title, SimulationRunner runner, GameDatabase database, Plan plan)
        {
            runner.Boot();
            // The world stays quiet except where a run is meant to end in one: a debt
            // spiral is supposed to finish in a default, and the default is an event.
            runner.Simulator.Events.RandomEventsEnabled = plan == Plan.Nothing;
            runner.Simulator.Approval.RevoltEnabled = true;
            EconomyState state = runner.State;

            if (plan == Plan.Rebuild)
            {
                // A government that means to last: tax rises that stop well short of
                // punitive, the social contract left intact, and the loan spent on the
                // infrastructure the country has to grow through.
                foreach (TaxDefinition tax in database.Taxes)
                {
                    if (tax.displayName.Contains("Credit")) continue;
                    string key = PolicyBootstrap.KeyFor(tax, "SO_Tax_");
                    float headroom = tax.maximumValue - tax.defaultValue;
                    runner.Policy.taxRates[key] = tax.defaultValue + headroom * 0.25f;
                }
                foreach (string line in SpendKeys.Infrastructure)
                    if (runner.Policy.spendingBillions.ContainsKey(line))
                        runner.Policy.spendingBillions[line] = runner.Policy.spendingBillions[line] * 3f;
                runner.Policy.centralBankRate = 2.5f;
            }

            if (plan == Plan.Austerity)
            {
                foreach (TaxDefinition tax in database.Taxes)
                {
                    string key = PolicyBootstrap.KeyFor(tax, "SO_Tax_");
                    // A child credit is money going OUT, so austerity sets it to zero.
                    runner.Policy.taxRates[key] = tax.displayName.Contains("Credit") ? tax.minimumValue : tax.maximumValue;
                }
                foreach (SpendingCategoryDefinition line in database.SpendingCategories)
                {
                    if (line.displayName == "Debt Service") continue;
                    string key = PolicyBootstrap.KeyFor(line, "SO_Spend_");
                    if (runner.Policy.spendingBillions.ContainsKey(key)) runner.Policy.spendingBillions[key] = line.minimumBillions;
                }
                runner.Policy.centralBankRate = 2f;
            }

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.Append("Sovereign debt probe - ").Append(title).Append('\n');
            report.Append("year   debt $B   debt/GDP   coupon     3M     5Y    30Y   service $B   balance %   unemp   approval\n");

            for (int year = 0; year <= 30; year++)
            {
                if (year > 0) runner.Step(52);
                if (year % 5 != 0 && year != 1) continue;

                report.Append(year.ToString().PadLeft(4))
                      .Append(state.bonds.debtStockBillions.ToString("#,0").PadLeft(10))
                      .Append((state.DebtToGdp * 100f).ToString("0").PadLeft(11))
                      .Append(state.bonds.averageCoupon.ToString("0.00").PadLeft(9))
                      .Append(state.bonds.yields.shortTermYield.ToString("0.00").PadLeft(7))
                      .Append(state.bonds.yields.mediumTermYield.ToString("0.00").PadLeft(7))
                      .Append(state.bonds.yields.longTermYield.ToString("0.00").PadLeft(7))
                      .Append(state.bonds.AnnualDebtServiceBillions.ToString("#,0").PadLeft(13))
                      .Append(state.budgetBalancePercentGdp.ToString("+0.0;-0.0").PadLeft(12))
                      .Append(state.unemployment.ToString("0.0").PadLeft(8))
                      .Append(state.approval.overall.ToString("0").PadLeft(10))
                      .Append('\n');
            }

            Debug.Log(report.ToString());
        }
    }
}
