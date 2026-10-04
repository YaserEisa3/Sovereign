using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Renders the world map camera to a PNG so the map can be checked without
    /// squinting at the Game view - and so a headless run can prove the map draws
    /// at all, rather than assuming it.
    ///
    /// Menu: Sovereign -> Capture Map Preview (writes next to the project).
    /// Headless: -executeMethod Sovereign.EditorTools.MapPreview.ApplyAndCapture
    /// with SOVEREIGN_CAPTURE set to the output path.
    /// </summary>
    public static class MapPreview
    {
        const int Width = 845;
        const int Height = 971;

        [MenuItem("Sovereign/Capture Map Preview", false, 42)]
        public static void CaptureFromMenu()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "MapPreview.png");
            Capture(path);
            Debug.Log("Sovereign: map preview written to " + path);
        }

        /// <summary>Fits the camera to the dashboard viewport, saves the scene, then
        /// captures the map full-frame so the contents can be inspected.</summary>
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene(ProjectBuilder.ScenePath, OpenSceneMode.Single);
            ProjectBuilder.FitMapCamera();
            EditorSceneManager.SaveOpenScenes();

            string path = Environment.GetEnvironmentVariable("SOVEREIGN_CAPTURE");
            if (string.IsNullOrEmpty(path))
                path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "MapPreview.png");

            Capture(path);

            // Second pass: the real viewport rect at screen size, to check placement.
            string placement = path.Replace(".png", "-placement.png");
            CaptureAtSize(placement, 1827, 1045, false);

            Camera camera = Camera.main;
            Debug.Log("Sovereign: camera rect is now " + camera.rect + ", ortho size " + camera.orthographicSize
                      + ", clear " + camera.clearFlags + ", culling " + camera.cullingMask
                      + ". Preview written to " + path);

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        static void Capture(string path) { CaptureAtSize(path, Width, Height, true); }

        /// <summary>
        /// fullFrame true ignores the camera's viewport rect and captures the whole
        /// map, for checking what it contains. fullFrame false keeps the rect and
        /// captures at screen size, for checking WHERE on screen the map lands.
        /// </summary>
        static void CaptureAtSize(string path, int width, int height, bool fullFrame)
        {
            Camera camera = Camera.main;
            if (camera == null) { Debug.LogError("Sovereign: no MainCamera in the open scene."); return; }

            Rect savedRect = camera.rect;
            if (fullFrame) camera.rect = new Rect(0f, 0f, 1f, 1f);

            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = target;

            // With a partial viewport rect nothing clears the rest of the texture,
            // so start from a known colour rather than whatever memory held.
            RenderTexture.active = target;
            GL.Clear(true, true, Color.black);

            camera.Render();

            RenderTexture.active = target;
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();

            RenderTexture.active = previousActive;
            camera.targetTexture = null;
            camera.rect = savedRect;

            File.WriteAllBytes(path, image.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
