using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Phase 3's scene wiring: the event engine on its Phase 0 manager object,
    /// and the World Events and Advisor drawers made real.</summary>
    public static partial class ProjectBuilder
    {
        static void WireEventEngine(Transform managers, SimulationRunner runner, GameDatabase database, Prefabs prefabs)
        {
            Transform engineObject = managers.Find("EventEngine");
            if (engineObject == null) return;

            SerializedObject s = new SerializedObject(engineObject.gameObject.AddComponent<EventEngine>());
            s.FindProperty("runner").objectReferenceValue = runner;
            s.FindProperty("database").objectReferenceValue = database;
            s.FindProperty("warZoneLayer").objectReferenceValue = Layer("WarZoneLayer");
            s.FindProperty("disasterLayer").objectReferenceValue = Layer("DisasterLayer");
            s.FindProperty("unrestLayer").objectReferenceValue = Layer("UnrestLayer");
            s.FindProperty("fdiLayer").objectReferenceValue = Layer("FDILayer");
            s.FindProperty("factoryIconPrefab").objectReferenceValue = prefabs.factoryIcon;

            SerializedProperty nations = s.FindProperty("nationMarkers");
            nations.arraySize = BuiltNations == null ? 0 : BuiltNations.Length;
            for (int i = 0; i < nations.arraySize; i++) nations.GetArrayElementAtIndex(i).objectReferenceValue = BuiltNations[i];

            s.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform Layer(string name)
        {
            Transform layer;
            return BuiltLayers.TryGetValue(name, out layer) ? layer : null;
        }

        static void WireEventDrawers(SimulationRunner runner, GameDatabase database, VisualTreeAsset policyRow)
        {
            VisualTreeAsset eventRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/EventAlertRow.uxml");
            VisualTreeAsset nationRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/NationStatusRow.uxml");

            GameObject worldEvents;
            if (BuiltPanels.TryGetValue("Panel_WorldEvents", out worldEvents))
            {
                SerializedObject s = WireDrawerBase(worldEvents.AddComponent<WorldEventsPanelController>(), runner, database, policyRow);
                s.FindProperty("eventAlertRowTemplate").objectReferenceValue = eventRow;
                s.FindProperty("nationStatusRowTemplate").objectReferenceValue = nationRow;
                s.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject advisor;
            if (BuiltPanels.TryGetValue("Panel_Advisor", out advisor))
            {
                SerializedObject s = WireDrawerBase(advisor.AddComponent<AdvisorPanelController>(), runner, database, policyRow);
                s.FindProperty("eventAlertRowTemplate").objectReferenceValue = eventRow;
                s.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
