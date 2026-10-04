using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Is the scenario actually playable? The debt probe runs a quiet world; this one
    /// runs the world the player gets - wars, pandemics, oil shocks, creditors with
    /// opinions - across several seeds, under sound policy, and reports what happened
    /// to each run.
    ///
    /// Menu: Sovereign -> Probe Survival.
    /// </summary>
    public static class SurvivalProbe
    {
        [MenuItem("Sovereign/Probe Survival", false, 49)]
        public static void Run()
        {
            SimulationRunner runner = null;
            UnityEngine.SceneManagement.Scene scene =
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectBuilder.ScenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SimulationRunner found = root.GetComponentInChildren<SimulationRunner>(true);
                if (found != null) { runner = found; break; }
            }
            if (runner == null) { Debug.LogError("Sovereign: no SimulationRunner in the scene."); return; }

            ScenarioDefinition aftermath = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ProjectBuilder.PostwarScenarioAsset);
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.Append("Sovereign survival probe - sound policy, live world, 30 years each\n");
            report.Append("seed   outcome        year   debt/GDP   unemp   approval   what happened\n");

            int survived = 0, won = 0;
            for (uint seed = 1; seed <= 6; seed++)
            {
                runner.Scenario = aftermath;
                runner.Boot();
                runner.State.events.random = new DeterministicRandom(seed);
                EconomyState state = runner.State;

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

                int fellAt = -1, wonAt = -1;
                for (int year = 1; year <= 30; year++)
                {
                    runner.Step(52);
                    if (state.prosperity && wonAt < 0) wonAt = year;
                    if (state.IsGameOver) { fellAt = year; break; }
                }

                if (fellAt < 0) survived++;
                if (wonAt > 0) won++;

                report.Append(seed.ToString().PadLeft(4))
                      .Append((fellAt > 0 ? "   FELL" : wonAt > 0 ? "   WON" : "   survived").PadRight(15))
                      .Append((fellAt > 0 ? fellAt : wonAt > 0 ? wonAt : 30).ToString().PadLeft(5))
                      .Append((state.DebtToGdp * 100f).ToString("0").PadLeft(11))
                      .Append(state.unemployment.ToString("0.0").PadLeft(8))
                      .Append(state.approval.overall.ToString("0").PadLeft(11))
                      .Append("   ")
                      .Append(fellAt > 0 ? state.approval.revoltReason : "")
                      .Append('\n');
            }

            report.Append("\n").Append(survived).Append(" of 6 survived, ").Append(won).Append(" of 6 won.");
            Debug.Log(report.ToString());
        }
    }
}
