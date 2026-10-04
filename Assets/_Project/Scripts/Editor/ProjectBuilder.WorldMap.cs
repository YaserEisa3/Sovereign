using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Builds the world map from real coastline data (Natural Earth 110m, public
    /// domain) shipped as a TextAsset the project can open and inspect. The output
    /// is three saved Mesh assets and three GameObjects under MapRoot - authored
    /// geometry, not something reconstructed at play time.
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string MapDataPath = Root + "/Data/Map/";
        const float OceanZ = 1.0f;
        const float LandZ = 0.8f;
        const float CoastZ = 0.75f;
        const float GraticuleZ = 0.7f;

        static Material MatSprite(string name, Color color)
        {
            string path = MaterialPath + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            // Sprites/Default is unlit and renders both faces, which matters because
            // ear-clipped coastline rings have no guaranteed winding order.
            Material m = new Material(Shader.Find("Sprites/Default")) { color = color };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void BuildWorldMap(Transform mapRoot, Prefabs prefabs)
        {
            List<List<Vector2>> rings = LoadCoastlineRings();
            if (rings.Count == 0)
            {
                Debug.LogError("Sovereign: no coastline rings found at " + MapDataPath + "Coastlines.txt");
                return;
            }

            float halfWidth = MapProjection.HalfWidth;
            float halfHeight = MapProjection.HalfHeight;

            GameObject ocean = Quad("Ocean", prefabs.oceanMaterial, halfWidth * 2f, halfHeight * 2f, mapRoot);
            ocean.transform.localPosition = new Vector3(0f, 0f, OceanZ);

            BuildLand(rings, mapRoot);
            BuildCoastlines(rings, mapRoot);
            BuildGraticule(mapRoot);

            int points = 0;
            foreach (List<Vector2> ring in rings) points += ring.Count;
            Debug.Log("Sovereign: world map built from " + rings.Count + " coastline rings, " + points + " points.");
        }

        static List<List<Vector2>> LoadCoastlineRings()
        {
            List<List<Vector2>> rings = new List<List<Vector2>>();
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(MapDataPath + "Coastlines.txt");
            if (source == null) return rings;

            string[] lines = source.text.Split('\n');
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                string[] pairs = line.Split(' ');
                List<Vector2> ring = new List<Vector2>(pairs.Length);
                foreach (string pair in pairs)
                {
                    int comma = pair.IndexOf(',');
                    if (comma <= 0) continue;
                    float lon, lat;
                    if (!float.TryParse(pair.Substring(0, comma), NumberStyles.Float, CultureInfo.InvariantCulture, out lon)) continue;
                    if (!float.TryParse(pair.Substring(comma + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out lat)) continue;
                    ring.Add(MapProjection.Project(lon, lat));
                }
                // GeoJSON rings repeat their first point at the end, and clamping near the
                // poles collapses runs of points onto each other. Both give the ear
                // clipper zero-length edges, which is what tore a slash across Antarctica.
                Dedupe(ring);
                if (ring.Count >= 3) rings.Add(ring);
            }
            return rings;
        }

        static void Dedupe(List<Vector2> ring)
        {
            const float epsilon = 0.0001f;
            for (int i = ring.Count - 1; i > 0; i--)
                if ((ring[i] - ring[i - 1]).sqrMagnitude < epsilon) ring.RemoveAt(i);
            while (ring.Count > 2 && (ring[ring.Count - 1] - ring[0]).sqrMagnitude < epsilon)
                ring.RemoveAt(ring.Count - 1);
        }

        static Mesh SaveMesh(Mesh mesh, string file)
        {
            string path = MapDataPath + file + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent, float z)
        {
            GameObject go = Child(name, parent);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
