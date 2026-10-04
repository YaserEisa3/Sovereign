using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>The weekly rewrite of the driver table. Reads the board the model filled
    /// as it computed the figures; invents nothing of its own.</summary>
    public partial class DashboardController
    {
        void RefreshDrivers(EconomyState state)
        {
            if (!_driverView || _driverSections[0] == null) return;
            DriverBoard board = state.drivers;

            // Growth. The headline reading is a slow average of where growth is HEADING,
            // which is why a policy change can look as if it did nothing for months - so
            // the target is on the line beside it.
            Fill(_driverSections[0],
                 state.realGdpGrowth.ToString("+0.00;-0.00") + "%",
                 "heading to " + board.growthTarget.ToString("+0.0;-0.0")
                 + "%, trend is " + board.growthTrend.ToString("0.0") + "%",
                 DriverBoard.Top(board.growth, DriversShown), "", 0.01f, false);

            // Unemployment, as a rate per year: a week's movement is far too small to
            // read, and a rate is what the player is deciding about. The unit lives in
            // the headline note so it does not have to be repeated down every row -
            // "pts/yr" on three lines was jargon three times over.
            float net = 0f;
            for (int i = 0; i < board.unemployment.Count; i++) net += board.unemployment[i].points;
            Fill(_driverSections[1],
                 state.unemployment.ToString("0.0") + "%",
                 Mathf.Abs(net) < 0.05f ? "holding steady"
                 : (net < 0f ? "falling " : "rising ") + Mathf.Abs(net).ToString("0.0") + " a year at this rate",
                 DriverBoard.Top(board.unemployment, DriversShown), "", 0.01f, true);

            // Revenue, against a year ago. A level cannot be explained by its own parts -
            // the breakdown chart below already shows those - so what is worth saying is
            // which lines MOVED, and whether it was the rate or the base that moved them.
            List<Driver> revenue = RevenueDrivers(state);
            TimeSeries history = state.Recorded(EconomySimulator.RevenueSeriesPrefix + "TOTAL");
            float yearAgo = history != null && history.Count > 52 ? history.Ago(52) : 0f;
            Fill(_driverSections[2],
                 "$" + state.revenueBillions.ToString("#,0") + "B",
                 yearAgo > 0f
                     ? (state.revenueBillions - yearAgo).ToString("+$#,0;-$#,0") + "B on a year ago"
                     : "first year - nothing to compare against yet",
                 DriverBoard.Top(revenue, DriversShown), "$B", 0.5f, false);
        }

        /// <summary>Green means the player is being helped, which is not the same as
        /// a positive number: a driver pushing UNEMPLOYMENT down is doing them a favour.
        /// Colouring by sign alone had the whole unemployment section backwards.</summary>
        void Fill(DriverSection section, string value, string note, List<Driver> drivers, string unit,
                  float flatBelow, bool lowerIsBetter)
        {
            section.value.text = value;
            section.note.text = note;

            for (int r = 0; r < DriversShown; r++)
            {
                bool has = r < drivers.Count;
                section.names[r].text = has ? drivers[r].label : "";
                section.notes[r].text = has ? drivers[r].note : "";

                Label amount = section.values[r];
                amount.RemoveFromClassList("helping");
                amount.RemoveFromClassList("hurting");
                amount.RemoveFromClassList("flat");
                if (!has) { amount.text = ""; continue; }

                float points = drivers[r].points;
                amount.text = unit == "$B"
                    ? points.ToString("+$#,0;-$#,0") + "B"
                    : points.ToString("+0.00;-0.00");
                bool helping = lowerIsBetter ? points < 0f : points > 0f;
                amount.AddToClassList(Mathf.Abs(points) < flatBelow ? "flat" : helping ? "helping" : "hurting");
            }
        }

        /// <summary>
        /// What moved each revenue line over the last year, and whether it moved because
        /// the player changed the rate or because the base under it changed. Built here
        /// rather than in the model because only the presentation layer knows what a tax
        /// is CALLED - and it shares the breakdown chart's name map, so a line cannot be
        /// called one thing in the table and another in the chart below it.
        /// </summary>
        List<Driver> RevenueDrivers(EconomyState state)
        {
            List<Driver> drivers = new List<Driver>();

            foreach (KeyValuePair<string, float> line in state.treasury.revenueByLine)
            {
                TimeSeries take = state.Recorded(EconomySimulator.RevenueSeriesPrefix + line.Key);
                if (take == null || take.Count <= 52) continue;

                float change = line.Value - take.Ago(52);
                if (Mathf.Abs(change) < 0.5f) continue;

                TimeSeries rates = state.Recorded(EconomySimulator.RateSeriesPrefix + line.Key);
                float rateNow = rates == null ? 0f : rates.Latest;
                float rateThen = rates == null ? 0f : rates.Ago(52);
                bool youMovedIt = Mathf.Abs(rateNow - rateThen) > 0.01f;

                string why = youMovedIt
                    ? "you set it to " + rateNow.ToString("0.0") + "%, from " + rateThen.ToString("0.0") + "%"
                    : "same rate - the base " + (change > 0f ? "grew under it" : "shrank under it");

                drivers.Add(new Driver
                {
                    label = _breakdown == null ? line.Key : _breakdown.NameFor(line.Key),
                    points = change,
                    note = why
                });
            }

            return drivers;
        }
    }
}
