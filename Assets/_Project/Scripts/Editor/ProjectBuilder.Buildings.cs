using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 3.4 and 18. The country you are running, drawn as buildings on the map:
    /// houses where people live, and a distinct silhouette for each sector, so a
    /// glance at the map says what this economy is MADE of rather than what its
    /// numbers are. Every shape is a flat unlit polygon, like the rest of the map -
    /// a war-room display, not a lit 3D city.
    ///
    /// Pivots sit on the ground line, so a building grows upward from where it stands.
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string BuildingPath = Root + "/Prefabs/Map/Buildings/";

        public static readonly string[] HousingTierNames = { "Building_Housing_1", "Building_Housing_2", "Building_Housing_3" };

        static GameObject[] _housingPrefabs;
        static GameObject[] _sectorBuildingPrefabs;
        static GameObject _farmlandPrefab;

        /// <summary>A flat polygon, in local units, fanned from its first point.</summary>
        static GameObject Shape(string name, Material material, Vector2[] points, Transform parent, float z)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);

            Vector3[] vertices = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) vertices[i] = new Vector3(points[i].x, points[i].y, 0f);

            // Both windings. A flat polygon has no meaningful front face, and getting the
            // winding wrong against an Unlit/Color material that culls back faces means
            // the building exists, reports correct bounds, and draws nothing at all.
            List<int> triangles = new List<int>();
            for (int i = 1; i < points.Length - 1; i++)
            {
                triangles.Add(0); triangles.Add(i); triangles.Add(i + 1);
                triangles.Add(0); triangles.Add(i + 1); triangles.Add(i);
            }

            Mesh mesh = new Mesh { name = "Mesh_" + name };
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(mesh, "Building_" + parent.name + "_" + name);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static GameObject Rect(string name, Material material, float x, float y, float w, float h, Transform parent, float z)
        {
            return Shape(name, material, new[]
            {
                new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h)
            }, parent, z);
        }

        /// <summary>Builds the ten building prefabs: three housing tiers and one per sector.</summary>
        static void BuildBuildingPrefabs()
        {
            EnsureFolder(Root + "/Prefabs/Map", "Buildings");

            Material wall = Mat("Mat_Building_Wall", new Color32(0x8a, 0x96, 0xa8, 0xff));
            Material roof = Mat("Mat_Building_Roof", new Color32(0x53, 0x5e, 0x70, 0xff));
            Material lit = Mat("Mat_Building_Window", new Color32(0xf0, 0xc8, 0x6a, 0xff));

            _housingPrefabs = new[]
            {
                HouseCottage(wall, roof),
                HouseBlock(wall, roof, lit),
                HouseTower(wall, lit)
            };

            // Indexed BY SectorId, not by asset order: CityscapeView looks a prefab up by
            // the sector's enum value, and the assets are authored in their own order.
            // Farmland is drawn from the same palette as the sector it belongs to.
            SectorDefinition farm = null;
            foreach (SectorDefinition s in _sectors) if (s.sectorId == SectorId.Agriculture) farm = s;
            _farmlandPrefab = Farmland(Mat("Mat_Building_Crop", farm == null ? Color.green : farm.chartColor),
                                       Mat("Mat_Building_Soil", new Color32(0x4a, 0x42, 0x33, 0xff)));

            _sectorBuildingPrefabs = new GameObject[System.Enum.GetValues(typeof(SectorId)).Length];
            for (int i = 0; i < _sectors.Length; i++)
            {
                Material body = Mat("Mat_Building_" + _sectors[i].sectorId, _sectors[i].chartColor);
                _sectorBuildingPrefabs[(int)_sectors[i].sectorId] = SectorBuilding(_sectors[i].sectorId, body, roof, lit);
            }
        }

        /// <summary>Worked fields: furrows under a hedgerow. Farmland is most of what
        /// agriculture looks like from above, and a barn alone never said "country".</summary>
        static GameObject Farmland(Material crop, Material soil)
        {
            GameObject go = new GameObject("Building_Farmland");
            Rect("Field", soil, -0.30f, 0f, 0.60f, 0.20f, go.transform, 0.001f);
            for (int i = 0; i < 5; i++)
                Rect("Furrow" + i, crop, -0.28f + i * 0.115f, 0.02f, 0.07f, 0.16f, go.transform, 0f);
            Rect("Hedge", soil, -0.30f, 0.20f, 0.60f, 0.03f, go.transform, -0.001f);
            return SaveBuilding(go);
        }

        static GameObject HouseCottage(Material wall, Material roof)
        {
            GameObject go = new GameObject(HousingTierNames[0]);
            Rect("Body", wall, -0.16f, 0f, 0.32f, 0.20f, go.transform, 0f);
            Shape("Roof", roof, new[] { new Vector2(-0.20f, 0.20f), new Vector2(0.20f, 0.20f), new Vector2(0f, 0.34f) }, go.transform, -0.001f);
            return SaveBuilding(go);
        }

        static GameObject HouseBlock(Material wall, Material roof, Material lit)
        {
            GameObject go = new GameObject(HousingTierNames[1]);
            Rect("Body", wall, -0.17f, 0f, 0.34f, 0.44f, go.transform, 0f);
            Rect("Cornice", roof, -0.20f, 0.44f, 0.40f, 0.05f, go.transform, -0.001f);
            // Two rows of lit windows: an apartment block, not a bigger cottage.
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 3; col++)
                    Rect("Window" + row + col, lit, -0.13f + col * 0.10f, 0.08f + row * 0.16f, 0.06f, 0.08f, go.transform, -0.002f);
            return SaveBuilding(go);
        }

        static GameObject HouseTower(Material wall, Material lit)
        {
            GameObject go = new GameObject(HousingTierNames[2]);
            Rect("Body", wall, -0.15f, 0f, 0.30f, 0.78f, go.transform, 0f);
            Rect("Crown", lit, -0.04f, 0.78f, 0.08f, 0.10f, go.transform, -0.001f);
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 2; col++)
                    Rect("Window" + row + col, lit, -0.10f + col * 0.11f, 0.08f + row * 0.14f, 0.07f, 0.09f, go.transform, -0.002f);
            return SaveBuilding(go);
        }

        static GameObject SectorBuilding(SectorId id, Material body, Material roof, Material lit)
        {
            GameObject go = new GameObject("Building_" + id);
            switch (id)
            {
                case SectorId.Manufacturing:
                    // Sawtooth roof and a chimney - the universal shorthand for a works.
                    Rect("Shed", body, -0.30f, 0f, 0.60f, 0.26f, go.transform, 0f);
                    for (int i = 0; i < 3; i++)
                        Shape("Saw" + i, roof, new[]
                        {
                            new Vector2(-0.30f + i * 0.20f, 0.26f),
                            new Vector2(-0.10f + i * 0.20f, 0.26f),
                            new Vector2(-0.10f + i * 0.20f, 0.38f)
                        }, go.transform, -0.001f);
                    Rect("Chimney", roof, 0.20f, 0.26f, 0.08f, 0.34f, go.transform, -0.001f);
                    Rect("Smoke", lit, 0.21f, 0.60f, 0.06f, 0.06f, go.transform, -0.002f);
                    break;

                case SectorId.Agriculture:
                    // A barn with a pitched roof, a silo beside it, and furrows in front.
                    Rect("Barn", body, -0.28f, 0f, 0.34f, 0.22f, go.transform, 0f);
                    Shape("BarnRoof", roof, new[]
                    {
                        new Vector2(-0.32f, 0.22f), new Vector2(0.10f, 0.22f), new Vector2(-0.11f, 0.36f)
                    }, go.transform, -0.001f);
                    Rect("Silo", body, 0.12f, 0f, 0.14f, 0.40f, go.transform, 0f);
                    Shape("SiloCap", roof, new[]
                    {
                        new Vector2(0.10f, 0.40f), new Vector2(0.28f, 0.40f), new Vector2(0.19f, 0.48f)
                    }, go.transform, -0.001f);
                    for (int i = 0; i < 3; i++)
                        Rect("Furrow" + i, roof, -0.30f + i * 0.22f, -0.05f, 0.16f, 0.02f, go.transform, 0.001f);
                    break;

                case SectorId.Technology:
                    // A slim glass tower with a lit band and a mast.
                    Rect("Tower", body, -0.13f, 0f, 0.26f, 0.72f, go.transform, 0f);
                    Rect("Band", lit, -0.13f, 0.46f, 0.26f, 0.06f, go.transform, -0.001f);
                    Rect("Band2", lit, -0.13f, 0.26f, 0.26f, 0.06f, go.transform, -0.001f);
                    Rect("Mast", roof, -0.01f, 0.72f, 0.02f, 0.16f, go.transform, -0.001f);
                    break;

                case SectorId.Energy:
                    // A cooling tower - waisted, with a plume above it.
                    Shape("Tower", body, new[]
                    {
                        new Vector2(-0.22f, 0f), new Vector2(0.22f, 0f), new Vector2(0.13f, 0.34f),
                        new Vector2(0.17f, 0.52f), new Vector2(-0.17f, 0.52f), new Vector2(-0.13f, 0.34f)
                    }, go.transform, 0f);
                    Rect("Plume", lit, -0.10f, 0.54f, 0.20f, 0.10f, go.transform, -0.001f);
                    break;

                case SectorId.ServicesRetail:
                    // A shopfront with a striped awning.
                    Rect("Front", body, -0.26f, 0f, 0.52f, 0.34f, go.transform, 0f);
                    for (int i = 0; i < 4; i++)
                        Rect("Stripe" + i, i % 2 == 0 ? roof : lit, -0.26f + i * 0.13f, 0.30f, 0.13f, 0.08f, go.transform, -0.001f);
                    Rect("Door", roof, -0.05f, 0f, 0.10f, 0.18f, go.transform, -0.002f);
                    break;

                case SectorId.Finance:
                    // Columns under a pediment: a bank, and nothing else looks like one.
                    Rect("Base", body, -0.28f, 0f, 0.56f, 0.06f, go.transform, 0f);
                    for (int i = 0; i < 4; i++)
                        Rect("Column" + i, body, -0.22f + i * 0.13f, 0.06f, 0.06f, 0.28f, go.transform, 0f);
                    Rect("Architrave", body, -0.28f, 0.34f, 0.56f, 0.05f, go.transform, -0.001f);
                    Shape("Pediment", roof, new[]
                    {
                        new Vector2(-0.30f, 0.39f), new Vector2(0.30f, 0.39f), new Vector2(0f, 0.54f)
                    }, go.transform, -0.001f);
                    break;

                default:
                    // Government: a dome on a block, with a flag.
                    Rect("Block", body, -0.26f, 0f, 0.52f, 0.26f, go.transform, 0f);
                    Shape("Dome", body, new[]
                    {
                        new Vector2(-0.14f, 0.26f), new Vector2(-0.10f, 0.38f), new Vector2(0f, 0.43f),
                        new Vector2(0.10f, 0.38f), new Vector2(0.14f, 0.26f)
                    }, go.transform, -0.001f);
                    Rect("Pole", roof, -0.01f, 0.43f, 0.02f, 0.14f, go.transform, -0.001f);
                    Rect("Flag", lit, 0.01f, 0.49f, 0.10f, 0.06f, go.transform, -0.002f);
                    break;
            }
            return SaveBuilding(go);
        }

        static GameObject SaveBuilding(GameObject source)
        {
            string path = BuildingPath + source.name + ".prefab";
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
