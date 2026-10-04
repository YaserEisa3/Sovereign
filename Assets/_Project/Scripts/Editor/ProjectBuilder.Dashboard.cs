using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Attaches DashboardController to UIRoot and assigns its references, exactly as
    /// dragging them in the Inspector would. Without this the dashboard renders its
    /// authored placeholders and never learns the simulation exists.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static void WireDashboard(SimulationRunner runner)
        {
            GameObject uiRoot;
            if (!BuiltPanels.TryGetValue("UIRoot", out uiRoot) || uiRoot == null)
            {
                Debug.LogError("Sovereign: UIRoot was not built, so the dashboard cannot be wired.");
                return;
            }

            DashboardController controller = uiRoot.AddComponent<DashboardController>();
            SerializedObject serialised = new SerializedObject(controller);

            serialised.FindProperty("runner").objectReferenceValue = runner;
            serialised.FindProperty("theme").objectReferenceValue = _theme;
            serialised.FindProperty("kpiCardTemplate").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/KPICard.uxml");
            serialised.FindProperty("meterRowTemplate").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/MeterRow.uxml");
            serialised.FindProperty("meterParameters").objectReferenceValue = _meterParameters;
            serialised.FindProperty("breakdownRowTemplate").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/BreakdownRow.uxml");
            serialised.FindProperty("approvalBarTemplate").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/ApprovalBar.uxml");
            serialised.FindProperty("newsTickerItemTemplate").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/NewsTickerItem.uxml");

            AssignPanel(serialised, "panelFiscal", "Panel_Fiscal");
            AssignPanel(serialised, "panelMonetaryBonds", "Panel_MonetaryBonds");
            AssignPanel(serialised, "panelTradeCurrency", "Panel_TradeCurrency");
            AssignPanel(serialised, "panelRegulatory", "Panel_Regulatory");
            AssignPanel(serialised, "panelPopulation", "Panel_Population");
            AssignPanel(serialised, "panelWorldEvents", "Panel_WorldEvents");
            AssignPanel(serialised, "panelAdvisor", "Panel_Advisor");

            // Phase 5: saving lives on the manager object Phase 0 left for it.
            Transform saveObject = runner.transform.parent.Find("SaveLoadManager");
            if (saveObject != null)
            {
                SaveLoadManager saveLoad = saveObject.gameObject.AddComponent<SaveLoadManager>();
                SerializedObject saveSerialised = new SerializedObject(saveLoad);
                saveSerialised.FindProperty("runner").objectReferenceValue = runner;
                saveSerialised.ApplyModifiedPropertiesWithoutUndo();
                serialised.FindProperty("saveLoad").objectReferenceValue = saveLoad;
            }

            Transform steamObject = runner.transform.parent.Find("SteamManager");
            if (steamObject != null)
            {
                SerializedObject steam = new SerializedObject(steamObject.gameObject.AddComponent<SteamManager>());
                steam.FindProperty("runner").objectReferenceValue = runner;
                steam.ApplyModifiedPropertiesWithoutUndo();
            }

            serialised.ApplyModifiedPropertiesWithoutUndo();

            // The popup starts inactive, so whatever listens for the ending has to live
            // somewhere that is always awake - the dashboard.
            GameObject popup;
            if (BuiltPanels.TryGetValue("Panel_EventPopup", out popup))
            {
                SerializedObject gameOver = new SerializedObject(uiRoot.AddComponent<GameOverController>());
                gameOver.FindProperty("runner").objectReferenceValue = runner;
                gameOver.FindProperty("popupPanel").objectReferenceValue = popup;
                gameOver.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void AssignPanel(SerializedObject serialised, string field, string panelName)
        {
            GameObject panel;
            if (!BuiltPanels.TryGetValue(panelName, out panel)) return;
            serialised.FindProperty(field).objectReferenceValue = panel;
        }
    }
}
