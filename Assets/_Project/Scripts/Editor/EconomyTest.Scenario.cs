using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 3.5. The opening the game actually starts from: a country that has just
    /// come out of a war on a reconstruction loan. It has to be as bad as it claims,
    /// and it has to be survivable - a scenario nobody can hold together for a year
    /// is not a game.
    /// </summary>
    public static partial class EconomyTest
    {
        static void TestAftermathScenario(SimulationRunner runner)
        {
            ScenarioDefinition aftermath = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ProjectBuilder.PostwarScenarioAsset);
            if (!Check(aftermath != null, "scenario: SO_Scenario_Aftermath is missing")) return;

            runner.Scenario = aftermath;
            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            runner.Simulator.Approval.RevoltEnabled = false;
            runner.State.events.random = new DeterministicRandom(TestSeed);
            EconomyState s = runner.State;
            Debug.Log("Aftermath opening: GDP " + s.nominalGdpBillions.ToString("0") + "B, revenue "
                      + (s.revenueBillions / s.nominalGdpBillions * 100f).ToString("0.0") + "% of GDP, spending "
                      + (s.spendingBillions / s.nominalGdpBillions * 100f).ToString("0.0") + "%, deficit "
                      + (-s.budgetBalancePercentGdp).ToString("0.0") + "% of GDP, debt service "
                      + s.bonds.AnnualDebtServiceBillions.ToString("0") + "B");

            Check(s.unemployment >= 12f, "scenario: the aftermath opens at " + s.unemployment.ToString("0.0") + "% unemployment, which is not a shattered labour market");
            Check(s.DebtToGdp >= 1.25f, "scenario: opening debt is " + (s.DebtToGdp * 100f).ToString("0") + "% of GDP, not a reconstruction loan");
            Check(s.infrastructureHealth <= 40f, "scenario: infrastructure opens at " + s.infrastructureHealth.ToString("0") + ", which is not war damage");
            Check(s.OutputGap <= -8f, "scenario: the output gap opens at " + s.OutputGap.ToString("0.0") + ", so nothing is idle");
            Check(s.events.productivityScar > 0f, "scenario: the war left no productivity scar");
            Check(s.bonds.foreignHoldingPercent >= 0.4f, "scenario: the loan is not mostly foreign-held");
            Check(s.events.alerts.Count >= 3, "scenario: the opening briefing was not posted to the ticker");

            int hostile = -1;
            NationProfile[] nations = runner.Simulator.Geopolitics.Config.nations;
            for (int i = 0; i < nations.Length; i++) if (nations[i].name == aftermath.conditions.hostileNation) hostile = i;
            if (Check(hostile >= 0, "scenario: the hostile nation named by the scenario is not in the world"))
            {
                NationState enemy = s.geopolitics.nations[hostile];
                Check(enemy.relationship <= -50f, "scenario: the nation you just fought opens at " + enemy.relationship.ToString("0") + " relationship");
                Check(enemy.sanctioningYou, "scenario: the nation you just fought is not sanctioning you");
            }

            // Ten years of leaving the loan to do the work by itself: the debt compounds
            // and nothing is rebuilt. The country has to still be standing, but worse.
            float openingDebt = s.DebtToGdp;
            float openingInfrastructure = s.infrastructureHealth;
            runner.Step(10 * Year);
            Debug.Log("Aftermath, hands off, year 10: gap " + s.OutputGapPercent.ToString("0.0")
                      + "%, unemployment " + s.unemployment.ToString("0.0")
                      + "%, inflation " + s.inflation.ToString("0.0")
                      + "%, debt " + (s.DebtToGdp * 100f).ToString("0")
                      + "%, infra " + s.infrastructureHealth.ToString("0")
                      + ", rating " + s.bonds.creditRating);
            Check(s.DebtToGdp > openingDebt, "scenario: doing nothing with a reconstruction loan left debt no worse");
            Check(s.infrastructureHealth <= openingInfrastructure + 5f, "scenario: the ruins repaired themselves");
            CheckFinite(s, "scenario");

            // Now play it: spend the loan on the infrastructure the briefing points at.
            // The scenario has to be winnable, or it is a cutscene rather than a game.
            runner.Scenario = aftermath;
            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            runner.Simulator.Approval.RevoltEnabled = false;
            runner.State.events.random = new DeterministicRandom(TestSeed);
            s = runner.State;
            float rebuildGap = s.OutputGapPercent;
            foreach (string line in SpendKeys.Infrastructure)
                runner.Policy.spendingBillions[line] = runner.Policy.spendingBillions[line] * 3f;
            // And stop strangling the recovery: 8% money against a slump is a choice.
            runner.Policy.centralBankRate = 2.5f;
            runner.Step(10 * Year);
            Debug.Log("Aftermath, rebuilding, year 10: gap " + s.OutputGapPercent.ToString("0.0")
                      + "%, unemployment " + s.unemployment.ToString("0.0")
                      + "%, inflation " + s.inflation.ToString("0.0")
                      + "%, debt " + (s.DebtToGdp * 100f).ToString("0")
                      + "%, infra " + s.infrastructureHealth.ToString("0")
                      + ", approval " + s.approval.overall.ToString("0")
                      + ", rating " + s.bonds.creditRating);
            Check(s.infrastructureHealth > openingInfrastructure + 15f,
                  "scenario: ten years of triple infrastructure spending only reached " + s.infrastructureHealth.ToString("0") + " health");
            Check(s.OutputGapPercent > rebuildGap,
                  "scenario: rebuilding did not close any of the output gap (" + rebuildGap.ToString("0.0") + "% to " + s.OutputGapPercent.ToString("0.0") + "%)");
            Check(s.unemployment < 12f, "scenario: rebuilding left unemployment at " + s.unemployment.ToString("0.0") + "%");
            CheckFinite(s, "scenario");

            // With politics live and no help from the player, the government should not
            // fall in the opening months - the country has just been through a war.
            runner.Scenario = aftermath;
            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            runner.State.events.random = new DeterministicRandom(TestSeed);
            runner.Step(Year / 2);
            Debug.Log("Aftermath after six months: approval " + runner.State.approval.overall.ToString("0")
                      + ", unrest(poor) " + runner.State.approval.unrestPoor.ToString("0")
                      + ", revolt " + runner.State.IsGameOver);
            Check(!runner.State.IsGameOver, "scenario: the government fell within six months of the war ending, before the player could act");
        }
    }
}
