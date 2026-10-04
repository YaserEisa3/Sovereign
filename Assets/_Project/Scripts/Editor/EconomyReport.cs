using System.Text;
using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Prints what the economy actually does, year by year. Tests check directions;
    /// this is for reading the magnitudes and deciding whether they feel like an
    /// economy or like a spreadsheet having a bad day.
    ///
    /// Menu: Sovereign -> Print Economy Report.
    /// </summary>
    public static class EconomyReport
    {
        const int Year = 52;

        [MenuItem("Sovereign/Print Economy Report", false, 44)]
        public static void RunFromMenu() { Report(); }

        public static void Run()
        {
            Report();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Report()
        {
            SimulationRunner runner = EconomyTestAccess.LoadRunner();
            if (runner == null) { Debug.LogError("Sovereign: no SimulationRunner in the scene."); return; }

            StringBuilder report = new StringBuilder();

            report.AppendLine("=== BASELINE: policy left alone ===");
            report.AppendLine("year  growth  infl  core  unemp  debt/gdp  short  long  cur   conf  infra  rating");
            Quiet(runner);
            for (int year = 1; year <= 10; year++)
            {
                runner.Step(Year);
                AppendRow(report, year, runner.State);
            }

            report.AppendLine();
            report.AppendLine("=== PEOPLE: baseline, policy left alone ===");
            report.AppendLine("year   pop   youth  work  retd   dep  births deaths  labour  employed  pool$B  vets  vetB  appr  poor  mid  rich  left  cent  right unrest");
            Quiet(runner);
            AppendPeopleRow(report, 0, runner.State);
            for (int year = 1; year <= 10; year++)
            {
                runner.Step(Year);
                AppendPeopleRow(report, year, runner.State);
            }

            report.AppendLine();
            report.AppendLine("=== VOLCKER: policy rate held at 11% ===");
            report.AppendLine("year  growth  infl  core  unemp  debt/gdp  short  long  cur   conf  infra  rating");
            Quiet(runner);
            runner.Policy.centralBankRate = 11f;
            for (int year = 1; year <= 8; year++)
            {
                runner.Step(Year);
                AppendRow(report, year, runner.State);
            }

            report.AppendLine();
            report.AppendLine("=== SPENDING SPREE: every line up 90% ===");
            report.AppendLine("year  growth  infl  core  unemp  debt/gdp  short  long  cur   conf  infra  rating");
            Quiet(runner);
            System.Collections.Generic.List<string> keys =
                new System.Collections.Generic.List<string>(runner.Policy.spendingBillions.Keys);
            foreach (string key in keys) runner.Policy.spendingBillions[key] *= 1.9f;
            for (int year = 1; year <= 12; year++)
            {
                runner.Step(Year);
                AppendRow(report, year, runner.State);
            }

            Debug.Log(report.ToString());
        }

        /// <summary>The economy report is about the economy: the world stays quiet and
        /// the government cannot fall, so the tables show policy and nothing else.
        /// Sovereign -> Print Event Report is the one with the world switched on.</summary>
        static void Quiet(SimulationRunner runner)
        {
            runner.Scenario = UnityEditor.AssetDatabase.LoadAssetAtPath<Sovereign.Data.ScenarioDefinition>(ProjectBuilder.PeacetimeScenarioAsset);

            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            runner.Simulator.Geopolitics.SignatureMovesEnabled = false;
            runner.Simulator.Approval.RevoltEnabled = false;
        }

        static void AppendPeopleRow(StringBuilder report, int year, EconomyState s)
        {
            PopulationState p = s.population;
            ApprovalState a = s.approval;
            float veteranSpend;
            s.treasury.spendingByLine.TryGetValue(SpendKeys.VeteransBenefits, out veteranSpend);
            report.AppendLine(string.Format(
                "{0,4} {1,6:0.0} {2,5:0.0} {3,5:0.0} {4,5:0.0} {5,5:0.00} {6,6:0.00} {7,6:0.00} {8,7:0.0} {9,9:0.0} {10,7:0} {11,5:0.0} {12,5:0} {13,5:0} {14,5:0} {15,4:0} {16,5:0} {17,5:0} {18,5:0} {19,5:0} {20,6:0}",
                year, p.Total, p.youth, p.workingAge, p.retired, p.DependencyRatio, p.birthsPerYear, p.deathsPerYear,
                p.laborForce, p.employed, p.taxableIncomePool, p.veterans, veteranSpend,
                a.overall, a.poor, a.middle, a.wealthy, a.left, a.centre, a.right, a.HighestUnrest));
        }

        static void AppendRow(StringBuilder report, int year, EconomyState s)
        {
            report.AppendLine(string.Format(
                "{0,4}  {1,6:0.0}  {2,4:0.0}  {3,4:0.0}  {4,5:0.0}  {5,8:0.0}  {6,5:0.0}  {7,4:0.0}  {8,4:0}  {9,4:0}  {10,5:0}  {11}",
                year, s.realGdpGrowth, s.inflation, s.coreInflation, s.unemployment, s.DebtToGdp * 100f,
                s.bonds.yields.shortTermYield, s.bonds.yields.longTermYield, s.currency.exchangeRateIndex,
                s.consumerConfidence, s.infrastructureHealth, s.bonds.creditRating));
        }
    }
}
