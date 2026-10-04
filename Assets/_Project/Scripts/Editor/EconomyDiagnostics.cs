using System.Text;
using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Traces a squeeze-then-relent run year by year with the terms that drive
    /// growth, so a recession that refuses to end can be pinned to a cause instead
    /// of guessed at. Menu: Sovereign -> Diagnose Recovery.
    /// </summary>
    public static class EconomyDiagnostics
    {
        [MenuItem("Sovereign/Diagnose Recovery", false, 46)]
        public static void RunFromMenu() { Trace(); }

        public static void Run()
        {
            Trace();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Trace()
        {
            SimulationRunner runner = EconomyTestAccess.LoadRunner();
            runner.Scenario = UnityEditor.AssetDatabase.LoadAssetAtPath<Sovereign.Data.ScenarioDefinition>(ProjectBuilder.PeacetimeScenarioAsset);

            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            runner.Simulator.Geopolitics.SignatureMovesEnabled = false;
            EconomyState s = runner.State;

            StringBuilder r = new StringBuilder();
            r.AppendLine("=== RECOVERY TRACE: 11% for 4 years, then 3% ===");
            r.AppendLine("yr rate growth  infl  expct unemp realR  lagR  conf  invI  govSh  potnl  lfGr  infra debt  long rating appr");

            for (int year = 1; year <= 10; year++)
            {
                runner.Policy.centralBankRate = year <= 4 ? 11f : 3f;
                runner.Step(52);

                float lagged = s.Series("realRate").Ago(runner.Simulator.Config.macro.monetaryLagQuarters * 13);
                float govShare = runner.Policy.TotalSpendingBillions / Mathf.Max(1f, s.nominalGdpBillions);
                r.AppendLine(string.Format(
                    "{0,2} {1,4:0.0} {2,6:0.0} {3,5:0.0} {4,6:0.0} {5,5:0.0} {6,5:0.0} {7,5:0.0} {8,5:0} {9,5:0} {10,6:0.000} {11,6:0.00} {12,5:0.00} {13,5:0} {14,5:0} {15,5:0.0} {16,5} {17,4:0}",
                    year, runner.Policy.centralBankRate, s.realGdpGrowth, s.inflation, s.monetary.inflationExpectations,
                    s.unemployment, s.RealInterestRate, lagged, s.consumerConfidence, s.businessInvestmentIndex, govShare,
                    runner.Simulator.Potential(s), s.population.laborForceGrowth, s.infrastructureHealth,
                    s.DebtToGdp * 100f, s.bonds.yields.longTermYield, s.bonds.creditRating, s.approval.overall));
            }
            r.AppendLine("unrest poor " + s.approval.unrestPoor.ToString("0") + " middle " + s.approval.unrestMiddle.ToString("0")
                         + " wealthy " + s.approval.unrestWealthy.ToString("0") + " | poor " + s.approval.poor.ToString("0")
                         + " middle " + s.approval.middle.ToString("0") + " wealthy " + s.approval.wealthy.ToString("0"));
            r.AppendLine(s.IsGameOver ? "GAME OVER at week " + s.week + ": " + s.approval.revoltReason : "still governing");
            Debug.Log(r.ToString());
        }
    }
}
