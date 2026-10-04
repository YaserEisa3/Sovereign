using UnityEditor;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 3.4. Everything that repeats or gets spawned is a prefab, so scripts
    /// instantiate prefabs instead of assembling objects piece by piece in code.
    /// The disaster family uses prefab VARIANTS of one MapIcon base, as asked.
    ///
    /// These carry visuals only. NationView, TradeRouteView and the marker
    /// behaviours attach to them in Phase 5 - the objects exist first.
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string PrefabPath = Root + "/Prefabs/Map/";
        const string MaterialPath = Root + "/Materials/";

        public class Prefabs
        {
            public GameObject nationMarker, tradeShip, warZoneMarker, mapIcon, unrestIcon, factoryIcon, refugeeFlowMarker;
            public GameObject[] disasterIcons;
            public Material routeMaterial, oceanMaterial;
        }

        static Font _font;

        static Material Mat(string name, Color color)
        {
            string path = MaterialPath + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            // Unlit keeps the map flat and readable with no lights in the scene -
            // a war room display, not a lit 3D world.
            Material m = new Material(Shader.Find("Unlit/Color")) { color = color };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameObject Quad(string name, Material material, float width, float height, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(width, height, 1f);
            return go;
        }

        /// <summary>A small diamond - the map's vocabulary for "something is happening
        /// here", readable at a glance and gone in a second when it ends.</summary>
        static GameObject Diamond(string name, Material material, float size, Transform parent, float z)
        {
            float half = size * 0.5f;
            return Shape(name, material, new[]
            {
                new Vector2(0f, half), new Vector2(half, 0f), new Vector2(0f, -half), new Vector2(-half, 0f)
            }, parent, z);
        }

        static GameObject Label(string name, string text, float size, Color color, Transform parent, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = _font;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            return go;
        }

        static GameObject SavePrefab(GameObject source, string file)
        {
            string path = PrefabPath + file + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && !_overwrite)
            {
                Object.DestroyImmediate(source);
                return existing;
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab;
        }
    }
}
