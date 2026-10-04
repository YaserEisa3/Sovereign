using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 3.3 and 3.7. One scene GameObject per panel, each with a UIDocument
    /// pointing at a real .uxml you can open in the UI Builder. No VisualElement is
    /// ever constructed in C# - Phase 4 controllers query these documents by name.
    ///
    /// The drawers are created inactive: the dashboard is what you see on Play, and
    /// the panel controllers show their own drawer. Tick one active in the Inspector
    /// to look at it.
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string UiPath = Root + "/UI/";

        struct PanelSpec
        {
            public string objectName, document;
            public int sortingOrder;
            public bool startsActive;

            public PanelSpec(string objectName, string document, int sortingOrder, bool startsActive)
            {
                this.objectName = objectName; this.document = document;
                this.sortingOrder = sortingOrder; this.startsActive = startsActive;
            }
        }

        static readonly PanelSpec[] Panels =
        {
            new PanelSpec("UIRoot", "Dashboard", 0, true),
            new PanelSpec("Panel_Fiscal", "PanelFiscal", 10, false),
            new PanelSpec("Panel_MonetaryBonds", "PanelMonetaryBonds", 11, false),
            new PanelSpec("Panel_TradeCurrency", "PanelTradeCurrency", 12, false),
            new PanelSpec("Panel_Regulatory", "PanelRegulatory", 13, false),
            new PanelSpec("Panel_Population", "PanelPopulation", 14, false),
            new PanelSpec("Panel_WorldEvents", "PanelWorldEvents", 15, false),
            new PanelSpec("Panel_Advisor", "PanelAdvisor", 16, false),
            new PanelSpec("Panel_EventPopup", "PanelEventPopup", 20, false),
        };

        static readonly System.Collections.Generic.Dictionary<string, GameObject> BuiltPanels =
            new System.Collections.Generic.Dictionary<string, GameObject>();

        static void BuildUI()
        {
            PanelSettings settings = BuildPanelSettings();
            GameObject uiParent = new GameObject("--- UI ---");
            BuiltPanels.Clear();

            foreach (PanelSpec spec in Panels)
            {
                GameObject panel = Child(spec.objectName, uiParent.transform);
                UIDocument document = panel.AddComponent<UIDocument>();
                document.panelSettings = settings;
                document.sortingOrder = spec.sortingOrder;

                string uxmlPath = UiPath + "Documents/" + spec.document + ".uxml";
                VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                if (tree == null) Debug.LogError("Sovereign: missing UXML at " + uxmlPath);
                document.visualTreeAsset = tree;

                panel.SetActive(spec.startsActive);
                BuiltPanels[spec.objectName] = panel;
            }
        }

        static PanelSettings BuildPanelSettings()
        {
            string path = UiPath + "SovereignPanelSettings.asset";
            PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (existing != null) return existing;

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiPath + "Styles/UnityDefaultRuntimeTheme.tss");
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            AssetDatabase.CreateAsset(settings, path);
            return settings;
        }
    }
}
