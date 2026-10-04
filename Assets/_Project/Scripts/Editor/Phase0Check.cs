using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Headless verification of the Phase 0 shell, in the spirit of Rocky Road's
    /// smoke test: open the scene, check that every object the GDD asks for is
    /// really there, and that GameDatabase has no empty slots.
    ///
    /// Menu: Sovereign -> Verify Phase 0. Headless:
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod Sovereign.EditorTools.Phase0Check.Run
    /// </summary>
    public static partial class Phase0Check
    {
        static readonly List<string> Failures = new List<string>();
        static int _checks;

        [MenuItem("Sovereign/Verify Phase 0", false, 40)]
        public static void RunFromMenu()
        {
            Verify();
        }

        public static void Run()
        {
            bool passed = Verify();
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        static bool Verify()
        {
            Failures.Clear();
            _checks = 0;

            Scene scene = EditorSceneManager.OpenScene(ProjectBuilder.ScenePath, OpenSceneMode.Single);
            Dictionary<string, Transform> roots = new Dictionary<string, Transform>();
            foreach (GameObject go in scene.GetRootGameObjects()) roots[go.name] = go.transform;

            CheckRoots(roots);
            CheckManagers(roots);
            CheckDatabase(roots);
            CheckMap(roots);
            CheckUI(roots);
            CheckCamera(roots);
            CheckAssetCounts();

            StringBuilder report = new StringBuilder();
            report.AppendLine("Sovereign Phase 0 check: " + (_checks - Failures.Count) + "/" + _checks + " passed.");
            foreach (string failure in Failures) report.AppendLine("  FAIL " + failure);

            if (Failures.Count == 0) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
            return Failures.Count == 0;
        }

        static void Check(bool condition, string description)
        {
            _checks++;
            if (!condition) Failures.Add(description);
        }

        static void CheckRoots(Dictionary<string, Transform> roots)
        {
            foreach (string divider in new[]
            {
                "--- MANAGERS ---", "--- DATA ---", "--- WORLD MAP ---",
                "--- UI ---", "--- CAMERAS ---", "--- AUDIO ---"
            })
                Check(roots.ContainsKey(divider), "missing labelled parent " + divider);
        }

        static void CheckManagers(Dictionary<string, Transform> roots)
        {
            Transform managers;
            if (!roots.TryGetValue("--- MANAGERS ---", out managers)) return;
            foreach (string manager in new[]
            {
                "GameBootstrap", "SimulationRunner", "PolicyController", "TreasuryController",
                "PopulationController", "EventEngine", "GeopoliticsController", "AdvisorController",
                "NewsGenerator", "SaveLoadManager", "SteamManager", "AudioManager"
            })
                Check(managers.Find(manager) != null, "missing manager object " + manager);

            // Phase 1: the clock, and the one reference it needs.
            Transform runner = managers.Find("SimulationRunner");
            if (runner == null) return;

            Sovereign.Presentation.SimulationRunner clock = runner.GetComponent<Sovereign.Presentation.SimulationRunner>();
            Check(clock != null, "SimulationRunner object has no SimulationRunner component");
            if (clock == null) return;

            SerializedObject serialised = new SerializedObject(clock);
            Check(serialised.FindProperty("database").objectReferenceValue != null,
                  "SimulationRunner has no GameDatabase assigned - it cannot boot the economy");
        }

        static void CheckDatabase(Dictionary<string, Transform> roots)
        {
            Transform data;
            if (!roots.TryGetValue("--- DATA ---", out data)) return;

            Transform databaseTransform = data.Find("GameDatabase");
            Check(databaseTransform != null, "missing GameDatabase object");
            if (databaseTransform == null) return;

            GameDatabase database = databaseTransform.GetComponent<GameDatabase>();
            Check(database != null, "GameDatabase object has no GameDatabase component");
            if (database == null) return;

            Check(database.Validate(false), "GameDatabase has empty slots or shares that do not sum to 1");
            Check(database.PlayerNation != null, "no NationDefinition is marked isPlayerNation");
        }
    }
}
