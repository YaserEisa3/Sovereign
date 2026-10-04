using System.Collections.Generic;
using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Data
{
    /// <summary>One line of an event's impact profile. Authored in the Inspector.</summary>
    [System.Serializable]
    public struct EconomicImpact
    {
        [Tooltip("What this impact pushes on.")]
        public ImpactTarget target;

        [Tooltip("Size of the push, in that target's own units, applied per week while the event is active.")]
        public float weeklyMagnitude;

        [Tooltip("Optional: restrict this impact to one sector. Leave as None-equivalent by ticking sectorSpecific off.")]
        public bool sectorSpecific;
        public SectorId sector;

        [Tooltip("How much the relevant mitigant (healthcare level, disaster fund, defence spend) can reduce this impact, 0-1.")]
        [Range(0f, 1f)] public float mitigable;
    }

    /// <summary>A gate on an event: it can only fire while every one of these holds.</summary>
    [System.Serializable]
    public struct TriggerConditionData
    {
        public ConditionMetric metric;
        public Comparison comparison;
        public float threshold;
    }

    /// <summary>
    /// GDD 17. Events arrive as alerts, never as multiple-choice popups. Every
    /// concrete event in the game is one of these assets - none are defined in code.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Event_", menuName = "Sovereign/Event Definition")]
    public class EventDefinition : ScriptableObject
    {
        [Header("Presentation")]
        public string title = "Untitled Event";
        [TextArea(3, 6)] public string description = "";
        [Tooltip("Ticker line shown when the event first appears as an early signal.")]
        [TextArea(2, 3)] public string earlySignalHeadline = "";
        [Tooltip("Advisor line shown when the event becomes imminent.")]
        [TextArea(2, 3)] public string imminentAdvisorLine = "";
        public EventCategory category = EventCategory.FinancialCrisis;

        [Header("Warning - GDD 17.2")]
        [Tooltip("Weeks of warning before the event strikes. 0 means it arrives as a pure shock with no telegraph.")]
        [Range(0, 26)] public int warningWeeksBeforeStrike = 0;

        [Tooltip("Of those warning weeks, how many are spent at Early Signal before escalating to Imminent.")]
        [Range(0, 20)] public int earlySignalWeeks = 0;

        [Header("Duration and escalation")]
        [Range(1, 260)] public int minDurationWeeks = 4;
        [Range(1, 260)] public int maxDurationWeeks = 26;

        [Tooltip("Chance per quarter that an active event escalates into something worse, 0-1.")]
        [Range(0f, 1f)] public float escalationRisk = 0f;

        [Tooltip("Event this one can escalate into. Leave empty if it cannot escalate.")]
        public EventDefinition escalatesTo;

        [Header("Trigger")]
        [Tooltip("Base chance per year that this event fires at all, 0-1. Modified at runtime by world state.")]
        [Range(0f, 1f)] public float baseAnnualProbability = 0.05f;

        [Tooltip("Earliest in-game year this event can fire. Late-game events (invasion) sit above 0.")]
        [Range(0, 50)] public int earliestYear = 0;

        [Tooltip("This event cannot fire while another instance of it is active.")]
        public bool uniqueWhileActive = true;

        [Tooltip("Every condition must hold for the event to be able to fire at all. GDD 17.6: a banking crisis needs weak banks, a hyperinflation needs unanchored expectations. Empty means it can happen to anyone.")]
        public List<TriggerConditionData> conditions = new List<TriggerConditionData>();

        [Header("Impacts - applied every week while active")]
        public List<EconomicImpact> impacts = new List<EconomicImpact>();

        [Header("Map presentation - GDD 3.4")]
        [Tooltip("Which overlay layer this event's marker belongs under. Blank means the event has no map marker.")]
        public string overlayLayerName = "";
        [Tooltip("Prefab key the EventEngine instantiates for this event. Assigned from GameDatabase's prefab list, never loaded by string path at runtime.")]
        public GameObject markerPrefab;
    }
}
