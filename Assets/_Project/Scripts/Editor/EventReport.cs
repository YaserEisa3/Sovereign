using System.Text;
using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Runs fifteen years with the world switched on, on several seeds, and prints
    /// what happened. The question it answers is the one tests cannot: does a run
    /// FEEL like a run - a handful of events, some warned, some not, none of them
    /// constant - or is the world either silent or on fire every quarter?
    /// Menu: Sovereign -> Print Event Report.
    /// </summary>
    public static class EventReport
    {
        [MenuItem("Sovereign/Print Event Report", false, 47)]
        public static void RunFromMenu() { Report(); }

        public static void Run()
        {
            Report();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Report()
        {
            SimulationRunner runner = EconomyTestAccess.LoadRunner();
            StringBuilder r = new StringBuilder();

            foreach (uint seed in new uint[] { 11u, 2027u, 90210u })
            {
                runner.Scenario = UnityEditor.AssetDatabase.LoadAssetAtPath<Sovereign.Data.ScenarioDefinition>(ProjectBuilder.PeacetimeScenarioAsset);

                runner.Boot();
                runner.State.events.random = new DeterministicRandom(seed);
                EconomyState s = runner.State;

                r.AppendLine("=== SEED " + seed + " - fifteen years, policy left alone ===");
                for (int year = 1; year <= 15 && !s.IsGameOver; year++) runner.Step(52);

                foreach (EventRecord e in s.events.history)
                    r.AppendLine(string.Format("  y{0,4:0.0}-{1,4:0.0}  {2,-28} sev {3:0.00} mit {4:0.00}  {5}",
                        e.startWeek / 52f, e.endWeek / 52f, e.title, e.severity, e.mitigation, e.outcome));
                foreach (EventInstance e in s.events.live)
                    r.AppendLine(string.Format("  y{0,4:0.0}-      {1,-28} sev {2:0.00} still {3}",
                        e.spawnWeek / 52f, runner.Simulator.Events.Config.events[e.configIndex].title, e.severity, e.stage));

                int advisor = 0, ticker = 0;
                foreach (Alert a in s.events.alerts) { if (a.channel == AlertChannel.Advisor) advisor++; else ticker++; }
                r.AppendLine("  " + s.events.history.Count + " ended, " + s.events.live.Count + " live; last alerts: "
                             + ticker + " ticker, " + advisor + " advisor");
                r.AppendLine(string.Format("  end: week {0}, growth {1:0.0}, infl {2:0.0}, unemp {3:0.0}, debt {4:0}%, rating {5}, approval {6:0}, KIA {7:#,0}, scar {8:0.00}{9}",
                    s.week, s.realGdpGrowth, s.inflation, s.unemployment, s.DebtToGdp * 100f, s.bonds.creditRating,
                    s.approval.overall, s.events.war.kia, s.events.productivityScar,
                    s.IsGameOver ? "  GAME OVER: " + s.approval.revoltReason : ""));
                r.AppendLine();
            }
            r.AppendLine("=== THE TICKER - seed 2027, first four years ===");
            runner.Scenario = UnityEditor.AssetDatabase.LoadAssetAtPath<Sovereign.Data.ScenarioDefinition>(ProjectBuilder.PeacetimeScenarioAsset);

            runner.Boot();
            runner.State.events.random = new DeterministicRandom(2027u);
            runner.Step(4 * 52);
            int shown = 0;
            foreach (Alert a in runner.State.events.alerts)
            {
                if (a.channel != AlertChannel.Ticker || shown++ > 40) continue;
                r.AppendLine(string.Format("  y{0,4:0.0}  {1}", a.week / 52f, a.text));
            }
            r.AppendLine("  (" + runner.Simulator.Headlines.TemplateCount + " templates loaded)");
            Debug.Log(r.ToString());
        }
    }
}
