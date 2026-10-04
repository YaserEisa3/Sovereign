using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 18. What is moving the three numbers a player steers by, in the box the
    /// line chart used to own. A chart says what happened; this says WHY, which is
    /// the question nobody could answer from the dashboard before.
    ///
    /// The rows are built once and rewritten weekly - a table rebuilt every tick
    /// cannot be hovered, measured or trusted to keep its place.
    /// </summary>
    public partial class DashboardController
    {
        class DriverSection
        {
            public Label value, note;
            public Label[] names, values, notes;
        }

        const int DriversShown = 3;

        VisualElement _driverContainer;
        Button _driversToggle;
        readonly DriverSection[] _driverSections = new DriverSection[3];
        bool _driverView = true;

        static readonly string[] DriverHeadings = { "REAL GDP GROWTH", "UNEMPLOYMENT", "REVENUE" };

        void BuildDrivers()
        {
            _driverContainer = _root.Q<VisualElement>("driver-container");
            _driversToggle = _root.Q<Button>("drivers-toggle");
            if (_driverContainer == null) return;

            _driverContainer.Clear();
            for (int s = 0; s < _driverSections.Length; s++)
            {
                VisualElement section = new VisualElement();
                section.AddToClassList("driver-section");

                VisualElement head = new VisualElement();
                head.AddToClassList("driver-head");
                head.Add(Text(DriverHeadings[s], "driver-head-name"));
                Label headValue = Text("", "driver-head-value");
                Label headNote = Text("", "driver-head-note");
                head.Add(headValue);
                head.Add(headNote);
                section.Add(head);

                DriverSection built = new DriverSection
                {
                    value = headValue,
                    note = headNote,
                    names = new Label[DriversShown],
                    values = new Label[DriversShown],
                    notes = new Label[DriversShown]
                };

                for (int r = 0; r < DriversShown; r++)
                {
                    VisualElement row = new VisualElement();
                    row.AddToClassList("driver-row");
                    built.names[r] = Text("", "driver-name");
                    built.values[r] = Text("", "driver-value");
                    built.notes[r] = Text("", "driver-note");
                    row.Add(built.names[r]);
                    row.Add(built.values[r]);
                    row.Add(built.notes[r]);
                    section.Add(row);
                }

                _driverSections[s] = built;
                _driverContainer.Add(section);
            }

            if (_driversToggle != null) _driversToggle.clicked += () => SetDriverView(true);
            SetDriverView(_driverView);
        }

        static Label Text(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        /// <summary>The table and the chart share one box. Pressing a series tab goes to
        /// the chart, pressing WHAT'S MOVING comes back here.</summary>
        void SetDriverView(bool on)
        {
            _driverView = on;
            if (on) { SetMeterView(false); SetIndustryView(false); }
            if (_driverContainer != null)
                _driverContainer.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;

            // Four views share one box and exactly one may be open, or the column grows
            // and pushes its own toggles out of reach.
            if (_chartContainer != null)
                _chartContainer.style.display = on || _meterView || _industryView
                    ? DisplayStyle.None : DisplayStyle.Flex;

            // The readout under the box describes the CHART's series, and says nothing
            // about a table of drivers.
            if (_chartReadout != null)
                _chartReadout.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
            if (_driversToggle != null)
            {
                if (on) _driversToggle.AddToClassList("selected");
                else _driversToggle.RemoveFromClassList("selected");
            }
            if (on && runner != null && runner.State != null) RefreshDrivers(runner.State);
        }
    }
}
