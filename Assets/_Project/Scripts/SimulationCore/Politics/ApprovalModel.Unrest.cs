namespace Sovereign.Core
{
    /// <summary>GDD 14. Unrest builds per class, and revolt ends the run.</summary>
    public partial class ApprovalModel
    {
        void UpdateUnrest(EconomyState s)
        {
            ApprovalState a = s.approval;
            a.unrestPoor = Unrest(s, a.unrestPoor, a.poor, "approvalPoor", ref a.weeksPoorLow);
            a.unrestMiddle = Unrest(s, a.unrestMiddle, a.middle, "approvalMiddle", ref a.weeksMiddleLow);
            a.unrestWealthy = Unrest(s, a.unrestWealthy, a.wealthy, "approvalWealthy", ref a.weeksWealthyLow);
            s.Series("unrest").Record(a.HighestUnrest);
        }

        /// <summary>
        /// GDD 14's list, point for point: sustained low approval, a rapid slide,
        /// runaway inflation, mass unemployment. When none of them hold, unrest cools.
        /// </summary>
        float Unrest(EconomyState s, float unrest, float approval, string series, ref int weeksLow)
        {
            weeksLow = approval < _c.unrestApprovalThreshold ? weeksLow + 1 : 0;

            float build = 0f;
            // Scaled by depth: resentment at 38% simmers, at 15% it boils.
            if (weeksLow >= _c.unrestApprovalWeeks)
                build += (_c.unrestApprovalThreshold - approval) * _c.unrestBuildPerPointBelow;
            if (s.Series(series).Ago(4) - approval > _c.rapidDeclinePointsPerMonth) build += _c.unrestPerCrisis;
            if (s.inflation > _c.unrestInflationThreshold) build += _c.unrestPerCrisis;
            if (s.unemployment > _c.unrestUnemploymentThreshold) build += _c.unrestPerCrisis;

            // A class that approves of the government is slow to take to the streets over
            // it. Without this a run ended with the poor at 71% approval overthrowing a
            // government they liked: unemployment sat one point over the threshold, unrest
            // climbed a point a week for ninety weeks, and being popular bought nothing.
            float relief = MathUtil.Clamp01((approval - _c.unrestContentApproval)
                                            / MathUtil.Max(1f, 100f - _c.unrestContentApproval));
            build *= 1f - relief * _c.unrestContentRelief;

            // Cooling is ALWAYS on now, rather than only when nothing at all is wrong.
            // One grievance borne by a contented class roughly cancels against it and
            // goes nowhere; two at once, or one carried by a class that has turned on
            // you, still builds. That is the difference between a problem and a clock.
            return MathUtil.Clamp(unrest + build - _c.unrestDecayPerWeek, 0f, 100f);
        }

        /// <summary>Off only in the economy tests, which check economic mechanics over
        /// decades of deliberate ruin and would otherwise be cut short by the politics.
        /// Unrest still accumulates; only the ending is suppressed.</summary>
        public bool RevoltEnabled = true;

        void CheckRevolt(EconomyState s)
        {
            ApprovalState a = s.approval;
            if (a.revolt || !RevoltEnabled) return;

            a.weeksOverallCritical = a.overall < _c.revoltOverallApproval ? a.weeksOverallCritical + 1 : 0;
            a.weeksPoorRevolting = a.unrestPoor > _c.revoltSingleClassUnrest ? a.weeksPoorRevolting + 1 : 0;
            a.weeksMiddleRevolting = a.unrestMiddle > _c.revoltSingleClassUnrest ? a.weeksMiddleRevolting + 1 : 0;
            a.weeksWealthyRevolting = a.unrestWealthy > _c.revoltSingleClassUnrest ? a.weeksWealthyRevolting + 1 : 0;

            // GDD 14 states hyperinflation monthly. Annual inflation converts to it here.
            float monthly = (MathUtil.Pow(1f + s.inflation * 0.01f, 1f / 12f) - 1f) * 100f;
            a.weeksHyperinflation = monthly > _c.hyperinflationMonthlyThreshold ? a.weeksHyperinflation + 1 : 0;

            if (a.weeksOverallCritical >= _c.revoltOverallWeeks)
                Revolt(a, "Approval stayed below " + _c.revoltOverallApproval.ToString("0")
                          + "% for " + a.weeksOverallCritical + " weeks. The government has fallen.");
            else if (a.weeksPoorRevolting >= _c.revoltSingleClassWeeks)
                Revolt(a, "The poor rose up after " + a.weeksPoorRevolting + " weeks of open unrest.");
            else if (a.weeksMiddleRevolting >= _c.revoltSingleClassWeeks)
                Revolt(a, "The middle class brought the country to a standstill after "
                          + a.weeksMiddleRevolting + " weeks of open unrest.");
            else if (a.weeksWealthyRevolting >= _c.revoltSingleClassWeeks)
                Revolt(a, "Capital fled and the establishment turned on you after "
                          + a.weeksWealthyRevolting + " weeks of open unrest.");
            else if (a.weeksHyperinflation >= 13)
                Revolt(a, "Hyperinflation - prices rising " + monthly.ToString("0") + "% a month. The currency is worthless.");
        }

        static void Revolt(ApprovalState a, string reason)
        {
            a.revolt = true;
            a.revoltReason = reason;
        }
    }
}
