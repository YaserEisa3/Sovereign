using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Phase 2 through the UI: approval on the dashboard, the Population
    /// drawer, and the run ending and restarting.</summary>
    public static partial class PlayModeSmoke
    {
        static void RunPhase2Stages()
        {
            SimulationRunner runner = Object.FindAnyObjectByType<SimulationRunner>();
            GameObject popup = FindPanel("Panel_EventPopup");

            switch (_stage)
            {
                case Stage.OpenPopulation:
                {
                    VisualElement dash = DashboardRoot();
                    int classes = CountChildren(dash, "class-approval-list");
                    int factions = CountChildren(dash, "faction-approval-list");
                    Label overall = dash.Q<Label>("overall-approval");
                    Debug.Log("PlayModeSmoke: approval - " + classes + " class bars, " + factions + " faction bars, '"
                              + (overall == null ? "" : overall.text) + "'");
                    if (!Require(classes == 3 && factions == 3, "dashboard built " + classes + "+" + factions + " approval bars, expected 3+3")) return;
                    if (!Require(overall != null && overall.text.StartsWith("Overall"), "the overall approval label is not live")) return;
                    if (!Press(Tab("open-population"), "the Population tab")) return;
                    Next(Stage.VerifyPopulation);
                    return;
                }

                case Stage.VerifyPopulation:
                {
                    GameObject population = FindPanel("Panel_Population");
                    if (!Require(population != null && population.activeSelf, "the Population tab did not open it")) return;
                    VisualElement root = PanelRoot(population);
                    int bands = CountChildren(root, "age-band-list");
                    int sectors = CountChildren(root, "sector-employment-list");
                    int levers = CountChildren(root, "population-policy-list");
                    int unrest = CountChildren(root, "income-class-list");
                    Label pool = root.Q<Label>("taxable-pool-readout");
                    Debug.Log("PlayModeSmoke: population drawer - " + bands + " bands, " + sectors + " sectors, "
                              + levers + " levers, " + unrest + " unrest rows, '" + (pool == null ? "" : pool.text) + "'");
                    if (!Require(bands == 3 && sectors == 7 && levers == 3 && unrest == 3,
                                 "population drawer built " + bands + "/" + sectors + "/" + levers + "/" + unrest + ", expected 3/7/3/3")) return;
                    if (!Require(pool != null && pool.text.Contains("$"), "the taxable income pool readout is not live")) return;
                    _stage = Stage.EndRun;
                    return;
                }

                case Stage.EndRun:
                    runner.EndRun("Smoke test: ended deliberately to check the game-over screen.");
                    Next(Stage.VerifyGameOver);
                    return;

                case Stage.VerifyGameOver:
                {
                    if (!Require(popup != null && popup.activeSelf, "the run ended but no game-over screen appeared")) return;
                    if (!Require(runner.Speed == GameSpeed.Paused, "the clock kept running after game over")) return;
                    Label title = PanelRoot(popup).Q<Label>("popup-title");
                    if (!Require(title != null && title.text == "The government has fallen", "the game-over card has the wrong title")) return;
                    Debug.Log("PlayModeSmoke: game over shown - '" + title.text + "'");
                    if (!Press(PanelRoot(popup).Q<Button>("popup-open-panel"), "the Start a new run button")) return;
                    Next(Stage.VerifyRestart);
                    return;
                }

                case Stage.VerifyRestart:
                {
                    bool fresh = !popup.activeSelf && !runner.State.IsGameOver && runner.State.week < 30
                                 && runner.Speed == GameSpeed.Normal;
                    Debug.Log("PlayModeSmoke: after restart - week " + runner.State.week + ", game over " + runner.State.IsGameOver);
                    if (!Require(fresh, "Start a new run did not give a fresh, running game")) return;
                    _stage = Stage.SpawnEvents;
                    return;
                }

                default:
                    RunPhase3Stages();
                    return;
            }
        }
    }
}
