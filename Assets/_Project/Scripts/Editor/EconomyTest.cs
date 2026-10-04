using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Headless economy tests. A hundred coefficients interacting can look plausible
    /// for a year and drift to nonsense by year eight, so these run whole decades and
    /// check DIRECTIONS rather than exact numbers - a tighter policy should raise
    /// unemployment and lower inflation, whatever the precise magnitudes end up being.
    ///
    /// Menu: Sovereign -> Run Economy Tests. Headless:
    ///   -executeMethod Sovereign.EditorTools.EconomyTest.Run
    /// </summary>
    public static partial class EconomyTest
    {
        const int Year = 52;
        const uint TestSeed = 20270101u;

        static readonly List<string> Failures = new List<string>();
        static int _checks;

        [MenuItem("Sovereign/Run Economy Tests", false, 43)]
        public static void RunFromMenu() { Verify(); }

        public static void Run()
        {
            bool passed = Verify();
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        static bool Verify()
        {
            Failures.Clear();
            _checks = 0;

            SimulationRunner runner = LoadRunner();
            if (runner == null)
            {
                Debug.LogError("Sovereign: no SimulationRunner in the scene - run Build Phase 0 Project first.");
                return false;
            }

            TestBaselineStability(runner);
            TestVolckerMoment(runner);
            TestInflationTrap(runner);
            TestInfrastructureNeglect(runner);
            TestDebtSpiral(runner);
            TestWeakCurrency(runner);
            TestPolicyIsQueued(runner);
            TestNoFreeDrift(runner);
            TestRecessionIsSevereEnough(runner);
            TestNoDeflationSpiral(runner);
            TestRecoveryIsPossible(runner);
            TestTreasury(runner);
            TestPopulation(runner);
            TestWagesKeepUp(runner);
            TestChildCredit(runner);
            TestImmigration(runner);
            TestApproval(runner);
            TestRevolt(runner);
            TestEventStages(runner);
            TestEventImpacts(runner);
            TestPreparation(runner);
            TestWar(runner);
            TestEventConditions(runner);
            TestDefault(runner);
            TestRandomEvents(runner);
            TestAdvisor(runner);
            TestDiplomacy(runner);
            TestBondWeapon(runner);
            TestNationMoves(runner);
            TestSaveLoad(runner);
            TestAchievements(runner);
            TestAftermathScenario(runner);
            TestBuyback(runner);
            TestProsperity(runner);

            StringBuilder report = new StringBuilder();
            report.AppendLine("Sovereign economy tests: " + (_checks - Failures.Count) + "/" + _checks + " passed.");
            foreach (string failure in Failures) report.AppendLine("  FAIL " + failure);

            if (Failures.Count == 0) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
            return Failures.Count == 0;
        }

        static SimulationRunner LoadRunner()
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectBuilder.ScenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SimulationRunner found = root.GetComponentInChildren<SimulationRunner>(true);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// A fresh run with revolt switched OFF. Most of these tests check economic
        /// mechanics under years of deliberate ruin, and a government falling halfway
        /// through would freeze the economy they are measuring.
        /// </summary>
        /// <summary>The calm GDD 18 opening. The game starts from the war's aftermath,
        /// but the model is measured from a steady state, or every test would be
        /// reading a recovery.</summary>
        static void Peacetime(SimulationRunner runner)
        {
            runner.Scenario = UnityEditor.AssetDatabase.LoadAssetAtPath<Sovereign.Data.ScenarioDefinition>(
                ProjectBuilder.PeacetimeScenarioAsset);
        }

        static SimulationRunner Fresh(SimulationRunner runner)
        {
            Peacetime(runner);
            runner.Boot();
            runner.Simulator.Approval.RevoltEnabled = false;
            runner.Simulator.Events.RandomEventsEnabled = false;
            // A fixed seed: events spawned by hand roll severity and duration from these
            // dice, and a clock-seeded roll made the suite pass or fail by luck.
            runner.State.events.random = new DeterministicRandom(TestSeed);
            runner.Simulator.Geopolitics.SignatureMovesEnabled = false;
            return runner;
        }

        /// <summary>A fresh run where the government CAN fall - for testing that it does.
        /// The world stays quiet so the only thing that can topple it is policy.</summary>
        static SimulationRunner FreshWithPolitics(SimulationRunner runner)
        {
            Peacetime(runner);
            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            // A fixed seed: events spawned by hand roll severity and duration from these
            // dice, and a clock-seeded roll made the suite pass or fail by luck.
            runner.State.events.random = new DeterministicRandom(TestSeed);
            runner.Simulator.Geopolitics.SignatureMovesEnabled = false;
            return runner;
        }

        /// <summary>A fresh run with the world switched on, on a fixed seed so the
        /// history it rolls is the same every time the test runs.</summary>
        static SimulationRunner FreshWithEvents(SimulationRunner runner, uint seed)
        {
            Peacetime(runner);
            runner.Boot();
            runner.Simulator.Approval.RevoltEnabled = false;
            runner.State.events.random = new DeterministicRandom(seed);
            return runner;
        }

        static bool Check(bool condition, string description)
        {
            _checks++;
            if (!condition) Failures.Add(description);
            return condition;
        }

        static void CheckFinite(EconomyState state, string label)
        {
            bool finite = MathUtil.IsFinite(state.realGdpGrowth)
                          && MathUtil.IsFinite(state.inflation)
                          && MathUtil.IsFinite(state.unemployment)
                          && MathUtil.IsFinite(state.nominalGdpBillions)
                          && MathUtil.IsFinite(state.DebtToGdp)
                          && MathUtil.IsFinite(state.bonds.yields.longTermYield)
                          && MathUtil.IsFinite(state.currency.exchangeRateIndex);
            Check(finite, label + ": produced a NaN or an infinity");
        }
    }
}
