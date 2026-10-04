using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>GDD 14 and 18. Six approval bars stamped from ApprovalBar.uxml, plus
    /// the overall number and how close the street is to boiling over.</summary>
    public partial class DashboardController
    {
        [Header("Approval")]
        [Tooltip("ApprovalBar.uxml - one per class and faction.")]
        [SerializeField] VisualTreeAsset approvalBarTemplate;

        [Tooltip("Unrest above this shows in the warning colour on the dashboard.")]
        [SerializeField] float unrestWarning = 30f;

        struct Bar { public Label value, trend; public VisualElement fill; public string series; }

        readonly Bar[] _bars = new Bar[6];

        void BuildApprovalBars()
        {
            if (approvalBarTemplate == null) return;
            VisualElement classes = _root.Q<VisualElement>("class-approval-list");
            VisualElement factions = _root.Q<VisualElement>("faction-approval-list");
            if (classes == null || factions == null) return;
            classes.Clear();
            factions.Clear();

            string[] names = { "Poor (30%)", "Middle (50%)", "Wealthy (20%)", "Progressive Left", "Centrist", "Conservative Right" };
            string[] series = { "approvalPoor", "approvalMiddle", "approvalWealthy", null, null, null };

            for (int i = 0; i < _bars.Length; i++)
            {
                VisualElement bar = approvalBarTemplate.Instantiate();
                (i < 3 ? classes : factions).Add(bar);

                Label name = bar.Q<Label>("approval-name");
                if (name != null) name.text = names[i];

                VisualElement fill = bar.Q<VisualElement>("approval-fill");
                if (fill != null && i >= 3) fill.AddToClassList("faction");

                _bars[i] = new Bar
                {
                    value = bar.Q<Label>("approval-value"),
                    trend = bar.Q<Label>("approval-trend"),
                    fill = fill,
                    series = series[i]
                };

                // Hovering a bar says WHY: the model already scores each group on a
                // handful of conditions, and until now it kept them to itself.
                int group = i;
                string groupName = names[i];
                VisualElement content = bar.childCount > 0 ? bar[0] : bar;
                content.RegisterCallback<PointerEnterEvent>(e => ExplainApproval(group, groupName, e.position));
                content.RegisterCallback<PointerLeaveEvent>(e => HideMeterTip());
            }
        }

        void RefreshApproval(EconomyState state)
        {
            ApprovalState a = state.approval;
            float[] values = { a.poor, a.middle, a.wealthy, a.left, a.centre, a.right };

            for (int i = 0; i < _bars.Length; i++)
            {
                Bar bar = _bars[i];
                if (bar.value == null) continue;

                bar.value.text = values[i].ToString("0") + "%";
                if (bar.fill != null)
                {
                    bar.fill.style.width = Length.Percent(values[i]);
                    if (theme != null && i < 3)
                        bar.fill.style.backgroundColor = values[i] >= 50f ? theme.growth
                                                       : values[i] >= 40f ? theme.warning : theme.danger;
                }

                if (bar.trend != null && bar.series != null)
                {
                    float change = values[i] - state.Series(bar.series).Ago(13);
                    bar.trend.text = Mathf.Abs(change) < 0.5f ? "" : (change > 0f ? "+" : "") + change.ToString("0");
                }
            }

            if (_overallApproval == null) return;
            float unrest = a.HighestUnrest;
            _overallApproval.text = "Overall " + a.overall.ToString("0") + "%"
                                    + (unrest >= 1f ? "     unrest " + unrest.ToString("0") + (unrest >= 90f ? " - REVOLT IMMINENT" : "") : "");
            if (theme != null)
                _overallApproval.style.color = unrest >= unrestWarning ? theme.danger : theme.primaryText;
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class DashboardController
    {
        /// <summary>
        /// GDD 14. What this group is unhappy about, worst first: the condition with
        /// the current reading in it, what it scores out of 100, and how heavily they
        /// weigh it. Three lines is enough to act on; the full list is the model's.
        /// </summary>
        void ExplainApproval(int group, string groupName, Vector3 panelPosition)
        {
            if (runner == null || runner.State == null || runner.Simulator == null) return;

            System.Collections.Generic.List<ApprovalDriver> drivers =
                runner.Simulator.Approval.Drivers(runner.State, runner.Policy, group);

            System.Text.StringBuilder body = new System.Text.StringBuilder();
            int shown = 0;
            foreach (ApprovalDriver driver in drivers)
            {
                if (shown >= 4) break;
                body.Append(shown == 0 ? "" : "\n")
                    .Append(driver.score < 40f ? "x  " : driver.score < 60f ? "-  " : "+  ")
                    .Append(driver.label)
                    .Append("  ").Append(driver.score.ToString("0")).Append("/100");
                shown++;
            }

            float[] values = Values(runner.State);
            string title = groupName + "  " + values[group].ToString("0") + "%"
                           + (drivers.Count > 0 && drivers[0].score < 50f ? "  -  worst: " + drivers[0].label : "");

            ShowMapTip(title, body.ToString(), ScreenFromPanel(panelPosition));
        }

        static float[] Values(EconomyState state)
        {
            ApprovalState a = state.approval;
            return new[] { a.poor, a.middle, a.wealthy, a.left, a.centre, a.right };
        }
    }
}
