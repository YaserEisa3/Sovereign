using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Enters Play mode, lets the clock run, and checks that the dashboard actually
    /// MOVED. The simulation advancing is not the same thing as the screen showing
    /// it, and this project has already shipped one gap exactly like that.
    ///
    /// Entering Play mode reloads the domain, which wipes static state and silently
    /// drops any EditorApplication.update subscription - so the run is flagged in
    /// SessionState and the static constructor re-attaches on the other side.
    ///
    /// Headless: -executeMethod Sovereign.EditorTools.PlayModeSmoke.Run
    /// (no -quit, and without -nographics so the UI really renders).
    /// </summary>
    [InitializeOnLoad]
    public static partial class PlayModeSmoke
    {
        const string FlagKey = "Sovereign.PlayModeSmoke.Running";
        // Wall clock, not frames: batch mode can burn thousands of editor frames in
        // the time it takes four one-second weeks to pass.
        const double TimeoutSeconds = 150.0;

        static double _deadline;
        static double _nextLog;

        static PlayModeSmoke()
        {
            // Runs again after the domain reload that Play mode triggers.
            if (!SessionState.GetBool(FlagKey, false)) return;
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Tick;
        }

        [MenuItem("Sovereign/Run Play Mode Smoke Test", false, 45)]
        public static void Run()
        {
            SessionState.SetBool(FlagKey, true);
            EditorSceneManager.OpenScene(ProjectBuilder.ScenePath, OpenSceneMode.Single);
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            SimulationRunner runner = EditorApplication.isPlaying
                ? UnityEngine.Object.FindAnyObjectByType<SimulationRunner>()
                : null;

            if (now > _nextLog)
            {
                _nextLog = now + 5.0;
                Debug.Log("PlayModeSmoke: waiting - isPlaying " + EditorApplication.isPlaying
                          + ", runner " + (runner == null ? "null" : "found")
                          + ", week " + (runner == null || runner.State == null ? -1 : runner.State.week));
            }

            if (now > _deadline)
            {
                Finish(false, "timed out after " + TimeoutSeconds + "s - isPlaying was " + EditorApplication.isPlaying
                              + ", week " + (runner == null || runner.State == null ? -1 : runner.State.week));
                return;
            }

            if (runner == null || runner.State == null) return;

            // Let several weeks pass at a second each before judging anything.
            if (runner.State.week < 4) return;

            if (_stage == Stage.Dashboard) Check(runner);
            else RunDrawerStages(runner);
        }

        static void Check(SimulationRunner runner)
        {
            UIDocument document = null;
            foreach (UIDocument candidate in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (candidate.gameObject.name == "UIRoot") document = candidate;

            if (document == null || document.rootVisualElement == null)
            {
                Finish(false, "UIRoot has no live visual tree in Play mode");
                return;
            }

            VisualElement root = document.rootVisualElement;
            Label date = root.Q<Label>("date-label");
            Label readout = root.Q<Label>("chart-readout");
            VisualElement kpiRow = root.Q<VisualElement>("kpi-row");
            Label firstValue = kpiRow == null ? null : kpiRow.Q<Label>("kpi-value");

            string detail = "";
            if (date == null || string.IsNullOrEmpty(date.text)) detail += " no date label;";
            if (readout == null || !readout.text.Contains("weeks recorded"))
                detail += " chart readout still shows the authored placeholder;";
            if (kpiRow == null || kpiRow.childCount < 11) detail += " KPI cards were not stamped from the template;";
            // Revenue and spending belong on the headline row: every fiscal number on the
            // screen is made of those two, and the player was reading ratios of invisible
            // figures.
            bool money = false;
            if (kpiRow != null)
                foreach (Label label in kpiRow.Query<Label>(className: "kpi-label").ToList())
                    if (label.text == "REVENUE" || label.text == "SPENDING" || label.text == "TOTAL DEBT") money = true;
            if (!money) detail += " no revenue or spending card;";

            // The debt ratio is a judgement; the stock is the fact. Both belong up here.
            bool debtStock = false;
            if (kpiRow != null)
                foreach (Label label in kpiRow.Query<Label>(className: "kpi-label").ToList())
                    if (label.text == "TOTAL DEBT") debtStock = true;
            bool gapCard = false;
            if (kpiRow != null)
                foreach (Label label in kpiRow.Query<Label>(className: "kpi-label").ToList())
                    if (label.text == "DEFICIT" || label.text == "SURPLUS") gapCard = true;
            if (!gapCard) detail += " no deficit card;";
            if (!debtStock) detail += " no total debt card;";

            // Where the money comes from now lives on the front page, where the bond
            // chart used to be.
            VisualElement breakdown = root.Q<VisualElement>("revenue-breakdown");
            Label breakdownNote = root.Q<Label>("breakdown-note");
            int bars = 0;
            if (breakdown != null)
                foreach (VisualElement bar in breakdown.Children())
                    if (bar.style.display != DisplayStyle.None) bars++;
            if (bars < 4) detail += " the dashboard revenue breakdown drew " + bars + " bars;";
            if (breakdownNote == null || !breakdownNote.text.Contains("% of GDP")) detail += " no breakdown total;";
            if (root.Q<VisualElement>("bond-panel") != null) detail += " the bond chart is still on the front page;";
            if (firstValue == null || string.IsNullOrEmpty(firstValue.text)) detail += " first KPI card is empty;";

            Debug.Log("PlayModeSmoke: week " + runner.State.week
                      + " | date '" + (date == null ? "" : date.text) + "'"
                      + " | gdp " + runner.State.realGdpGrowth.ToString("0.00") + "%"
                      + " | kpi[0] '" + (firstValue == null ? "" : firstValue.text) + "'"
                      + " | readout '" + (readout == null ? "" : readout.text) + "'");

            if (detail.Length > 0) { Finish(false, detail); return; }

            Debug.Log("PlayModeSmoke: dashboard is live - now driving the Fiscal drawer.");
            _stage = Stage.OpenFiscal;
        }

        static void Finish(bool passed, string detail)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(FlagKey, false);

            if (passed) Debug.Log("PlayModeSmoke: PASS - every drawer, approval, population, game over and restart, events, diplomacy, regulation, nations on the map, ships, save/load, chart hover and Steam presence.");
            else Debug.LogError("PlayModeSmoke: FAIL -" + (string.IsNullOrEmpty(detail) ? " unknown" : detail));

            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else EditorApplication.ExitPlaymode();
        }
    }
}
