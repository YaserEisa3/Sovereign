using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sovereign.EditorTools
{
    /// <summary>The map, UI and asset-count half of the Phase 0 check.</summary>
    public static partial class Phase0Check
    {
        static void CheckMap(Dictionary<string, Transform> roots)
        {
            Transform worldMap;
            if (!roots.TryGetValue("--- WORLD MAP ---", out worldMap)) return;

            Transform mapRoot = worldMap.Find("MapRoot");
            Check(mapRoot != null, "missing MapRoot");
            if (mapRoot == null) return;

            // The map is real geography now: ocean sheet, filled land, coastlines, graticule.
            foreach (string layer in new[] { "Ocean", "Land", "Coastlines", "Graticule" })
                Check(mapRoot.Find(layer) != null, "missing map layer " + layer);

            Transform land = mapRoot.Find("Land");
            if (land != null)
            {
                MeshFilter filter = land.GetComponent<MeshFilter>();
                Check(filter != null && filter.sharedMesh != null, "Land has no mesh");
                if (filter != null && filter.sharedMesh != null)
                    Check(filter.sharedMesh.vertexCount > 1000,
                          "Land mesh has only " + filter.sharedMesh.vertexCount + " vertices - coastline data did not load");
            }

            Transform nations = mapRoot.Find("Nations");
            Check(nations != null, "missing Nations parent");
            if (nations != null)
            {
                foreach (string nation in new[]
                {
                    "Nation_Home", "Nation_Euroland", "Nation_SinoPacific", "Nation_PetroGulf",
                    "Nation_EmergingSouth", "Nation_IslandFinance", "Nation_NorthernAlliance"
                })
                {
                    Transform marker = nations.Find(nation);
                    Check(marker != null, "missing " + nation);
                    if (marker != null)
                        Check(PrefabUtility.GetCorrespondingObjectFromSource(marker.gameObject) != null,
                              nation + " is not an instance of the NationMarker prefab");
                }
            }

            Transform routes = mapRoot.Find("TradeRoutes");
            Check(routes != null, "missing TradeRoutes parent");
            if (routes != null)
            {
                Check(routes.childCount == 6, "expected 6 trade routes, found " + routes.childCount);
                foreach (Transform route in routes)
                    Check(route.childCount >= 4, route.name + " has fewer than 4 waypoints");
            }

            Transform overlays = mapRoot.Find("Overlays");
            Check(overlays != null, "missing Overlays parent");
            if (overlays == null) return;
            foreach (string layer in new[] { "WarZoneLayer", "DisasterLayer", "UnrestLayer", "FDILayer" })
                Check(overlays.Find(layer) != null, "missing overlay layer " + layer);
        }

        static void CheckUI(Dictionary<string, Transform> roots)
        {
            Transform ui;
            if (!roots.TryGetValue("--- UI ---", out ui)) return;

            foreach (string panel in new[]
            {
                "UIRoot", "Panel_Fiscal", "Panel_MonetaryBonds", "Panel_TradeCurrency", "Panel_Regulatory",
                "Panel_Population", "Panel_WorldEvents", "Panel_Advisor", "Panel_EventPopup"
            })
            {
                Transform panelTransform = ui.Find(panel);
                Check(panelTransform != null, "missing UI object " + panel);
                if (panelTransform == null) continue;

                UIDocument document = panelTransform.GetComponent<UIDocument>();
                Check(document != null, panel + " has no UIDocument");
                if (document == null) continue;

                Check(document.visualTreeAsset != null, panel + " has no UXML assigned");
                Check(document.panelSettings != null, panel + " has no PanelSettings assigned");
                if (document.panelSettings != null)
                    Check(document.panelSettings.themeStyleSheet != null,
                          panel + " PanelSettings has no theme style sheet - the panel would render nothing");
            }
        }

        /// <summary>The map is only visible through the transparent dashboard viewport,
        /// so a full-screen camera rect means a map nobody can see. This check exists
        /// because the first Phase 0 build passed every other test with an invisible map.</summary>
        static void CheckCamera(Dictionary<string, Transform> roots)
        {
            Transform cameras;
            if (!roots.TryGetValue("--- CAMERAS ---", out cameras)) return;

            Transform cameraTransform = cameras.Find("MainCamera");
            Check(cameraTransform != null, "missing MainCamera");
            if (cameraTransform == null) return;

            Camera camera = cameraTransform.GetComponent<Camera>();
            Check(camera != null, "MainCamera has no Camera component");
            if (camera == null) return;

            Check(camera.rect != new Rect(0f, 0f, 1f, 1f),
                  "MainCamera fills the screen, so the map renders behind the opaque UI - run Sovereign > Fit Map Camera to Dashboard");
            Check(camera.orthographic, "MainCamera is not orthographic");
        }

        static void CheckAssetCounts()
        {
            CheckCount("SectorDefinition", 7);
            CheckCount("NationDefinition", 7);
            CheckCount("TaxDefinition", 13);
            CheckCount("SpendingCategoryDefinition", 30);
            CheckCount("EventDefinition", 20);

            int uxml = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { ProjectBuilder.Root + "/UI" }).Length;
            Check(uxml >= 18, "expected at least 18 UXML assets (9 documents + 9 templates), found " + uxml);

            int prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectBuilder.Root + "/Prefabs" }).Length;
            Check(prefabs >= 12, "expected at least 12 map prefabs, found " + prefabs);
        }

        static void CheckCount(string type, int expectedMinimum)
        {
            int found = AssetDatabase.FindAssets("t:" + type, new[] { ProjectBuilder.Root + "/Data" }).Length;
            Check(found >= expectedMinimum,
                  "expected at least " + expectedMinimum + " " + type + " assets, found " + found);
        }
    }
}
