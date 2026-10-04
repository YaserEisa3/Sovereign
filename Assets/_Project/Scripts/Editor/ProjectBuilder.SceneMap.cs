using UnityEditor;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 3.3, the world map. The seven nations are placed at real map positions
    /// and saved with the scene, so you can open it and see the whole world laid
    /// out - then drag any of them somewhere better. Trade routes are real objects
    /// with real child waypoints; TradeRouteView reads those waypoints in Phase 5
    /// rather than computing a curve from numbers buried in a script.
    /// </summary>
    public static partial class ProjectBuilder
    {
        // Hand-placed starting layout. Home sits west, Sino-Pacific east across the
        // widest span on the board, because that relationship is the long one.
        static readonly string[] NationObjectNames =
        {
            "Nation_Home", "Nation_Euroland", "Nation_SinoPacific", "Nation_PetroGulf",
            "Nation_EmergingSouth", "Nation_IslandFinance", "Nation_NorthernAlliance"
        };

        // Real coordinates, longitude then latitude. The archetypes map onto real
        // geography: the home nation is North America, Euroland is western Europe,
        // Sino-Pacific is east Asia, Petro-Gulf is the Arabian peninsula, Emerging
        // South is equatorial Africa, Island Finance is a Caribbean haven and the
        // Northern Alliance is Scandinavia.
        static readonly Vector2[] NationCoordinates =
        {
            new Vector2(-98f, 39f),   // Home
            new Vector2(6f, 47f),     // Euroland
            new Vector2(112f, 34f),   // Sino-Pacific
            new Vector2(48f, 24f),    // Petro-Gulf
            new Vector2(22f, 2f),     // Emerging South
            new Vector2(-73f, 22f),   // Island Finance
            new Vector2(18f, 64f)     // Northern Alliance
        };

        static readonly System.Collections.Generic.Dictionary<string, Transform> BuiltLayers =
            new System.Collections.Generic.Dictionary<string, Transform>();
        static Transform[] BuiltNations;

        static void BuildMap(Prefabs prefabs)
        {
            GameObject worldMap = new GameObject("--- WORLD MAP ---");
            GameObject mapRoot = Child("MapRoot", worldMap.transform);

            // Ocean, land, coastlines and graticule, from real coastline data.
            BuildWorldMap(mapRoot.transform, prefabs);

            GameObject nationsParent = Child("Nations", mapRoot.transform);
            Transform[] nationTransforms = new Transform[_nations.Length];

            for (int i = 0; i < _nations.Length; i++)
            {
                GameObject marker = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.nationMarker, nationsParent.transform);
                marker.name = NationObjectNames[i];
                Vector2 projected = MapProjection.Project(NationCoordinates[i].x, NationCoordinates[i].y);
                marker.transform.localPosition = new Vector3(projected.x, projected.y, 0f);

                TextMesh nameLabel = marker.transform.Find("NameLabel").GetComponent<TextMesh>();
                nameLabel.text = _nations[i].displayName.ToUpperInvariant();

                TextMesh statusLabel = marker.transform.Find("StatusLabel").GetComponent<TextMesh>();
                statusLabel.text = _nations[i].isPlayerNation ? "HOME" : "REL " + Mathf.RoundToInt(_nations[i].startingRelationship);

                nationTransforms[i] = marker.transform;
            }

            GameObject routesParent = Child("TradeRoutes", mapRoot.transform);
            BuiltRoutes.Clear();
            for (int i = 1; i < _nations.Length; i++)
            {
                string routeName = "Route_Home_" + NationObjectNames[i].Substring("Nation_".Length);
                BuildTradeRoute(routeName, routesParent.transform, nationTransforms[0].localPosition,
                                nationTransforms[i].localPosition, prefabs.routeMaterial);
            }

            // Spawn points for runtime markers, placed in advance so nothing ever
            // lands at the scene root - GDD 3.3.
            GameObject overlays = Child("Overlays", mapRoot.transform);
            BuiltLayers.Clear();
            foreach (string layer in new[] { "WarZoneLayer", "DisasterLayer", "UnrestLayer", "FDILayer" })
                BuiltLayers[layer] = Child(layer, overlays.transform).transform;
            BuildCitySites(nationTransforms);
            BuiltNations = nationTransforms;
        }

        static readonly System.Collections.Generic.List<GameObject> BuiltRoutes = new System.Collections.Generic.List<GameObject>();

        static void BuildTradeRoute(string name, Transform parent, Vector3 from, Vector3 to, Material material)
        {
            GameObject route = Child(name, parent);
            BuiltRoutes.Add(route);

            // Four waypoints, bowed perpendicular to the straight line so the route
            // reads as a shipping lane. Drag any of them to reshape the curve.
            Vector3 direction = to - from;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f).normalized;
            float bow = Mathf.Min(2.2f, direction.magnitude * 0.18f);

            Vector3[] points =
            {
                from,
                Vector3.Lerp(from, to, 0.33f) + perpendicular * bow,
                Vector3.Lerp(from, to, 0.66f) + perpendicular * bow * 0.7f,
                to
            };

            for (int i = 0; i < points.Length; i++)
            {
                GameObject waypoint = Child("Waypoint_" + i.ToString("00"), route.transform);
                waypoint.transform.localPosition = points[i];
            }

            // A LineRenderer so the lane is visible in the Editor before
            // TradeRouteView exists. Phase 5 replaces what draws it, not the data.
            LineRenderer line = route.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.widthMultiplier = 0.06f;
            line.sharedMaterial = material;
            line.numCornerVertices = 4;
            line.alignment = LineAlignment.TransformZ;
        }
    }
}
