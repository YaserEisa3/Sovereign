using System.Collections.Generic;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 17.3, 17.6 and 19: war, the gates on crises, default, the dice, the advisor.</summary>
    public static partial class EconomyTest
    {
        /// <summary>Outspend the enemy and the war is cheap; underspend and it is not.</summary>
        static void TestWar(SimulationRunner runner)
        {
            EconomyState lean = Fresh(runner).State;
            runner.State.events.random = new DeterministicRandom(7u);
            float approvalBefore = lean.approval.overall;
            runner.Simulator.Events.Spawn(lean, runner.Policy, "InvasionThreat", true);
            Check(lean.events.war.active, "war: an invasion did not start a war");
            runner.Step(1);  // overall approval is recomputed on the weekly tick
            Check(lean.approval.overall > approvalBefore + 2f, "war: no rally round the flag");

            runner.Step(26);
            float leanKia = lean.events.war.kia;
            float veteransAfter = lean.population.veterans;
            Check(ForcedDefence(runner) >= 900f - 0.5f, "war: the wartime minimum defence spend was not enforced");

            EconomyState heavy = Fresh(runner).State;
            runner.State.events.random = new DeterministicRandom(7u);
            foreach (string key in new[] { "MilitaryPersonnel", "MilitaryEquipment", "MilitaryOperations", "MilitaryRnD" })
                runner.Policy.spendingBillions[key] = runner.Policy.Spending(key) * 2.5f;
            runner.Simulator.Events.Spawn(heavy, runner.Policy, "InvasionThreat", true);
            runner.Step(26);

            Check(heavy.events.war.kia < leanKia * 0.5f,
                  "war: outspending the enemy 2.5x did not halve casualties (" + heavy.events.war.kia.ToString("0")
                  + " vs " + leanKia.ToString("0") + ")");
            Check(heavy.events.war.lastReport.Contains("KIA"), "war: no plain-numbers war report");
            Check(veteransAfter > 0f, "war: veteran count lost");
        }

        static float ForcedDefence(SimulationRunner runner)
        {
            PolicyState p = runner.Policy;
            return p.Spending("MilitaryPersonnel") + p.Spending("MilitaryEquipment") + p.Spending("MilitaryRnD")
                   + p.Spending("MilitaryOperations") + p.Spending("HomelandSecurity");
        }

        /// <summary>GDD 17.6: crises need their causes.</summary>
        static void TestEventConditions(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            EventSystem events = runner.Simulator.Events;
            EventConfig hyper = events.Config.events[events.IndexOf("HyperinflationSpiral")];
            EventConfig banking = events.Config.events[events.IndexOf("BankingCrisis")];
            EventConfig debt = events.Config.events[events.IndexOf("DebtCrisis")];

            Check(!events.ConditionsHold(s, hyper), "conditions: hyperinflation could fire in a calm opening economy");
            Check(!events.ConditionsHold(s, debt), "conditions: a debt crisis could fire at 112% debt");
            Check(events.ConditionsHold(s, banking), "conditions: a banking crisis is impossible even with default capital rules");

            runner.Policy.bankingCapitalRequirement = 14f;
            runner.Step(1);
            Check(!events.ConditionsHold(s, banking), "conditions: raising capital requirements did not rule out a banking crisis");
        }

        /// <summary>GDD 17.6 and 14: a debt crisis above the danger line ends in default.</summary>
        static void TestDefault(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            s.bonds.debtStockBillions = s.nominalGdpBillions * 2.6f;
            s.bonds.creditRating = CreditRating.B;
            runner.Simulator.Events.Spawn(s, runner.Policy, "DebtCrisis", true);
            runner.Step(3 * 13 + 1);
            Check(s.IsGameOver && s.approval.revoltReason.StartsWith("Sovereign default"),
                  "default: a debt crisis at 260% of GDP never ended in default (cover "
                  + s.bonds.lastAuctionCover.ToString("0.00") + ")");
            Check(s.bonds.creditRating == CreditRating.D, "default: the rating did not go to D");
        }

        /// <summary>The dice: a run has a handful of events, and a seed replays exactly.</summary>
        static void TestRandomEvents(SimulationRunner runner)
        {
            EconomyState first = FreshWithEvents(runner, 424242u).State;
            runner.Step(10 * Year);
            List<string> firstHistory = Keys(first);
            int count = first.events.history.Count + first.events.live.Count;
            Check(count >= 5 && count <= 60, "random events: " + count + " events in ten years - the world is either silent or on fire");

            EconomyState second = FreshWithEvents(runner, 424242u).State;
            runner.Step(10 * Year);
            Check(string.Join(",", Keys(second).ToArray()) == string.Join(",", firstHistory.ToArray()),
                  "random events: the same seed produced a different history - runs are not replayable");

            EconomyState quiet = Fresh(runner).State;
            runner.Step(10 * Year);
            Check(quiet.events.history.Count == 0 && quiet.events.live.Count == 0,
                  "random events: the world acted while random events were switched off");
        }

        static List<string> Keys(EconomyState s)
        {
            List<string> keys = new List<string>();
            foreach (EventRecord e in s.events.history) keys.Add(e.key + "@" + e.startWeek);
            return keys;
        }

        /// <summary>GDD 19: warnings fire on the way into a condition, not every week.</summary>
        static void TestAdvisor(SimulationRunner runner)
        {
            // Regression: the curve used to open at 0% and read as inverted for weeks.
            EconomyState opening = Fresh(runner).State;
            Check(!opening.bonds.yields.IsInverted && opening.bonds.yields.longTermYield > 3f,
                  "advisor: a new run opens with an inverted or empty yield curve");
            runner.Step(8);
            Check(!HasAlert(opening, AlertChannel.Advisor, "inverted"),
                  "advisor: a new run raised a yield curve alarm in its first two months");

            EconomyState s = Fresh(runner).State;
            runner.Policy.centralBankRate = 9f;
            runner.Step(Year);
            int inversions = 0;
            foreach (Alert a in s.events.alerts) if (a.text.Contains("yield curve has inverted")) inversions++;
            Check(s.bonds.yields.IsInverted ? inversions == 1 : inversions <= 1,
                  "advisor: the inversion warning fired " + inversions + " times instead of once");
        }
    }
}
