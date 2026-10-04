using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>The weekly refresh. Reads state, writes text - no economics here.</summary>
    public partial class DashboardController
    {
        void Refresh(EconomyState state)
        {
            if (_root == null || state == null) return;

            if (_date != null) _date.text = "Q" + state.Quarter + " " + state.Year;

            SetCard(0, "REAL GDP", state.realGdpGrowth.ToString("+0.0;-0.0") + "%",
                    "potential " + runner.Simulator.Config.potentialGrowthRate.ToString("0.0") + "%",
                    state.realGdpGrowth >= 0f);
            SetCard(1, "CPI", state.inflation.ToString("0.0") + "%",
                    "core " + state.coreInflation.ToString("0.0") + "%",
                    state.inflation <= 4f);
            SetCard(2, "UNEMPLOYMENT", state.unemployment.ToString("0.0") + "%",
                    "part. " + state.participationRate.ToString("0") + "%",
                    state.unemployment <= 7f);
            SetCard(3, "DEBT / GDP", (state.DebtToGdp * 100f).ToString("0") + "%",
                    "balance " + state.budgetBalancePercentGdp.ToString("+0.0;-0.0") + "%",
                    state.DebtToGdp < 1.2f);
            SetCard(4, "CURRENCY", state.currency.exchangeRateIndex.ToString("0.0"),
                    "reserves $" + state.currency.fxReservesBillions.ToString("0") + "B",
                    state.currency.exchangeRateIndex >= 90f);
            SetCard(5, "INFRASTRUCTURE", state.infrastructureHealth.ToString("0") + "%",
                    state.infrastructureHealth < 70f ? "dragging on growth" : "above the drag line",
                    state.infrastructureHealth >= 70f);

            // What comes in and what goes out, in money rather than in ratios - the two
            // numbers every other fiscal figure on this screen is made of.
            float revenueShare = state.revenueBillions / Mathf.Max(1f, state.nominalGdpBillions) * 100f;
            float spendingShare = state.spendingBillions / Mathf.Max(1f, state.nominalGdpBillions) * 100f;
            SetCard(6, "REVENUE", "$" + state.revenueBillions.ToString("#,0") + "B",
                    revenueShare.ToString("0.0") + "% of GDP",
                    state.revenueBillions >= state.spendingBillions);
            SetCard(7, "SPENDING", "$" + state.spendingBillions.ToString("#,0") + "B",
                    "debt service $" + state.bonds.AnnualDebtServiceBillions.ToString("#,0") + "B",
                    state.spendingBillions <= state.revenueBillions);

            // The ratio tells you how heavy the debt is; this is what you actually owe.
            SetCard(8, "TOTAL DEBT", "$" + state.bonds.debtStockBillions.ToString("#,0") + "B",
                    (state.bonds.foreignHoldingPercent * 100f).ToString("0") + "% owed abroad",
                    state.bonds.foreignHoldingPercent < 0.4f);

            // What the debt COSTS: the blended rate on the stock, against what the market
            // charges today. The gap between them is the whole fiscal problem - old cheap
            // paper repricing onto new dear paper as it matures.
            SetCard(10, "AVG DEBT RATE", state.bonds.averageCoupon.ToString("0.00") + "%",
                    "market " + state.bonds.yields.mediumTermYield.ToString("0.0") + "%",
                    state.bonds.averageCoupon >= state.bonds.yields.mediumTermYield);

            // And this year's gap - what the debt grows BY, which is the number the
            // player can actually do something about this quarter.
            float gap = state.revenueBillions - state.spendingBillions;
            SetCard(9, gap >= 0f ? "SURPLUS" : "DEFICIT",
                    "$" + Mathf.Abs(gap).ToString("#,0") + "B",
                    state.budgetBalancePercentGdp.ToString("+0.0;-0.0") + "% of GDP",
                    gap >= 0f);

            RefreshBonds(state);
            RefreshApproval(state);
            RefreshEvents(state);
            RefreshChart(state);
            RefreshMeters(state);
            RefreshDrivers(state);
            RefreshGoal(state);
            if (_breakdown != null) _breakdown.Refresh(state);
            if (_spendingBreakdown != null) _spendingBreakdown.Refresh(state);
            RefreshIndustry(state);

            if (_mapStatus != null)
                _mapStatus.text = "6 trade partners - 0 active conflicts - week " + state.week;

        }

        void SetCard(int index, string label, string value, string delta, bool healthy)
        {
            if (index >= _cards.Length) return;
            KpiCard card = _cards[index];
            if (card.label != null) card.label.text = label;
            if (card.value != null) card.value.text = value;
            if (card.delta == null) return;

            card.delta.text = delta;
            card.delta.style.color = theme == null
                ? (healthy ? Color.green : Color.red)
                : (healthy ? theme.growth : theme.warning);
        }

        void RefreshBonds(EconomyState state)
        {
            YieldCurve curve = state.bonds.yields;

            if (_curveRow != null)
            {
                // The model carries three maturities; the five points shown are
                // interpolated across them, which is what a real curve display does
                // between its liquid benchmarks.
                SetCurvePoint("3M", curve.shortTermYield);
                SetCurvePoint("1Y", Mathf.Lerp(curve.shortTermYield, curve.mediumTermYield, 0.25f));
                SetCurvePoint("5Y", curve.mediumTermYield);
                SetCurvePoint("10Y", Mathf.Lerp(curve.mediumTermYield, curve.longTermYield, 0.5f));
                SetCurvePoint("30Y", curve.longTermYield);
            }

            if (_inversionFlag != null)
                _inversionFlag.text = curve.IsInverted ? "INVERTED" : "";

            if (_foreignHoldings != null)
                _foreignHoldings.text = "Foreign holdings " + (state.bonds.foreignHoldingPercent * 100f).ToString("0") + "%";

            if (_creditRating != null)
                _creditRating.text = "Rating " + state.bonds.creditRating
                                     + (state.bonds.ratingWatchNegative ? " (watch negative)" : "");

            if (_marketConfidence != null)
                _marketConfidence.text = "Market confidence " + state.monetary.MarketConfidence.ToString("0");
        }

        /// <summary>The curve labels are authored in the UXML as "3M 4.10" and rewritten
        /// in place, so the layout stays the designer's.</summary>
        void SetCurvePoint(string maturity, float yield)
        {
            foreach (VisualElement child in _curveRow.Children())
            {
                foreach (VisualElement point in child.Children())
                {
                    Label label = point as Label;
                    if (label == null || !label.text.StartsWith(maturity)) continue;
                    label.text = maturity + " " + yield.ToString("0.00");
                    return;
                }
            }
        }

        int _hoverIndex = -1;

        /// <summary>Hovering the chart rewrites the readout to that week; leaving restores it.</summary>
        void OnChartHover(int index, float value)
        {
            _hoverIndex = index;
            if (_chartReadout == null || runner == null || runner.State == null) return;
            if (index < 0) { RefreshChart(runner.State); return; }

            int week = runner.State.week - (_chart.PointCount - 1 - index);
            _chartReadout.text = _chartSeries + "  " + value.ToString("0.00") + ChartUnit(_chartSeries)
                                 + "   " + Quarter(runner.State, week)
                                 + "   (" + (runner.State.week - week) + " weeks ago)";
        }

        void RefreshChart(EconomyState state)
        {
            if (_chart == null) return;
            if (_hoverIndex >= 0) { _chart.SetValues(state.Series(_chartSeries).Values, 520); return; }

            TimeSeries series = state.Series(_chartSeries);

            // The scale reads in the series' own unit, and the bottom axis in game dates.
            string unit = ChartUnit(_chartSeries);
            int firstWeek = state.week - (Mathf.Min(series.Count, 520) - 1);
            _chart.FormatValue = v => v.ToString(Mathf.Abs(v) >= 100f ? "0" : "0.0") + unit;
            _chart.FormatDate = i => Quarter(state, firstWeek + i);
            _chart.SetValues(series.Values, 520);

            if (_chartReadout == null) return;
            _chartReadout.text = _chartSeries + "  now " + series.Latest.ToString("0.00") + unit
                                 + "   1yr avg " + series.Average(52).ToString("0.00") + unit
                                 + "   " + series.Count + " weeks recorded";
        }

        static string Quarter(EconomyState state, int week)
        {
            return "Q" + ((week % 52) / 13 + 1) + " " + (state.StartYear + week / 52);
        }

        /// <summary>What the numbers on a series mean, so the scale is readable without
        /// knowing which series is showing.</summary>
        static string ChartUnit(string series)
        {
            switch (series)
            {
                case "population": return "M";
                case "fxReserves": return "$B";
                case "currency":
                case "averageWage":
                case "realWage":
                case "confidence":
                case "investment":
                case "infrastructure":
                case "marketConfidence":
                case "gini":
                case "tradeVolume": return "";
                default: return "%";   // growth, inflation, unemployment, debt, yields, approval
            }
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class DashboardController
    {
        /// <summary>
        /// GDD 20, forever mode. Where the country STANDS, not how far it is from an
        /// ending - there isn't one. A rung of the ladder it has yet to climb, how many
        /// it holds, and when it is prosperous, how long it has managed to stay that way.
        /// </summary>
        void RefreshGoal(EconomyState state)
        {
            Label goal = _root.Q<Label>("goal-line");
            if (goal == null || runner == null || runner.Simulator == null) return;

            VictoryModel victory = runner.Simulator.Victory;
            if (!victory.Enabled) { goal.text = ""; return; }

            int held = MilestoneModel.Count(state);
            string standing = held + " of " + MilestoneModel.Ladder.Length + " milestones";

            if (state.prosperity)
            {
                goal.text = "PROSPEROUS - held " + Years(state.prosperityWeeks) + "   (best "
                            + Years(state.longestProsperityWeeks) + ", reached "
                            + state.timesProsperous + (state.timesProsperous == 1 ? " time)" : " times)")
                            + "   " + standing;
                return;
            }

            string next = MilestoneModel.Next(state);
            string chasing = next.Length > 0 ? "NEXT: " + next : "PROSPERITY: " + victory.Outstanding(state);

            // Once every rung is held, the only thing left to chase is prosperity itself,
            // and after it has been held once, getting back there.
            if (state.timesProsperous > 0)
                chasing += "   (prosperous " + state.timesProsperous
                           + (state.timesProsperous == 1 ? " time, best " : " times, best ")
                           + Years(state.longestProsperityWeeks) + ")";

            goal.text = chasing + "   " + standing;
        }

        static string Years(int weeks)
        {
            return weeks >= 52 ? (weeks / 52f).ToString("0.0") + " years"
                 : weeks + (weeks == 1 ? " week" : " weeks");
        }
    }
}
