using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Attaches the Fiscal and Monetary drawer controllers to the panel
    /// objects that have existed since Phase 0, and assigns their references.</summary>
    public static partial class ProjectBuilder
    {
        static void WireDrawers(SimulationRunner runner, GameDatabase database)
        {
            VisualTreeAsset policyRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/PolicyRow.uxml");
            VisualTreeAsset maturityRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/MaturityRow.uxml");
            VisualTreeAsset nationRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/NationStatusRow.uxml");

            GameObject fiscal;
            if (BuiltPanels.TryGetValue("Panel_Fiscal", out fiscal))
            {
                SerializedObject s = WireDrawerBase(fiscal.AddComponent<FiscalPanelController>(), runner, database, policyRow);
                s.FindProperty("breakdownRowTemplate").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/BreakdownRow.uxml");
                s.ApplyModifiedPropertiesWithoutUndo();
            }

            // Phase 2: the population drawer is real now.
            GameObject population;
            if (BuiltPanels.TryGetValue("Panel_Population", out population))
            {
                SerializedObject s = WireDrawerBase(population.AddComponent<PopulationPanelController>(), runner, database, policyRow);
                s.FindProperty("ageBandRowTemplate").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/AgeBandRow.uxml");
                s.FindProperty("sectorRowTemplate").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/SectorRow.uxml");
                s.FindProperty("approvalBarTemplate").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/ApprovalBar.uxml");
                s.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject monetary;
            if (BuiltPanels.TryGetValue("Panel_MonetaryBonds", out monetary))
            {
                SerializedObject s = WireDrawerBase(monetary.AddComponent<MonetaryBondsPanelController>(), runner, database, policyRow);
                s.FindProperty("maturityRowTemplate").objectReferenceValue = maturityRow;
                s.FindProperty("nationStatusRowTemplate").objectReferenceValue = nationRow;
                s.ApplyModifiedPropertiesWithoutUndo();
            }

            WireTradeAndRegulation(runner, database, policyRow);
            // Phase 3: World Events and the Advisor are real now.
            WireEventDrawers(runner, database, policyRow);
        }

        static SerializedObject WireDrawerBase(DrawerPanelController controller, SimulationRunner runner,
                                               GameDatabase database, VisualTreeAsset policyRow)
        {
            SerializedObject s = new SerializedObject(controller);
            s.FindProperty("runner").objectReferenceValue = runner;
            s.FindProperty("database").objectReferenceValue = database;
            s.FindProperty("theme").objectReferenceValue = _theme;
            s.FindProperty("policyRowTemplate").objectReferenceValue = policyRow;
            return s;
        }
    }
}
