using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>GDD 17: the alert bell and the news ticker. The ticker is stamped from
    /// NewsTickerItem.uxml with the newest headlines; the bell counts what is live
    /// and opens World Events.</summary>
    public partial class DashboardController
    {
        [Header("News")]
        [Tooltip("NewsTickerItem.uxml - one per headline.")]
        [SerializeField] VisualTreeAsset newsTickerItemTemplate;
        [Tooltip("How many headlines the ticker shows at once, newest first.")]
        [Range(1, 8)] [SerializeField] int tickerHeadlines = 3;

        int _tickerAlertStamp = -1;

        void BindAlertBell()
        {
            Button bell = _root.Q<Button>("alert-bell");
            if (bell == null || panelWorldEvents == null) return;
            bell.clicked += () =>
            {
                bool opening = !panelWorldEvents.activeSelf;
                CloseAllPanels();
                panelWorldEvents.SetActive(opening);
            };
        }

        void RefreshEvents(EconomyState state)
        {
            EventState ev = state.events;

            Label count = _root.Q<Label>("alert-count");
            if (count != null)
            {
                count.text = ev.live.Count.ToString();
                if (theme != null) count.style.color = ev.ActiveCount > 0 ? theme.danger : ev.WarningCount > 0 ? theme.warning : theme.mutedText;
            }

            if (_mapStatus != null)
                _mapStatus.text = "6 trade partners - " + ev.ActiveCount + " active events - " + ev.WarningCount + " warnings"
                                  + (ev.war.active ? " - AT WAR" : "");

            // Only restamp when something new has been said.
            int stamp = ev.alerts.Count == 0 ? 0 : ev.alerts[ev.alerts.Count - 1].week * 1000 + ev.alerts.Count;
            if (stamp == _tickerAlertStamp) return;
            _tickerAlertStamp = stamp;

            VisualElement track = _root.Q<VisualElement>("ticker-track");
            if (track == null || newsTickerItemTemplate == null) return;
            track.Clear();

            int shown = 0;
            for (int i = ev.alerts.Count - 1; i >= 0 && shown < tickerHeadlines; i--)
            {
                Alert alert = ev.alerts[i];
                if (alert.channel != AlertChannel.Ticker) continue;

                VisualElement item = newsTickerItemTemplate.Instantiate();
                Label text = item.Q<Label>("ticker-text");
                if (text != null)
                {
                    text.text = alert.text;
                    if (theme != null && alert.level == AlertLevel.Danger) text.style.color = theme.danger;
                    else if (theme != null && alert.level == AlertLevel.Warning) text.style.color = theme.warning;
                }
                track.Add(item);
                shown++;
            }

            if (shown == 0)
            {
                VisualElement item = newsTickerItemTemplate.Instantiate();
                Label text = item.Q<Label>("ticker-text");
                if (text != null) text.text = "Markets quiet. Q" + state.Quarter + " " + state.Year + ".";
                track.Add(item);
            }
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class DashboardController
    {
        void BindSaveLoad()
        {
            if (saveLoad == null) return;
            UnityEngine.UIElements.Button save = _root.Q<UnityEngine.UIElements.Button>("save-game");
            UnityEngine.UIElements.Button load = _root.Q<UnityEngine.UIElements.Button>("load-game");
            if (save != null) save.clicked += () => saveLoad.Save();
            if (load != null) load.clicked += () => saveLoad.Load();
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class DashboardController
    {
        /// <summary>GDD 18: the map zooms with the wheel, and with these for anyone who
        /// would rather press a button.</summary>
        void BindMapControls()
        {
            if (mapCamera == null) return;
            Bind("map-zoom-in", () => mapCamera.Zoom(1f));
            Bind("map-zoom-out", () => mapCamera.Zoom(-1f));
            Bind("map-reset", mapCamera.ResetView);
        }

        void Bind(string name, System.Action action)
        {
            UnityEngine.UIElements.Button button = _root.Q<UnityEngine.UIElements.Button>(name);
            if (button != null) button.clicked += () => action();
        }
    }
}
