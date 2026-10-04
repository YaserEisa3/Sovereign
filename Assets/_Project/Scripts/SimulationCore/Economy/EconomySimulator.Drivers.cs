using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// What is moving revenue. Growth and unemployment are decomposed where they are
    /// computed, because they are sums and the sum can be built from the record. A
    /// revenue LEVEL cannot be explained that way - the lines of a budget are already
    /// on the breakdown chart - so what matters is the CHANGE, and the change is only
    /// visible against last year. Each line's take and rate is recorded weekly, and
    /// the dashboard reads them a year apart.
    /// </summary>
    public partial class EconomySimulator
    {
        public const string RevenueSeriesPrefix = "rev:";
        public const string RateSeriesPrefix = "rate:";

        void RecordRevenueHistory(EconomyState state, PolicyState policy)
        {
            state.Series(RevenueSeriesPrefix + "TOTAL").Record(state.revenueBillions);
            foreach (KeyValuePair<string, float> line in state.treasury.revenueByLine)
            {
                state.Series(RevenueSeriesPrefix + line.Key).Record(line.Value);
                state.Series(RateSeriesPrefix + line.Key).Record(policy.Tax(line.Key));
            }
        }
    }
}
