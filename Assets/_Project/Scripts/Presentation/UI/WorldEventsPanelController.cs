using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 18: active events with severity and time remaining, the amber early-warning
    /// list, the war panel, diplomatic status per nation, and the history. Rows are
    /// EventAlertRow and NationStatusRow template instances, restamped each week.
    /// </summary>
    public class WorldEventsPanelController : DrawerPanelController
    {
        [Header("Templates")]
        [SerializeField] VisualTreeAsset eventAlertRowTemplate;
        [SerializeField] VisualTreeAsset nationStatusRowTemplate;

        [Tooltip("How many ended events the history list shows, newest first.")]
        [Range(1, 50)] [SerializeField] int historyShown = 12;

        protected override void Build() { }

        protected override void Refresh(EconomyState state)
        {
            EventSystem events = runner.Simulator.Events;

            VisualElement active = Clear("active-event-list");
            VisualElement warnings = Clear("warning-event-list");
            foreach (EventInstance e in state.events.live)
            {
                EventConfig cfg = events.Config.events[e.configIndex];
                string where = e.nationIndex >= 0 && e.nationIndex < events.Config.nations.Length
                    ? events.Config.nations[e.nationIndex].name : "";

                if (e.IsActive)
                    AddEvent(active, cfg.title, where + " - severity " + e.severity.ToString("0.00")
                             + (e.mitigation > 0f ? ", " + (e.mitigation * 100f).ToString("0") + "% absorbed by preparation" : ""),
                             "ACTIVE", e.WeeksRemaining + " wk left", theme == null ? Color.red : theme.danger);
                else
                    AddEvent(warnings, cfg.title, e.stage == EventWarningLevel.EarlySignal ? cfg.earlySignalHeadline : cfg.imminentAdvisorLine,
                             e.stage == EventWarningLevel.EarlySignal ? "SIGNAL" : "IMMINENT",
                             WarningLeft(e, cfg) + " wk", theme == null ? Color.yellow : theme.warning);
            }
            if (active != null && active.childCount == 0) AddEvent(active, "Nothing active", "", "", "", Color.grey);
            if (warnings != null && warnings.childCount == 0) AddEvent(warnings, "No warnings", "", "", "", Color.grey);

            RefreshWar(state);
            RefreshNations();

            VisualElement history = Clear("event-history-list");
            int shown = 0;
            for (int i = state.events.history.Count - 1; i >= 0 && shown < historyShown; i--, shown++)
            {
                EventRecord r = state.events.history[i];
                AddEvent(history, r.title, r.outcome, "ENDED", "year " + (r.endWeek / 52 + 1), theme == null ? Color.grey : theme.neutralData);
            }
        }

        static int WarningLeft(EventInstance e, EventConfig cfg)
        {
            return e.stage == EventWarningLevel.EarlySignal
                ? cfg.warningWeeks - e.weeksInStage
                : cfg.warningWeeks - cfg.earlySignalWeeks - e.weeksInStage;
        }

        void RefreshWar(EconomyState state)
        {
            WarState war = state.events.war;
            SetLabel("war-status", war.active ? "AT WAR - week " + war.weeks : "No active conflict");
            SetLabel("war-kia", "KIA " + war.kia.ToString("#,0"));
            SetLabel("war-equipment", "Equipment stock " + war.equipmentStock.ToString("0") + "%");
            SetLabel("war-infrastructure", "Infrastructure " + state.infrastructureHealth.ToString("0") + "%");
            SetLabel("war-veterans", "Veterans " + state.population.veterans.ToString("0.00") + "M");
            SetLabel("war-cost", "Cumulative war cost " + Billions(war.cumulativeCostBillions));
        }

        void RefreshNations()
        {
            VisualElement list = Clear("nation-status-list");
            if (list == null || nationStatusRowTemplate == null) return;
            GeopoliticsState g = runner.State.geopolitics;
            for (int i = 0; i < database.Nations.Length && i < g.nations.Count; i++)
            {
                NationDefinition nation = database.Nations[i];
                if (nation == null || nation.isPlayerNation) continue;
                NationState live = g.nations[i];
                VisualElement row = nationStatusRowTemplate.Instantiate();
                Set(row, "nation-name", nation.displayName);
                Set(row, "nation-archetype", nation.archetype.ToString());
                Set(row, "nation-relationship", "rel " + live.relationship.ToString("+0;-0"));
                Set(row, "nation-bond-holding", (live.bondHolding * 100f).ToString("0.0") + "% of debt"
                    + (live.buyingBonds ? "" : " - NOT BUYING"));
                Set(row, "nation-flag", live.sanctioningYou ? "SANCTIONS" : live.theirTariffOnYou > 0.5f ? "TARIFF " + live.theirTariffOnYou.ToString("0") + "%" : "");
                Set(row, "nation-trade", (nation.tradeVolumePercent * 100f).ToString("0") + "% of trade");
                VisualElement swatch = row.Q<VisualElement>("nation-colour");
                if (swatch != null) swatch.style.backgroundColor = nation.nationColor;
                list.Add(row);
            }
        }

        VisualElement Clear(string name)
        {
            VisualElement list = Root.Q<VisualElement>(name);
            if (list != null) list.Clear();
            return list;
        }

        void AddEvent(VisualElement list, string title, string detail, string stage, string time, Color strip)
        {
            if (list == null || eventAlertRowTemplate == null) return;
            VisualElement row = eventAlertRowTemplate.Instantiate();
            Set(row, "event-title", title);
            Set(row, "event-detail", detail);
            Set(row, "event-stage", stage);
            Set(row, "event-time", time);
            VisualElement severity = row.Q<VisualElement>("event-severity-strip");
            if (severity != null) severity.style.backgroundColor = strip;
            list.Add(row);
        }

        static void Set(VisualElement row, string name, string text)
        {
            Label label = row.Q<Label>(name);
            if (label != null) label.text = text;
        }
    }
}
