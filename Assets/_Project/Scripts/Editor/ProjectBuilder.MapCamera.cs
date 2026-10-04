using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// The map camera has to render INSIDE the dashboard's map viewport, not behind
    /// the whole screen - the UI panel is an opaque overlay, so anything outside
    /// that transparent hole is simply covered up.
    ///
    /// This sets the camera's Viewport Rect to match .map-viewport in Sovereign.uss
    /// (a 38%-wide column, minus the header, the drawer bar and the ticker). Because
    /// PanelSettings scales the UI with screen size, a proportional rect tracks the
    /// layout across resolutions. It is a starting fit, not a law: nudge it on the
    /// Camera component and it stays nudged.
    ///
    /// Phase 5's MapController should replace this with a RenderTexture bound to the
    /// viewport element, which is exact rather than approximate.
    /// </summary>
    public static partial class ProjectBuilder
    {
        /// <summary>x, y, width, height in viewport space, origin bottom-left.</summary>
        public static readonly Rect MapCameraRect = new Rect(0f, 0.053f, 0.440f, 0.899f);

        [MenuItem("Sovereign/Fit Map Camera to Dashboard", false, 41)]
        public static void FitMapCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("Sovereign: no MainCamera in the open scene. Open SovereignMain first.");
                return;
            }

            Undo.RecordObject(camera, "Fit Map Camera");
            camera.rect = MapCameraRect;
            camera.orthographic = true;
            camera.orthographicSize = 21f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x08, 0x0c, 0x12, 0xff);
            EditorUtility.SetDirty(camera);
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);

            Debug.Log("Sovereign: map camera fitted to the dashboard viewport (" + MapCameraRect + "). Save the scene to keep it.");
        }
    }
}
