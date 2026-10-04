using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 6 and 18. How each industry is doing, on the front page. The Sectors tab
    /// used to plot business investment - one number for the whole economy - under a
    /// name that promised seven. It now shows the sectors themselves: a health bar
    /// each, their share of GDP and what they employ.
    /// </summary>
    public partial class DashboardController
    {
        VisualElement _industryContainer;
        readonly List<VisualElement> _industryRows = new List<VisualElement>();
        bool _industryView;

        void BuildIndustry()
        {
            _industryContainer = _root.Q<VisualElement>("industry-container");
            if (_industryContainer != null) _industryContainer.style.display = DisplayStyle.None;
        }

        /// <summary>The Sectors tab swaps the chart for the industry list, and any other
        /// tab swaps it back.</summary>
        void SetIndustryView(bool industry)
        {
            // Three views share one box, and exactly one of them may be open: with two
            // showing, the column grew and the view toggle ended up underneath a meter.
            _industryView = industry;
            if (industry) SetDriverView(false);
            if (industry) _meterView = false;

            if (_industryContainer != null)
                _industryContainer.style.display = industry ? DisplayStyle.Flex : DisplayStyle.None;
            if (_meterContainer != null && industry) _meterContainer.style.display = DisplayStyle.None;
            if (_chartContainer != null)
                _chartContainer.style.display = industry || _meterView ? DisplayStyle.None : DisplayStyle.Flex;
            if (_viewToggle != null && industry) _viewToggle.text = "ALL METERS";

            if (runner != null && runner.State != null) RefreshIndustry(runner.State);
        }

        void RefreshIndustry(EconomyState state)
        {
            if (!_industryView || _industryContainer == null || breakdownRowTemplate == null) return;

            while (_industryRows.Count < state.sectors.Count)
            {
                VisualElement row = breakdownRowTemplate.Instantiate();
                VisualElement content = row.childCount > 0 ? row[0] : row;
                content.AddToClassList("breakdown-row");

                int index = _industryRows.Count;
                content.RegisterCallback<PointerEnterEvent>(e => ExplainIndustry(index, e.position));
                content.RegisterCallback<PointerLeaveEvent>(e => HideMeterTip());

                _industryContainer.Add(row);
                _industryRows.Add(row);
            }

            for (int i = 0; i < _industryRows.Count; i++)
            {
                VisualElement row = _industryRows[i];
                bool used = i < state.sectors.Count;
                row.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (!used) continue;

                EconomicSector sector = state.sectors[i];
                Label name = row.Q<Label>("breakdown-name");
                Label value = row.Q<Label>("breakdown-value");
                Label share = row.Q<Label>("breakdown-share");
                VisualElement fill = row.Q<VisualElement>("breakdown-fill");

                if (name != null) name.text = sector.name;
                // The bar is HEALTH - the thing a sector can be good or bad at - with
                // its size in the economy beside it.
                if (value != null) value.text = "health " + sector.health.ToString("0");
                if (share != null) share.text = (sector.gdpShare * 100f).ToString("0.0") + "%";
                if (fill != null)
                {
                    fill.style.width = Length.Percent(Mathf.Clamp(sector.health, 0f, 100f));
                    if (theme != null)
                        fill.style.backgroundColor = sector.health >= 60f ? theme.growth
                                                   : sector.health >= 40f ? theme.warning : theme.danger;
                }
            }
        }

        void ExplainIndustry(int index, Vector3 panelPosition)
        {
            if (runner == null || runner.State == null) return;
            if (index < 0 || index >= runner.State.sectors.Count) return;

            EconomicSector sector = runner.State.sectors[index];
            string body = sector.headcount.ToString("0.0") + "M employed at "
                        + (sector.averageWage / 1000f).ToString("0") + "k average, wage bill "
                        + "$" + sector.WageBillBillions.ToString("#,0") + "B/yr.\n"
                        + "Exports " + (sector.exportShare * 100f).ToString("0") + "% of output, "
                        + "foreign competition " + sector.foreignCompetition.ToString("0") + "/100, "
                        + "regulation burden " + sector.regulationBurden.ToString("0") + "/100.";

            ShowMapTip(sector.name + "  health " + sector.health.ToString("0"), body, ScreenFromPanel(panelPosition));
        }
    }
}
