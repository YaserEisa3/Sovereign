using System.Collections.Generic;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 17: the stages, the impacts, preparation, and the world at large.</summary>
    public static partial class EconomyTest
    {
        static void TestEventStages(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            EventSystem events = runner.Simulator.Events;
            EventInstance pandemic = events.Spawn(s, runner.Policy, "Pandemic", false);
            int alertsAtSpawn = s.events.alerts.Count;

            Check(pandemic != null && pandemic.stage == EventWarningLevel.EarlySignal,
                  "events: a telegraphed pandemic did not start as an early signal");
            Check(alertsAtSpawn > 0 && s.events.alerts[alertsAtSpawn - 1].channel == AlertChannel.Ticker,
                  "events: the early signal did not reach the ticker");

            runner.Step(4);
            Check(pandemic.stage == EventWarningLevel.Imminent, "events: the pandemic never became imminent");
            Check(HasAlert(s, AlertChannel.Advisor, "transmission"), "events: the advisor gave no warning at the imminent stage");

            runner.Step(2);
            Check(pandemic.IsActive, "events: the pandemic never struck");
            Check(HasAlert(s, AlertChannel.Ticker, "BREAKING"), "events: the strike was not announced");

            runner.Step(pandemic.durationWeeks + 1);
            Check(!s.events.live.Contains(pandemic), "events: the pandemic never ended");
            Check(s.events.history.Count == 1 && s.events.history[0].key == "Pandemic", "events: the ended pandemic is not in the history");

            // Un-telegraphed means no warning at all (GDD 17.2).
            EventInstance quake = events.Spawn(s, runner.Policy, "Earthquake", false);
            Check(quake.IsActive, "events: an earthquake came with a warning - it should be a pure shock");
        }

        static void TestEventImpacts(SimulationRunner runner)
        {
            // Measured at 13 weeks - the shortest an oil shock can last - so it is
            // always still live when checked.
            EconomyState calm = Fresh(runner).State;
            runner.Step(13);
            float calmInflation = calm.inflation, calmOil = calm.oilPriceIndex;

            EconomyState shocked = Fresh(runner).State;
            runner.Simulator.Events.Spawn(shocked, runner.Policy, "OilPriceShock", true);
            runner.Step(13);
            Check(shocked.oilPriceIndex > calmOil + 8f, "events: an oil supply shock barely moved the oil price ("
                  + shocked.oilPriceIndex.ToString("0") + " vs " + calmOil.ToString("0") + ")");
            Check(shocked.inflation > calmInflation + 0.2f, "events: an oil shock did not raise inflation");

            // It heals once it is over.
            runner.Step(4 * Year);
            Check(shocked.oilPriceIndex < 110f, "events: the oil price never came back after the shock ended");
        }

        /// <summary>GDD 17's central rule: preparation at the moment of the strike decides the damage.</summary>
        static void TestPreparation(SimulationRunner runner)
        {
            EconomyState bare = Fresh(runner).State;
            runner.Policy.spendingBillions[SpendKeys.DisasterReliefFund] = 0f;
            float bareStart = bare.infrastructureHealth;
            runner.Simulator.Events.Spawn(bare, runner.Policy, "Earthquake", true);
            runner.Step(30);
            float bareLoss = bareStart - bare.infrastructureHealth;

            EconomyState ready = Fresh(runner).State;
            runner.Policy.spendingBillions[SpendKeys.DisasterReliefFund] = 300f;
            float readyStart = ready.infrastructureHealth;
            runner.Simulator.Events.Spawn(ready, runner.Policy, "Earthquake", true);
            runner.Step(30);
            float readyLoss = readyStart - ready.infrastructureHealth;

            // Equal severity needs the same dice: compare per unit of severity.
            float bareSeverity = bare.events.history.Count > 0 ? bare.events.history[0].severity : 1f;
            float readySeverity = ready.events.history.Count > 0 ? ready.events.history[0].severity : 1f;
            Check(readyLoss / readySeverity < bareLoss / bareSeverity * 0.6f,
                  "preparation: a well-funded relief fund did not soften an earthquake (loss "
                  + readyLoss.ToString("0.0") + " vs " + bareLoss.ToString("0.0") + ")");

            // A pandemic striking an under-funded health system scars growth for good.
            EconomyState starved = Fresh(runner).State;
            runner.Policy.spendingBillions[SpendKeys.PublicHealth] = 0f;
            runner.Policy.spendingBillions[SpendKeys.Medicaid] = 150f;
            float potentialBefore = runner.Simulator.Potential(starved);
            runner.Simulator.Events.Spawn(starved, runner.Policy, "Pandemic", true);
            runner.Step(4 * Year);
            Check(starved.events.productivityScar > 0f, "preparation: an unprepared pandemic left no permanent scar");
            Check(runner.Simulator.Potential(starved) < potentialBefore, "preparation: the scar did not lower potential growth");
        }

        static bool HasAlert(EconomyState s, AlertChannel channel, string fragment)
        {
            foreach (Alert a in s.events.alerts)
                if (a.channel == channel && a.text.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
