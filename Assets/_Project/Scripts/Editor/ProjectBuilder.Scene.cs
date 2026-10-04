using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 3.3. The scene, built once and then saved to disk as real objects you
    /// can open, select and drag. The managers are empty GameObjects on purpose -
    /// Phase 0 says the shell exists before the behaviour does, and Phases 1-5
    /// attach their scripts to these exact objects.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static readonly string[] ManagerNames =
        {
            "GameBootstrap", "SimulationRunner", "PolicyController", "TreasuryController",
            "PopulationController", "EventEngine", "GeopoliticsController", "AdvisorController",
            "NewsGenerator", "SaveLoadManager", "SteamManager", "AudioManager"
        };

        static GameObject Child(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static SimulationRunner _pendingRunner;

        static void BuildScene(Prefabs prefabs)
        {
            if (System.IO.File.Exists(ScenePath) && !_overwrite)
            {
                Debug.Log("Sovereign: scene already exists at " + ScenePath
                          + " - left untouched so hand-placed nations survive. Use the rebuild menu item to regenerate it.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject managers = new GameObject("--- MANAGERS ---");
            foreach (string manager in ManagerNames) Child(manager, managers.transform);

            GameObject data = new GameObject("--- DATA ---");
            GameObject databaseObject = Child("GameDatabase", data.transform);
            GameDatabase database = databaseObject.AddComponent<GameDatabase>();
            WireDatabase(database);

            // Phase 1: the clock goes on the manager object that has existed since
            // Phase 0, with its GameDatabase reference assigned as if dragged.
            Transform runnerTransform = managers.transform.Find("SimulationRunner");
            SimulationRunner runner = runnerTransform.gameObject.AddComponent<SimulationRunner>();
            SerializedObject runnerObject = new SerializedObject(runner);
            runnerObject.FindProperty("database").objectReferenceValue = database;
            // The game opens on the war's aftermath; the tests boot the peacetime asset.
            runnerObject.FindProperty("scenario").objectReferenceValue = _postwarScenario;
            runnerObject.ApplyModifiedPropertiesWithoutUndo();

            // BuildUI runs below and records the panels, so the dashboard is wired after it.
            _pendingRunner = runner;

            BuildMap(prefabs);
            BuildUI();
            WireDashboard(_pendingRunner);
            WireDrawers(_pendingRunner, database);
            WireEventEngine(managers.transform, _pendingRunner, database, prefabs);
            WireNationViews(_pendingRunner);
            WireTradeRoutes(_pendingRunner, prefabs);
            WireCityscapes(_pendingRunner);

            GameObject cameras = new GameObject("--- CAMERAS ---");
            GameObject cameraObject = Child("MainCamera", cameras.transform);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 21f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _theme.background;
            camera.transform.position = new Vector3(0f, 0f, -20f);
            // Render inside the dashboard map viewport, not behind the whole
            // screen - see ProjectBuilder.MapCamera.cs.
            camera.rect = MapCameraRect;
            cameraObject.AddComponent<AudioListener>();

            // Zoom and pan, driven only while the pointer is inside the map viewport.
            MapCameraController mapController = cameraObject.AddComponent<MapCameraController>();
            SerializedObject cameraSerialised = new SerializedObject(mapController);
            cameraSerialised.FindProperty("mapCamera").objectReferenceValue = camera;
            cameraSerialised.ApplyModifiedPropertiesWithoutUndo();
            WireMapControls(mapController);

            MapHoverProbe probe = cameraObject.AddComponent<MapHoverProbe>();
            SerializedObject probeSerialised = new SerializedObject(probe);
            probeSerialised.FindProperty("mapCamera").objectReferenceValue = camera;
            probeSerialised.ApplyModifiedPropertiesWithoutUndo();
            WireMapProbe(probe);

            GameObject audio = new GameObject("--- AUDIO ---");
            Child("Audio_Ambient", audio.transform).AddComponent<AudioSource>().playOnAwake = false;
            Child("Audio_UI", audio.transform).AddComponent<AudioSource>().playOnAwake = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
        }

        /// <summary>
        /// Assigns every asset into GameDatabase's private [SerializeField] slots,
        /// exactly as dragging them in the Inspector would.
        /// </summary>
        static void WireDatabase(GameDatabase database)
        {
            SerializedObject so = new SerializedObject(database);
            so.FindProperty("macro").objectReferenceValue = _macro;
            so.FindProperty("population").objectReferenceValue = _population;
            so.FindProperty("war").objectReferenceValue = _war;
            so.FindProperty("infrastructure").objectReferenceValue = _infrastructure;
            so.FindProperty("approvalWeights").objectReferenceValue = _approval;
            so.FindProperty("uiTheme").objectReferenceValue = _theme;
            so.FindProperty("eventParameters").objectReferenceValue = _eventParameters;
            so.FindProperty("geopoliticsParameters").objectReferenceValue = _geopoliticsParameters;
            so.FindProperty("achievementParameters").objectReferenceValue = _achievementParameters;
            so.FindProperty("headlineLibrary").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/News/Headlines.txt");
            FillArray(so, "sectors", _sectors);
            FillArray(so, "nations", _nations);
            FillArray(so, "taxes", _taxes);
            FillArray(so, "spendingCategories", _spending);
            FillArray(so, "events", _events);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void FillArray(SerializedObject so, string propertyName, Object[] assets)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            property.arraySize = assets.Length;
            for (int i = 0; i < assets.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        }

        static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
