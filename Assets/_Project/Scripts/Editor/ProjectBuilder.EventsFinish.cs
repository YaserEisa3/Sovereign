using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// The second pass over the event assets, once every event and every map prefab
    /// exists: trigger conditions, escalation links and markers. Each is filled only
    /// where it is empty, so a designer's hand edits survive a non-destructive build.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static void FinishEvents(Prefabs prefabs)
        {
            // GDD 17.6: crises need the conditions that cause them.
            Gate("BankingCrisis", When(ConditionMetric.BankCapitalRequirement, Comparison.AtMost, 10f));
            Gate("HousingCollapse", When(ConditionMetric.RealRateThreeYearAverage, Comparison.Below, 0.5f));
            Gate("HyperinflationSpiral",
                 When(ConditionMetric.ExpectationsUnanchored, Comparison.AtLeast, 1f),
                 When(ConditionMetric.Inflation, Comparison.Above, 15f));
            Gate("DebtCrisis",
                 When(ConditionMetric.DebtToGdp, Comparison.Above, 1.8f),
                 When(ConditionMetric.CreditRating, Comparison.AtLeast, (float)CreditRating.BB));
            Gate("BondDumpingAttack",
                 When(ConditionMetric.ForeignHolding, Comparison.Above, 0.3f),
                 When(ConditionMetric.DebtToGdp, Comparison.Above, 1.3f));
            Gate("CurrencyAttack",
                 When(ConditionMetric.CurrencyIndex, Comparison.Below, 92f),
                 When(ConditionMetric.FxReserves, Comparison.Below, 300f));
            Gate("DomesticUnrest", When(ConditionMetric.ApprovalOverall, Comparison.Below, 45f));

            // What each can turn into.
            Link("DiplomaticIncident", "TradeWar");
            Link("RegionalWar", "RefugeeInflow");
            Link("BankingCrisis", "DebtCrisis");

            // GDD 3.4: markers are prefab instances under the overlay layers placed in the scene.
            Marker("RegionalWar", prefabs.warZoneMarker, "WarZoneLayer");
            Marker("InvasionThreat", prefabs.warZoneMarker, "WarZoneLayer");
            Marker("Earthquake", prefabs.disasterIcons[0], "DisasterLayer");
            Marker("HurricaneSeason", prefabs.disasterIcons[1], "DisasterLayer");
            Marker("Drought", prefabs.disasterIcons[2], "DisasterLayer");
            Marker("Flooding", prefabs.disasterIcons[3], "DisasterLayer");
            Marker("WildfireSeason", prefabs.disasterIcons[4], "DisasterLayer");
            Marker("DomesticUnrest", prefabs.unrestIcon, "UnrestLayer");
            Marker("RefugeeInflow", prefabs.refugeeFlowMarker, "UnrestLayer");
            foreach (string generic in new[]
            {
                "Pandemic", "TradeWar", "BankingCrisis", "HousingCollapse", "HyperinflationSpiral", "DebtCrisis",
                "BondDumpingAttack", "ForeignRecession", "EmergingSouthDefault", "OilPriceShock", "DiplomaticIncident",
                "WtoComplaint", "CurrencyAttack", "TechRevolution"
            })
                Marker(generic, prefabs.mapIcon, "DisasterLayer");

            AssetDatabase.SaveAssets();
        }

        static EventDefinition FindEvent(string key)
        {
            foreach (EventDefinition e in _events)
                if (e != null && e.name == "SO_Event_" + key) return e;
            Debug.LogError("Sovereign: no event asset for " + key);
            return null;
        }

        static void Gate(string key, params TriggerConditionData[] conditions)
        {
            EventDefinition e = FindEvent(key);
            if (e == null || (e.conditions != null && e.conditions.Count > 0)) return;
            e.conditions = new System.Collections.Generic.List<TriggerConditionData>(conditions);
            EditorUtility.SetDirty(e);
        }

        static void Link(string from, string to)
        {
            EventDefinition source = FindEvent(from), target = FindEvent(to);
            if (source == null || target == null || source.escalatesTo != null) return;
            source.escalatesTo = target;
            EditorUtility.SetDirty(source);
        }

        static void Marker(string key, GameObject prefab, string layer)
        {
            EventDefinition e = FindEvent(key);
            if (e == null) return;
            if (e.markerPrefab == null) e.markerPrefab = prefab;
            if (string.IsNullOrEmpty(e.overlayLayerName)) e.overlayLayerName = layer;
            EditorUtility.SetDirty(e);
        }
    }
}
