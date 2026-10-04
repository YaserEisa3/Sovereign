using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 19. The Chief Economic Advisor's messages, newest first, in three modes:
    /// full guidance, data only (warnings and dangers, no commentary), or silent -
    /// the hardcore setting, where you read the dashboard and nothing else.
    /// </summary>
    public class AdvisorPanelController : DrawerPanelController
    {
        public enum Mode { Full, DataOnly, Silent }

        [Header("Templates")]
        [SerializeField] VisualTreeAsset eventAlertRowTemplate;

        [SerializeField] Mode mode = Mode.Full;
        [Range(1, 60)] [SerializeField] int messagesShown = 20;

        protected override void Build()
        {
            BindMode("advisor-full", Mode.Full);
            BindMode("advisor-data", Mode.DataOnly);
            BindMode("advisor-silent", Mode.Silent);
        }

        void BindMode(string name, Mode value)
        {
            Button button = Root.Q<Button>(name);
            if (button == null) return;
            button.clicked += () => { mode = value; Refresh(runner.State); };
        }

        protected override void Refresh(EconomyState state)
        {
            foreach (string name in new[] { "advisor-full", "advisor-data", "advisor-silent" })
            {
                VisualElement tab = Root.Q<VisualElement>(name);
                if (tab == null) continue;
                bool selected = (name == "advisor-full" && mode == Mode.Full) || (name == "advisor-data" && mode == Mode.DataOnly)
                                || (name == "advisor-silent" && mode == Mode.Silent);
                if (selected) tab.AddToClassList("selected"); else tab.RemoveFromClassList("selected");
            }

            VisualElement list = Root.Q<VisualElement>("advisor-message-list");
            if (list == null || eventAlertRowTemplate == null) return;
            list.Clear();

            if (mode == Mode.Silent)
            {
                SetLabel("advisor-latest", "Silent. You are on your own.");
                return;
            }

            int shown = 0;
            string latest = "";
            for (int i = state.events.alerts.Count - 1; i >= 0 && shown < messagesShown; i--)
            {
                Alert alert = state.events.alerts[i];
                if (alert.channel != AlertChannel.Advisor) continue;
                if (mode == Mode.DataOnly && alert.level == AlertLevel.Info) continue;

                VisualElement row = eventAlertRowTemplate.Instantiate();
                Set(row, "event-title", alert.text);
                Set(row, "event-detail", "");
                Set(row, "event-stage", alert.level.ToString().ToUpperInvariant());
                Set(row, "event-time", "Q" + ((alert.week % 52) / 13 + 1) + " " + (state.StartYear + alert.week / 52));
                VisualElement strip = row.Q<VisualElement>("event-severity-strip");
                if (strip != null && theme != null)
                    strip.style.backgroundColor = alert.level == AlertLevel.Danger ? theme.danger
                                                : alert.level == AlertLevel.Warning ? theme.warning : theme.neutralData;
                list.Add(row);

                if (shown == 0) latest = alert.text;
                shown++;
            }

            SetLabel("advisor-latest", shown == 0 ? "Nothing to report. That will not last." : "Latest: " + latest);
        }

        static void Set(VisualElement row, string name, string text)
        {
            Label label = row.Q<Label>(name);
            if (label != null) label.text = text;
        }
    }
}
