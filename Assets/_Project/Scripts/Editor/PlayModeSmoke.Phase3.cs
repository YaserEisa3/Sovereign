using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Phase 3 through the UI: events reach the map, the bell, the ticker,
    /// World Events and the Advisor.</summary>
    public static partial class PlayModeSmoke
    {
        static int _eventsSpawnWeek;

        static void RunPhase3Stages()
        {
            SimulationRunner runner = Object.FindAnyObjectByType<SimulationRunner>();
            EventEngine engine = Object.FindAnyObjectByType<EventEngine>();

            switch (_stage)
            {
                case Stage.SpawnEvents:
                    runner.Simulator.Events.Spawn(runner.State, runner.Policy, "Earthquake", true);
                    runner.Simulator.Events.Spawn(runner.State, runner.Policy, "RegionalWar", true);
                    runner.Simulator.Events.Spawn(runner.State, runner.Policy, "Pandemic", false);
                    _eventsSpawnWeek = runner.State.week;
                    _stage = Stage.VerifyEvents;
                    return;

                case Stage.VerifyEvents:
                {
                    // The map and the dashboard sync on the weekly tick.
                    if (runner.State.week <= _eventsSpawnWeek + 1) return;

                    int disasters = LayerChildren("DisasterLayer");
                    int wars = LayerChildren("WarZoneLayer");
                    VisualElement dash = DashboardRoot();
                    Label count = dash.Q<Label>("alert-count");
                    bool breaking = false;
                    foreach (Label label in dash.Q<VisualElement>("ticker-track").Query<Label>().ToList())
                        if (label.text.StartsWith("BREAKING")) breaking = true;

                    Debug.Log("PlayModeSmoke: events - " + disasters + " disaster markers, " + wars + " war markers, bell '"
                              + (count == null ? "" : count.text) + "', breaking news " + breaking
                              + ", engine tracking " + (engine == null ? -1 : engine.MarkerCount));

                    if (!Require(disasters >= 1, "the earthquake left no marker under DisasterLayer")) return;
                    if (!Require(wars >= 1, "the regional war left no marker under WarZoneLayer")) return;
                    if (!Require(count != null && count.text == "3", "the alert bell does not count the three live events")) return;
                    if (!Require(breaking, "the ticker did not break the news")) return;

                    if (!Press(dash.Q<Button>("alert-bell"), "the alert bell")) return;
                    Next(Stage.VerifyWorldEvents);
                    return;
                }

                case Stage.VerifyWorldEvents:
                {
                    GameObject panel = FindPanel("Panel_WorldEvents");
                    if (!Require(panel != null && panel.activeSelf, "the alert bell did not open World Events")) return;
                    VisualElement root = PanelRoot(panel);
                    int active = CountChildren(root, "active-event-list");
                    int warnings = CountChildren(root, "warning-event-list");
                    int nations = CountChildren(root, "nation-status-list");
                    Debug.Log("PlayModeSmoke: world events drawer - " + active + " active, " + warnings + " warnings, " + nations + " nations");
                    if (!Require(active == 2 && warnings == 1 && nations == 6,
                                 "World Events listed " + active + "/" + warnings + "/" + nations + ", expected 2/1/6")) return;

                    if (!Press(Tab("open-advisor"), "the Advisor tab")) return;
                    Next(Stage.VerifyAdvisor);
                    return;
                }

                case Stage.VerifyAdvisor:
                {
                    GameObject panel = FindPanel("Panel_Advisor");
                    if (!Require(panel != null && panel.activeSelf, "the Advisor tab did not open it")) return;
                    Label latest = PanelRoot(panel).Q<Label>("advisor-latest");
                    Debug.Log("PlayModeSmoke: advisor - '" + (latest == null ? "" : latest.text) + "'");
                    if (!Require(latest != null && latest.text.Length > 0, "the advisor drawer says nothing at all")) return;
                    if (!Press(Tab("open-trade"), "the Trade tab")) return;
                    Next(Stage.VerifyTrade);
                    return;
                }

                default:
                    RunPhase5Stages();
                    return;
            }
        }

        static int LayerChildren(string layer)
        {
            GameObject found = GameObject.Find(layer);
            return found == null ? -1 : found.transform.childCount;
        }
    }
}
