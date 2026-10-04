using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 18. The map's cityscapes: the player's country raises houses and one
    /// silhouette per sector, the rest of the world raises what it is known for.
    /// Also renders the map camera to a PNG when SOVEREIGN_MAPSHOT names a file, so
    /// the layout can be looked at rather than guessed at.
    /// </summary>
    public static partial class PlayModeSmoke
    {
        static float _worldSize;

        static void RunCityscapeStages(SimulationRunner runner)
        {
            switch (_stage)
            {
                case Stage.VerifyCityscape:
                {
                    CityscapeView home = null;
                    int foreignWithBuildings = 0;
                    foreach (CityscapeView view in Object.FindObjectsByType<CityscapeView>(FindObjectsSortMode.None))
                    {
                        if (view.gameObject.name == "Nation_Home") home = view;
                        else if (view.BuildingCount > 0) foreignWithBuildings++;
                    }

                    if (!Require(home != null, "the home nation has no CityscapeView")) return;

                    // What actually got built, by prefab family - a country that draws six
                    // identical boxes has failed the point of the feature.
                    // Buildings live under each CitySite_*/Cityscape, one town per site.
                    HashSet<string> kinds = new HashSet<string>();
                    int standing = 0;
                    float westmost = float.MaxValue, eastmost = float.MinValue;
                    foreach (Transform site in home.transform)
                    {
                        Transform cityscape = site.name.StartsWith("CitySite_") ? site.Find("Cityscape") : null;
                        if (cityscape == null) continue;
                        foreach (Transform child in cityscape)
                        {
                            if (!child.gameObject.activeSelf) continue;
                            standing++;
                            kinds.Add(child.name.Substring(0, child.name.LastIndexOf('_')));
                            westmost = Mathf.Min(westmost, child.position.x);
                            eastmost = Mathf.Max(eastmost, child.position.x);
                        }
                    }

                    Debug.Log("PlayModeSmoke: home cityscape - " + standing + " buildings of " + kinds.Count
                              + " kinds (" + string.Join(", ", new List<string>(kinds)) + "); "
                              + foreignWithBuildings + " foreign nations built too");

                    if (!Require(standing >= 8, "the home country raised only " + standing + " buildings")) return;

                    // Hovering a building must say what it is and how that sector is doing.
                    Transform sample = null;
                    foreach (Transform site in home.transform)
                    {
                        Transform cityscape = site.name.StartsWith("CitySite_") ? site.Find("Cityscape") : null;
                        if (cityscape == null || cityscape.childCount == 0) continue;
                        foreach (Transform child in cityscape)
                            if (child.gameObject.activeSelf && child.name.StartsWith("Building_Manufacturing")) sample = child;
                        if (sample != null) break;
                    }
                    if (!Require(sample != null, "no factory to hover")) return;

                    // Hover it the way a player does: through the probe, from a SCREEN point
                    // over that building. Testing the tooltip without the lookup is exactly
                    // how this shipped doing nothing the first time.
                    DashboardController dash2 = Object.FindAnyObjectByType<DashboardController>();
                    MapHoverProbe probe = Object.FindAnyObjectByType<MapHoverProbe>();
                    if (!Require(probe != null, "there is no map hover probe on the camera")) return;

                    Camera mapCamera = Object.FindAnyObjectByType<MapCameraController>().MapCamera;
                    Vector3 onScreen = mapCamera.WorldToScreenPoint(sample.position + new Vector3(0f, 0.12f, 0f));
                    Transform hit = probe.HoverAt(onScreen);
                    Debug.Log("PlayModeSmoke: hovering a factory at screen " + onScreen + " found "
                              + (hit == null ? "nothing" : hit.name) + " - tip '" + dash2.MapTipText + "'");
                    if (!Require(hit == sample, "the probe found " + (hit == null ? "nothing" : hit.name)
                                 + " under a factory's own screen position")) return;
                    if (!Require(dash2.MapTipText.Contains("Manufacturing") && dash2.MapTipText.Contains("% of GDP"),
                                 "hovering a factory did not describe it: " + dash2.MapTipText)) return;

                    // And empty ocean must clear it again.
                    Vector3 ocean = mapCamera.WorldToScreenPoint(new Vector3(-4f, -6f, 0f));
                    probe.HoverAt(ocean);
                    if (!Require(dash2.MapTipText.Length == 0, "the tooltip stayed up over empty sea")) return;
                    if (!Require(kinds.Count >= 5, "the home country raised " + kinds.Count
                                 + " kinds of building - the sectors do not look different")) return;
                    if (!Require(foreignWithBuildings >= 5, "only " + foreignWithBuildings + " foreign nations raised anything")) return;

                    // Sanity check the predicate before trusting what it says about buildings:
                    // mid-continent must be land, mid-ocean must not.
                    Debug.Log("PlayModeSmoke: land test - Kansas " + home.TestLand(new Vector3(-9.8f, 4.24f, 0f))
                              + ", mid-Atlantic " + home.TestLand(new Vector3(-4f, 2f, 0f))
                              + ", Gulf of Guinea " + home.TestLand(Vector3.zero)
                              + ", Sahara " + home.TestLand(new Vector3(1.5f, 2.5f, 0f)));

                    // Nothing may be built at sea - anywhere in the world, not just at home.
                    int wet = 0, withoutMesh = 0;
                    string offenders = "";
                    foreach (CityscapeView view in Object.FindObjectsByType<CityscapeView>(FindObjectsSortMode.None))
                    {
                        if (!view.HasLandMesh) { withoutMesh++; continue; }
                        int sea = view.BuildingsAtSea;
                        if (sea > 0) { wet += sea; offenders += view.gameObject.name + " x" + sea + " "; }
                    }
                    Debug.Log("PlayModeSmoke: buildings at sea - " + wet + " (" + offenders + "), "
                              + withoutMesh + " nations have no land mesh to check against");
                    if (!Require(withoutMesh == 0, withoutMesh + " nations were given no land mesh")) return;
                    if (!Require(wet == 0, wet + " buildings are standing in the sea: " + offenders)) return;

                    // Towns spread across the country, rather than piling under the flag.
                    Debug.Log("PlayModeSmoke: home towns - " + home.OccupiedSites + " sites occupied, spanning "
                              + (eastmost - westmost).ToString("0.0") + " world units");
                    if (!Require(home.OccupiedSites >= 3, "the country built in only " + home.OccupiedSites + " of its cities")) return;
                    if (!Require(eastmost - westmost > 3f, "every building is within "
                                 + (eastmost - westmost).ToString("0.0") + " units - the country is not spread out")) return;

                    // Bounds alone do not prove a building is VISIBLE: flat polygons with the
                    // wrong winding reported perfect bounds and drew nothing at all. Count the
                    // triangles that face the camera instead.
                    int facing = 0, meshes = 0;
                    foreach (MeshFilter filter in home.GetComponentsInChildren<MeshFilter>())
                    {
                        Mesh mesh = filter.sharedMesh;
                        if (mesh == null) continue;
                        meshes++;
                        Vector3[] v = mesh.vertices;
                        int[] t = mesh.triangles;
                        for (int i = 0; i + 2 < t.Length; i += 3)
                        {
                            Vector3 normal = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                            if (normal.z < 0f) { facing++; break; }
                        }
                    }
                    Debug.Log("PlayModeSmoke: " + facing + " of " + meshes + " building meshes face the camera");
                    if (!Require(meshes > 0 && facing == meshes, "building meshes are back-facing - they will be culled and draw nothing")) return;

                    CaptureMap();

                    MapCameraController map = Object.FindAnyObjectByType<MapCameraController>();
                    if (!Require(map != null, "the map camera has no zoom controller")) return;
                    _worldSize = map.TargetSize;
                    VisualElement dash = DashboardRoot();
                    if (!Press(dash.Q<Button>("map-zoom-in"), "the map + button")) return;
                    if (!Press(dash.Q<Button>("map-zoom-in"), "the map + button again")) return;
                    Next(Stage.PressZoom);
                    return;
                }

                case Stage.PressZoom:
                {
                    MapCameraController map = Object.FindAnyObjectByType<MapCameraController>();
                    Debug.Log("PlayModeSmoke: map zoom " + _worldSize.ToString("0.0") + " -> target "
                              + map.TargetSize.ToString("0.0") + ", camera now " + map.MapCamera.orthographicSize.ToString("0.0"));
                    if (!Require(map.TargetSize < _worldSize, "the + button did not zoom in")) return;
                    if (!Require(map.MapCamera.orthographicSize < _worldSize, "the camera never followed the zoom")) return;

                    // And it has to stop somewhere: twenty notches must not turn the map inside out.
                    for (int i = 0; i < 20; i++) map.Zoom(1f);
                    float floor = map.TargetSize;
                    map.Zoom(1f);
                    if (!Require(Mathf.Approximately(map.TargetSize, floor) && floor > 0.5f,
                                 "zoom ran past its limit to " + map.TargetSize.ToString("0.00"))) return;

                    if (!Press(DashboardRoot().Q<Button>("map-reset"), "the map WORLD button")) return;
                    Next(Stage.VerifyZoom);
                    return;
                }

                case Stage.VerifyZoom:
                {
                    MapCameraController map = Object.FindAnyObjectByType<MapCameraController>();
                    Debug.Log("PlayModeSmoke: WORLD button returned the map to " + map.TargetSize.ToString("0.0"));
                    if (!Require(Mathf.Approximately(map.TargetSize, _worldSize), "the WORLD button did not restore the whole-world view")) return;

                    // The keyboard walks the map, and cannot walk it off the world.
                    Vector3 before = map.TargetPosition;
                    map.Nudge(new Vector2(0.25f, 0f));
                    Vector3 east = map.TargetPosition;
                    map.Nudge(new Vector2(0f, 0.25f));
                    Vector3 north = map.TargetPosition;
                    for (int i = 0; i < 40; i++) map.Nudge(new Vector2(1f, 1f));
                    Vector3 wall = map.TargetPosition;
                    map.Nudge(new Vector2(1f, 1f));
                    Debug.Log("PlayModeSmoke: keyboard pan " + before + " -> east " + east + " -> north " + north
                              + ", stopped at " + wall);
                    if (!Require(east.x > before.x, "panning east did not move the map")) return;
                    if (!Require(north.y > east.y, "panning north did not move the map")) return;
                    if (!Require((map.TargetPosition - wall).sqrMagnitude < 0.0001f, "the map can be panned off the world")) return;

                    map.ResetView();
                    _stage = Stage.Done;
                    Finish(true, "");
                    return;
                }
            }
        }

        /// <summary>Renders the map camera straight to a PNG. ScreenCapture does not
        /// survive batch mode, but rendering into a texture we own does.</summary>
        static void CaptureMap()
        {
            string path = System.Environment.GetEnvironmentVariable("SOVEREIGN_MAPSHOT");
            if (string.IsNullOrEmpty(path)) return;

            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null) return;

            // Optionally frame one nation, to see the buildings rather than the world.
            string zoom = System.Environment.GetEnvironmentVariable("SOVEREIGN_MAPZOOM");
            Vector3 cameraPosition = camera.transform.position;
            float size = camera.orthographicSize;
            if (!string.IsNullOrEmpty(zoom))
            {
                GameObject target = GameObject.Find(zoom);
                if (target != null)
                {
                    camera.transform.position = new Vector3(target.transform.position.x, target.transform.position.y, cameraPosition.z);
                    string zoomSize = System.Environment.GetEnvironmentVariable("SOVEREIGN_MAPZOOMSIZE");
                    float parsed;
                    camera.orthographicSize = !string.IsNullOrEmpty(zoomSize) && float.TryParse(zoomSize, out parsed) ? parsed : 2.5f;
                }
            }

            RenderTexture texture = new RenderTexture(1400, 1400, 24);
            Rect rect = camera.rect;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            camera.targetTexture = texture;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            Texture2D image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            camera.targetTexture = null;
            camera.rect = rect;
            camera.transform.position = cameraPosition;
            camera.orthographicSize = size;

            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log("PlayModeSmoke: wrote a map render to " + path);
        }
    }
}
