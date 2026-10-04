using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 18. The second way to read the same numbers: every indicator as a bar,
    /// all of them at once. The line chart answers "what has this one been doing?";
    /// the meters answer "what shape is the country in?" - which is the question you
    /// are actually asking when a crisis lands.
    /// </summary>
    public partial class DashboardController
    {
        class Meter
        {
            public MeterParameters.Entry entry;
            public VisualElement fill, target;
            public Label value, note;
        }

        RevenueBreakdown _breakdown, _spendingBreakdown;
        readonly List<Meter> _meters = new List<Meter>();
        VisualElement _meterContainer, _chartContainer, _tooltip;
        Label _tooltipTitle, _tooltipBody;
        Button _viewToggle;
        bool _meterView;

        void BuildMeters()
        {
            _chartContainer = _root.Q<VisualElement>("chart-container");
            _meterContainer = _root.Q<VisualElement>("meter-container");
            _viewToggle = _root.Q<Button>("view-toggle");
            _tooltip = _root.Q<VisualElement>("meter-tooltip");
            _tooltipTitle = _root.Q<Label>("tooltip-title");
            _tooltipBody = _root.Q<Label>("tooltip-body");

            _meters.Clear();
            if (_meterContainer != null && meterRowTemplate != null && meterParameters != null)
            {
                _meterContainer.Clear();
                foreach (MeterParameters.Entry entry in meterParameters.entries)
                {
                    VisualElement row = meterRowTemplate.Instantiate();
                    VisualElement content = row.childCount > 0 ? row[0] : row;
                    content.AddToClassList("meter-column");
                    // Instantiate wraps the template in a TemplateContainer, and the wrapper
                    // is what the row lays out - without this every column is its own width.
                    row.style.flexGrow = 1f;
                    row.style.flexBasis = 0f;
                    row.style.minWidth = 0f;

                    Label name = content.Q<Label>("meter-name");
                    if (name != null) name.text = entry.label;

                    Meter meter = new Meter
                    {
                        entry = entry,
                        fill = content.Q<VisualElement>("meter-fill"),
                        target = content.Q<VisualElement>("meter-target"),
                        value = content.Q<Label>("meter-value"),
                        note = content.Q<Label>("meter-note")
                    };
                    _meters.Add(meter);

                    // A bar with no legend is a decoration. Hovering says what it means.
                    content.RegisterCallback<PointerEnterEvent>(e => ShowMeterTip(meter, e.position));
                    content.RegisterCallback<PointerMoveEvent>(e => PlaceTip(e.position));
                    content.RegisterCallback<PointerLeaveEvent>(e => HideMeterTip());
                    _meterContainer.Add(row);
                }
            }

            if (_viewToggle != null) _viewToggle.clicked += () => SetMeterView(!_meterView);
            SetMeterView(_meterView);
        }

        /// <summary>Swaps the two views. The chart tabs stay visible, because they are
        /// what you press to go back to a single series.</summary>
        void SetMeterView(bool meters)
        {
            _meterView = meters;
            // The meters and the industry list share the chart's box; opening one closes
            // the other, or the panel grows and swallows its own toggle.
            if (meters) SetIndustryView(false);
            if (_chartContainer != null) _chartContainer.style.display = meters ? DisplayStyle.None : DisplayStyle.Flex;
            if (_meterContainer != null) _meterContainer.style.display = meters ? DisplayStyle.Flex : DisplayStyle.None;
            if (_viewToggle != null)
            {
                _viewToggle.text = meters ? "SINGLE CHART" : "ALL METERS";
                if (meters) _viewToggle.AddToClassList("selected"); else _viewToggle.RemoveFromClassList("selected");
            }
            if (runner != null && runner.State != null) RefreshMeters(runner.State);
        }

        void RefreshMeters(EconomyState state)
        {
            if (!_meterView) return;

            foreach (Meter meter in _meters)
            {
                MeterParameters.Entry e = meter.entry;
                float value = state.Series(e.series).Latest;
                float span = Mathf.Max(0.0001f, e.max - e.min);
                float fraction = Mathf.Clamp01((value - e.min) / span);

                if (meter.fill != null)
                {
                    // The column fills from the bottom, so the reading is its height.
                    meter.fill.style.height = Length.Percent(fraction * 100f);
                    meter.fill.RemoveFromClassList("good");
                    meter.fill.RemoveFromClassList("warn");
                    meter.fill.RemoveFromClassList("bad");
                    meter.fill.AddToClassList(Health(e, value));
                }

                if (meter.target != null)
                    meter.target.style.bottom = Length.Percent(Mathf.Clamp01((e.target - e.min) / span) * 100f);

                if (meter.value != null) meter.value.text = value.ToString("F" + e.decimals) + e.unit;

                if (meter.note != null)
                {
                    // A year of context, so a bar that looks fine can still read "falling".
                    float ago = state.Series(e.series).Ago(52);
                    float change = value - ago;
                    meter.note.text = state.Series(e.series).Count < 8
                        ? ""
                        : (change >= 0f ? "+" : "") + change.ToString("F" + e.decimals) + " /yr";
                }
            }
        }

        /// <summary>Good, warning or bad, judged either by distance from the target or
        /// by which side of it the reading sits.</summary>
        static string Health(MeterParameters.Entry e, float value)
        {
            float tolerance = Mathf.Max(0.0001f, e.tolerance);
            float distance = e.judgeByDistance
                ? Mathf.Abs(value - e.target)
                : (e.higherIsBetter ? e.target - value : value - e.target);

            if (distance <= tolerance) return "good";
            return distance <= tolerance * 2f ? "warn" : "bad";
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class DashboardController
    {
        /// <summary>GDD 18: hovering a meter explains the indicator, and says where this
        /// reading sits against the one it is judged by.</summary>
        void ShowMeterTip(object meterObject, Vector3 position)
        {
            Meter meter = meterObject as Meter;
            if (meter == null || _tooltip == null || runner == null || runner.State == null) return;

            MeterParameters.Entry e = meter.entry;
            float value = runner.State.Series(e.series).Latest;

            if (_tooltipTitle != null)
                _tooltipTitle.text = e.label + "   " + value.ToString("F" + e.decimals) + e.unit
                                     + "   (aiming at " + e.target.ToString("F" + e.decimals) + e.unit + ")";
            if (_tooltipBody != null) _tooltipBody.text = e.explanation;

            _tooltip.style.display = DisplayStyle.Flex;
            PlaceTip(position);
        }

        /// <summary>Keeps the tip beside the pointer and inside the screen.</summary>
        void PlaceTip(Vector3 position)
        {
            if (_tooltip == null || _tooltip.style.display == DisplayStyle.None) return;

            float width = _tooltip.resolvedStyle.width > 1f ? _tooltip.resolvedStyle.width : 330f;
            float height = _tooltip.resolvedStyle.height > 1f ? _tooltip.resolvedStyle.height : 80f;
            float maxX = _root.resolvedStyle.width - width - 8f;
            float maxY = _root.resolvedStyle.height - height - 8f;

            _tooltip.style.left = Mathf.Clamp(position.x + 14f, 8f, Mathf.Max(8f, maxX));
            _tooltip.style.top = Mathf.Clamp(position.y + 16f, 8f, Mathf.Max(8f, maxY));
        }

        void HideMeterTip()
        {
            if (_tooltip != null) _tooltip.style.display = DisplayStyle.None;
        }

        /// <summary>The smoke test reads what a hover would show.</summary>
        public string MeterTipFor(int index)
        {
            if (index < 0 || index >= _meters.Count) return "";
            return _meters[index].entry.label + ": " + _meters[index].entry.explanation;
        }

        public int MeterCount { get { return _meters.Count; } }
    }
}

namespace Sovereign.Presentation
{
    public partial class DashboardController
    {
        /// <summary>GDD 18: the map's own tooltip, for a building under the pointer. It
        /// borrows the meter tooltip, because only one of them can ever be showing.</summary>
        public void ShowMapTip(string title, string body, Vector3 screenPosition)
        {
            if (_tooltip == null) return;
            if (_tooltipTitle != null) _tooltipTitle.text = title;
            if (_tooltipBody != null) _tooltipBody.text = body;
            _tooltip.style.display = DisplayStyle.Flex;
            MoveMapTip(screenPosition);
        }

        /// <summary>Screen space has Y up from the bottom; the panel has it down from
        /// the top. Getting that backwards pins every map tooltip to the wrong corner.</summary>
        public void MoveMapTip(Vector3 screenPosition)
        {
            PlaceTip(new Vector3(screenPosition.x, Screen.height - screenPosition.y, 0f));
        }

        public void HideMapTip() { HideMeterTip(); }

        /// <summary>ShowMapTip takes SCREEN coordinates (Y up from the bottom) because the
        /// map probe works in them; a UI pointer event is already panel space, so it has
        /// to be flipped back before it is flipped again.</summary>
        static Vector3 ScreenFromPanel(Vector3 panelPosition)
        {
            return new Vector3(panelPosition.x, Screen.height - panelPosition.y, 0f);
        }

        /// <summary>What the map tooltip is saying, for the tests.</summary>
        public string MapTipText
        {
            get
            {
                if (_tooltip == null || _tooltip.style.display == DisplayStyle.None) return "";
                return (_tooltipTitle == null ? "" : _tooltipTitle.text) + ": "
                     + (_tooltipBody == null ? "" : _tooltipBody.text);
            }
        }
    }
}
