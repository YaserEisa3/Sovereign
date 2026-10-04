using UnityEditor;
using UnityEngine;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    public static partial class ProjectBuilder
    {
        /// <summary>Hands the dashboard the map camera, so its +, - and WORLD buttons
        /// drive the same controller the mouse wheel does.</summary>
        static void WireMapControls(MapCameraController controller)
        {
            GameObject uiRoot;
            if (!BuiltPanels.TryGetValue("UIRoot", out uiRoot) || uiRoot == null) return;

            DashboardController dashboard = uiRoot.GetComponent<DashboardController>();
            if (dashboard == null) return;

            SerializedObject s = new SerializedObject(dashboard);
            s.FindProperty("mapCamera").objectReferenceValue = controller;
            s.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The hover probe needs the dashboard to draw its tooltip into.</summary>
        static void WireMapProbe(MapHoverProbe probe)
        {
            GameObject uiRoot;
            if (!BuiltPanels.TryGetValue("UIRoot", out uiRoot) || uiRoot == null) return;

            DashboardController dashboard = uiRoot.GetComponent<DashboardController>();
            if (dashboard == null) return;

            SerializedObject s = new SerializedObject(probe);
            s.FindProperty("dashboard").objectReferenceValue = dashboard;
            s.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
