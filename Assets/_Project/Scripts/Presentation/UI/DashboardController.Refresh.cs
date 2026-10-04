using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>The buttons: speed, chart tabs and the drawer bar.</summary>
    public partial class DashboardController
    {
        void BindButtons()
        {
            BindSpeed("speed-pause", GameSpeed.Paused);
            BindSpeed("speed-1x", GameSpeed.Normal);
            BindSpeed("speed-2x", GameSpeed.Double);
            BindSpeed("speed-4x", GameSpeed.Quadruple);

            BindChartTab("tab-gdp", "gdpGrowth", 0f);
            BindChartTab("tab-inflation", "inflation", 2f);
            BindChartTab("tab-unemployment", "unemployment", 4.5f);
            BindChartTab("tab-debt", "debtToGdp", 120f);
            BindChartTab("tab-trade", "currentAccount", 0f);
            BindChartTab("tab-fx", "currency", 100f);
            BindChartTab("tab-approval", "approvalOverall", 40f);
            BindChartTab("tab-sectors", "investment", 50f);   // the tab body is the industry list below
            BindChartTab("tab-population", "population", float.NaN);

            BindDrawer("open-fiscal", panelFiscal);
            BindDrawer("open-monetary", panelMonetaryBonds);
            BindDrawer("open-trade", panelTradeCurrency);
            BindDrawer("open-regulatory", panelRegulatory);
            BindDrawer("open-population", panelPopulation);
            BindDrawer("open-events", panelWorldEvents);
            BindDrawer("open-advisor", panelAdvisor);
        }

        void BindSpeed(string elementName, GameSpeed value)
        {
            Button button = _root.Q<Button>(elementName);
            if (button == null) return;
            button.clicked += () =>
            {
                if (runner != null) runner.Speed = value;
                HighlightSpeed();
            };
        }

        void HighlightSpeed()
        {
            if (runner == null) return;
            SetSelected("speed-pause", runner.Speed == GameSpeed.Paused);
            SetSelected("speed-1x", runner.Speed == GameSpeed.Normal);
            SetSelected("speed-2x", runner.Speed == GameSpeed.Double);
            SetSelected("speed-4x", runner.Speed == GameSpeed.Quadruple);
        }

        void SetSelected(string elementName, bool selected)
        {
            VisualElement element = _root.Q<VisualElement>(elementName);
            if (element == null) return;
            if (selected) element.AddToClassList("selected");
            else element.RemoveFromClassList("selected");
        }

        void BindChartTab(string elementName, string series, float threshold)
        {
            Button button = _root.Q<Button>(elementName);
            if (button == null) return;
            button.clicked += () =>
            {
                // One tab shows the industries themselves rather than a series.
                SetIndustryView(elementName == "tab-sectors");
                _chartSeries = series;
                if (_chart != null) _chart.threshold = threshold;
                foreach (string tab in new[]
                {
                    "tab-gdp", "tab-inflation", "tab-unemployment", "tab-debt", "tab-trade",
                    "tab-fx", "tab-approval", "tab-sectors", "tab-population"
                })
                    SetSelected(tab, tab == elementName);

                if (runner != null && runner.State != null) Refresh(runner.State);
            };
        }

        void BindDrawer(string elementName, GameObject panel)
        {
            Button button = _root.Q<Button>(elementName);
            if (button == null || panel == null) return;

            button.clicked += () =>
            {
                bool opening = !panel.activeSelf;
                CloseAllPanels();
                panel.SetActive(opening);
            };
        }

        void CloseAllPanels()
        {
            foreach (GameObject panel in new[]
            {
                panelFiscal, panelMonetaryBonds, panelTradeCurrency, panelRegulatory,
                panelPopulation, panelWorldEvents, panelAdvisor
            })
                if (panel != null) panel.SetActive(false);
        }

    }
}
