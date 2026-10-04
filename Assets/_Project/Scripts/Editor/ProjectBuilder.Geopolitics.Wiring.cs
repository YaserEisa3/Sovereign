using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Phase 5's scene wiring: the Trade and Regulatory drawers, and a
    /// NationView on each Nation_* object placed on the map since Phase 0.</summary>
    public static partial class ProjectBuilder
    {
        static void WireTradeAndRegulation(SimulationRunner runner, GameDatabase database, VisualTreeAsset policyRow)
        {
            GameObject trade;
            if (BuiltPanels.TryGetValue("Panel_TradeCurrency", out trade))
            {
                SerializedObject s = WireDrawerBase(trade.AddComponent<TradeCurrencyPanelController>(), runner, database, policyRow);
                s.FindProperty("diplomacyRowTemplate").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "Templates/DiplomacyRow.uxml");
                s.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject regulatory;
            if (BuiltPanels.TryGetValue("Panel_Regulatory", out regulatory))
                WireDrawerBase(regulatory.AddComponent<RegulatoryPanelController>(), runner, database, policyRow)
                    .ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireNationViews(SimulationRunner runner)
        {
            if (BuiltNations == null) return;
            for (int i = 0; i < BuiltNations.Length; i++)
            {
                Transform nation = BuiltNations[i];
                if (nation == null) continue;

                SerializedObject s = new SerializedObject(nation.gameObject.AddComponent<NationView>());
                s.FindProperty("runner").objectReferenceValue = runner;
                s.FindProperty("theme").objectReferenceValue = _theme;
                s.FindProperty("nationIndex").intValue = i;
                Transform nameLabel = nation.Find("NameLabel");
                if (nameLabel != null) s.FindProperty("nameLabel").objectReferenceValue = nameLabel.GetComponent<TextMesh>();
                Transform status = nation.Find("StatusLabel");
                if (status != null) s.FindProperty("statusLabel").objectReferenceValue = status.GetComponent<TextMesh>();
                s.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}

namespace Sovereign.EditorTools
{
    public static partial class ProjectBuilder
    {
        /// <summary>A TradeRouteView on each Route_* object. Routes were built in nation
        /// order skipping the home nation, so route k belongs to nation k + 1.</summary>
        static void WireTradeRoutes(Sovereign.Presentation.SimulationRunner runner, Prefabs prefabs)
        {
            for (int k = 0; k < BuiltRoutes.Count; k++)
            {
                UnityEditor.SerializedObject s = new UnityEditor.SerializedObject(
                    BuiltRoutes[k].AddComponent<Sovereign.Presentation.TradeRouteView>());
                s.FindProperty("runner").objectReferenceValue = runner;
                s.FindProperty("nationIndex").intValue = k + 1;
                s.FindProperty("shipPrefab").objectReferenceValue = prefabs.tradeShip;
                s.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
