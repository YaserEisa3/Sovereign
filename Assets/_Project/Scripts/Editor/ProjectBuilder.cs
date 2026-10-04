using UnityEditor;
using UnityEngine;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Phase 0 authoring tool (GDD 20). Builds the folder structure, every
    /// ScriptableObject asset, the map prefabs and the SovereignMain scene, then
    /// wires GameDatabase in the Inspector.
    ///
    /// This is an AUTHORING tool, not a runtime generator: it runs once in the
    /// Editor and leaves behind real assets and a real saved scene you can open,
    /// inspect and drag around. Nothing here executes at play time.
    ///
    /// It is deliberately NON-DESTRUCTIVE. An asset that already exists is left
    /// alone, so re-running it never eats a coefficient you hand-tuned. Use
    /// "Rebuild Data Assets (overwrites)" when you do want the document's values
    /// stamped back over your edits.
    /// </summary>
    public static partial class ProjectBuilder
    {
        public const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/SovereignMain.unity";

        static bool _overwrite;

        // Everything built this run, so the scene step can wire it up.
        static MacroParameters _macro;
        static PopulationParameters _population;
        static WarParameters _war;
        static InfrastructureParameters _infrastructure;
        static ApprovalWeights _approval;
        static EventParameters _eventParameters;
        static GeopoliticsParameters _geopoliticsParameters;
        static AchievementParameters _achievementParameters;
        static ScenarioDefinition _postwarScenario;
        static MeterParameters _meterParameters;
        static UITheme _theme;
        static SectorDefinition[] _sectors;
        static NationDefinition[] _nations;
        static TaxDefinition[] _taxes;
        static SpendingCategoryDefinition[] _spending;
        static EventDefinition[] _events;

        [MenuItem("Sovereign/Build Phase 0 Project", false, 0)]
        public static void BuildAll()
        {
            Build(false);
        }

        [MenuItem("Sovereign/Rebuild Data Assets (overwrites hand-tuned values)", false, 20)]
        public static void RebuildData()
        {
            if (!EditorUtility.DisplayDialog("Rebuild data assets?",
                    "This stamps the GDD's values back over every ScriptableObject asset, "
                    + "discarding coefficients you have tuned by hand. The scene and prefabs are kept.",
                    "Overwrite", "Cancel")) return;
            Build(true);
        }

        /// <summary>Batch-mode entry point for a full overwrite rebuild - same as the
        /// rebuild menu item without the confirmation dialog.</summary>
        public static void RebuildAllHeadless()
        {
            Build(true);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Build(bool overwrite)
        {
            _overwrite = overwrite;
            try
            {
                AssetDatabase.StartAssetEditing();
                EnsureFolders();
                _theme = BuildTheme();
                _macro = BuildMacroParameters();
                _population = BuildPopulationParameters();
                _war = BuildWarParameters();
                _infrastructure = BuildInfrastructureParameters();
                _approval = BuildApprovalWeights();
                _eventParameters = BuildEventParameters();
                _geopoliticsParameters = BuildGeopoliticsParameters();
                _achievementParameters = BuildAchievementParameters();
                _postwarScenario = BuildPostwarScenario();
                _meterParameters = BuildMeterParameters();
                _cityscapeParameters = BuildCityscapeParameters();
                BuildPeacetimeScenario();
                _sectors = BuildSectors();
                _nations = BuildNations();
                _taxes = BuildTaxes();
                _spending = BuildSpendingCategories();
                _events = BuildEvents();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Prefabs prefabs = BuildPrefabs();
            BuildBuildingPrefabs();
            FinishEvents(prefabs);
            BuildScene(prefabs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Sovereign Phase 0 built: " + _sectors.Length + " sectors, " + _nations.Length
                      + " nations, " + _taxes.Length + " taxes, " + _spending.Length + " spending lines, "
                      + _events.Length + " events. Scene at " + ScenePath);
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            foreach (string top in new[] { "Scenes", "Scripts", "Data", "Prefabs", "UI", "Materials", "Audio" })
                EnsureFolder(Root, top);

            foreach (string s in new[] { "SimulationCore", "Presentation", "Data", "Editor" })
                EnsureFolder(Root + "/Scripts", s);
            foreach (string s in new[] { "Economy", "Population", "Treasury", "Geopolitics", "Events", "Policy" })
                EnsureFolder(Root + "/Scripts/SimulationCore", s);
            foreach (string s in new[] { "Managers", "Map", "UI", "Charts" })
                EnsureFolder(Root + "/Scripts/Presentation", s);

            foreach (string s in new[] { "Sectors", "Nations", "Events", "Taxes", "Spending", "Parameters", "Scenarios" })
                EnsureFolder(Root + "/Data", s);
            foreach (string s in new[] { "Map", "UI" })
                EnsureFolder(Root + "/Prefabs", s);
            foreach (string s in new[] { "Documents", "Templates", "Styles" })
                EnsureFolder(Root + "/UI", s);
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        /// <summary>
        /// Loads the asset at <paramref name="path"/> if it exists, otherwise creates it.
        /// Existing assets are only re-stamped when the overwrite menu item was used.
        /// </summary>
        static T Asset<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                configure(asset);
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            if (_overwrite)
            {
                configure(asset);
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        static AnimationCurve Curve(params Keyframe[] keys)
        {
            AnimationCurve c = new AnimationCurve(keys);
            for (int i = 0; i < c.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            return c;
        }
    }
}
