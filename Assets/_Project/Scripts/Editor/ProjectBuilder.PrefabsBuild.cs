using UnityEditor;
using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>The seven map prefabs of GDD 3.4, plus the five disaster variants.</summary>
    public static partial class ProjectBuilder
    {
        static Prefabs BuildPrefabs()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Prefabs p = new Prefabs();

            Material home = Mat("Mat_NationHome", _theme.homeNation);
            Material neutral = Mat("Mat_NationNeutral", _theme.neutralNation);
            Material danger = Mat("Mat_Danger", _theme.danger);
            Material warn = Mat("Mat_Warning", _theme.warning);
            Material growth = Mat("Mat_Growth", _theme.growth);
            Material data = Mat("Mat_Data", _theme.neutralData);
            p.routeMaterial = Mat("Mat_TradeRoute", _theme.tradeRoute);
            p.oceanMaterial = Mat("Mat_MapOcean", _theme.mapOcean);

            // NationMarker - GDD 3.4: label, flag and relationship ring.
            GameObject nation = new GameObject("NationMarker");
            // No marker square: the country IS the buildings standing on it. All that sits
            // above the city is its name, which carries the relationship in its colour.
            Label("NameLabel", "NATION", 0.07f, _theme.primaryText, nation.transform, new Vector3(0f, 1.45f, 0f));
            Label("StatusLabel", "", 0.05f, _theme.secondaryText, nation.transform, new Vector3(0f, 1.18f, 0f));
            p.nationMarker = SavePrefab(nation, "NationMarker");

            GameObject ship = new GameObject("TradeShip");
            Quad("Hull", data, 0.28f, 0.14f, ship.transform);
            p.tradeShip = SavePrefab(ship, "TradeShip");

            // Crises are diamonds, not billboards: the map is a country now, and a red
            // square the size of Texas hid everything the player was trying to read.
            GameObject warZone = new GameObject("WarZoneMarker");
            Diamond("Pulse", danger, 0.52f, warZone.transform, -0.01f);
            Diamond("Core", danger, 0.26f, warZone.transform, -0.02f);
            Label("WarLabel", "CONFLICT", 0.055f, _theme.danger, warZone.transform, new Vector3(0f, 0.36f, -0.02f));
            warZone.AddComponent<Sovereign.Presentation.MapMarkerPulse>();
            p.warZoneMarker = SavePrefab(warZone, "WarZoneMarker");

            GameObject icon = new GameObject("MapIcon");
            Diamond("Icon", warn, 0.3f, icon.transform, 0f);
            Label("IconLabel", "EVENT", 0.05f, _theme.warning, icon.transform, new Vector3(0f, 0.26f, 0f));
            p.mapIcon = SavePrefab(icon, "MapIcon");

            p.disasterIcons = new[]
            {
                DisasterVariant(p.mapIcon, "DisasterIcon_Earthquake", "QUAKE", _theme.danger),
                DisasterVariant(p.mapIcon, "DisasterIcon_Hurricane", "STORM", _theme.neutralData),
                DisasterVariant(p.mapIcon, "DisasterIcon_Drought", "DROUGHT", _theme.warning),
                DisasterVariant(p.mapIcon, "DisasterIcon_Flood", "FLOOD", _theme.neutralData),
                DisasterVariant(p.mapIcon, "DisasterIcon_Wildfire", "WILDFIRE", _theme.warning),
            };

            GameObject unrest = new GameObject("UnrestIcon");
            Diamond("Icon", warn, 0.24f, unrest.transform, 0f);
            Label("UnrestLabel", "UNREST", 0.045f, _theme.warning, unrest.transform, new Vector3(0f, 0.22f, 0f));
            p.unrestIcon = SavePrefab(unrest, "UnrestIcon");

            GameObject factory = new GameObject("FactoryIcon");
            Diamond("Icon", growth, 0.2f, factory.transform, 0f);
            Label("FdiLabel", "FDI", 0.045f, _theme.growth, factory.transform, new Vector3(0f, 0.19f, 0f));
            p.factoryIcon = SavePrefab(factory, "FactoryIcon");

            GameObject refugee = new GameObject("RefugeeFlowMarker");
            Quad("Flow", warn, 0.18f, 0.09f, refugee.transform);
            p.refugeeFlowMarker = SavePrefab(refugee, "RefugeeFlowMarker");

            return p;
        }

        /// <summary>A prefab VARIANT of MapIcon, per GDD 3.4 - one base, five faces.</summary>
        static GameObject DisasterVariant(GameObject basePrefab, string file, string label, Color color)
        {
            string path = PrefabPath + file + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && !_overwrite) return existing;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            inst.name = file;
            inst.transform.Find("Icon").GetComponent<MeshRenderer>().sharedMaterial = Mat("Mat_" + file, color);
            TextMesh tm = inst.transform.Find("IconLabel").GetComponent<TextMesh>();
            tm.text = label;
            tm.color = color;
            GameObject variant = PrefabUtility.SaveAsPrefabAsset(inst, path);
            Object.DestroyImmediate(inst);
            return variant;
        }
    }
}
