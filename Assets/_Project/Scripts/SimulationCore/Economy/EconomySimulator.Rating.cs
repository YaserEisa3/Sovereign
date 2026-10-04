namespace Sovereign.Core
{
    /// <summary>GDD 9.4 and 9.5. Credibility, and the annual rating review that
    /// prices it. Both are telegraphed before they bite.</summary>
    public partial class EconomySimulator
    {
        /// <summary>
        /// GDD 9.4. Credibility is earned by doing the unpopular thing at the right
        /// time and lost by printing into an inflation you are already losing.
        /// </summary>
        void UpdateCredibility(EconomyState state, PolicyState policy)
        {
            MacroConfig m = _config.macro;
            float change = 0f;

            bool inflationHigh = state.coreInflation > m.inflationTarget + 2f;
            bool running = policy.qeAmountPerQuarter > 0f;

            // Printing while prices are already rising.
            if (running && inflationHigh) change -= m.credibilityLossPerQeAtHighInflation;

            // Monetising the deficit: QE while the fiscal position is bad.
            if (running && state.budgetBalancePercentGdp < -6f) change -= 2f;

            // Sitting on your hands while inflation climbs.
            if (state.inflation > m.inflationTarget + 3f && state.RealInterestRate < 0f) change -= 2.5f;

            // Raising rates when it hurts - a recession you accepted to hold the line.
            if (state.RealInterestRate > 1.5f && state.realGdpGrowth < 0f) change += 2f;

            // Hitting the target quietly, quarter after quarter.
            if (MathUtil.Abs(state.coreInflation - m.inflationTarget) < 1f)
                change += m.credibilityGainPerYearOnTarget * 0.25f;

            state.monetary.credibility = MathUtil.Clamp(state.monetary.credibility + change, 0f, 100f);
        }

        /// <summary>
        /// GDD 9.5. Reviewed annually against debt, deficit and growth, and always
        /// telegraphed: the watch-negative flag goes up a review before the cut.
        /// </summary>
        void ReviewCreditRating(EconomyState state, PolicyState policy)
        {
            float debt = state.DebtToGdp;
            float deficit = -state.budgetBalancePercentGdp;
            float growth = state.Series("gdpGrowth").Average(52);

            CreditRating deserved = DeservedRating(debt, deficit, growth);

            if (deserved > state.bonds.creditRating)
            {
                // A downgrade is coming. Warn first, cut at the next review.
                if (state.bonds.ratingWatchNegative)
                {
                    state.bonds.creditRating = (CreditRating)((int)state.bonds.creditRating + 1);
                    state.bonds.ratingWatchNegative = deserved > state.bonds.creditRating;
                }
                else state.bonds.ratingWatchNegative = true;
            }
            else
            {
                state.bonds.ratingWatchNegative = false;
                // Upgrades are slow and grudging, one notch at a time.
                if (deserved < state.bonds.creditRating)
                    state.bonds.creditRating = (CreditRating)((int)state.bonds.creditRating - 1);
            }
        }

        /// <summary>The GDD 9.5 table, read top down: the first condition you fail
        /// sets the rating.</summary>
        CreditRating DeservedRating(float debtToGdp, float deficitPercentGdp, float averageGrowth)
        {
            if (debtToGdp > 1.6f || (averageGrowth < 0f && deficitPercentGdp > 10f)) return CreditRating.B;
            if (debtToGdp > 1.3f || deficitPercentGdp > 8f) return CreditRating.BB;
            if (debtToGdp > 1.0f) return CreditRating.BBB;
            if (debtToGdp > 0.8f) return CreditRating.A;
            if (debtToGdp > 0.6f) return CreditRating.AA;
            return CreditRating.AAA;
        }

        /// <summary>GDD 12. Instant cut, once a year, and the market notices you panicked.</summary>
        public bool TryEmergencyRateCut(EconomyState state, PolicyState policy)
        {
            if (state.monetary.weeksSinceEmergencyCut < WeeksPerYear) return false;

            policy.centralBankRate = MathUtil.Max(0f, policy.centralBankRate - 1f);
            state.monetary.centralBankRate = policy.centralBankRate;
            state.monetary.weeksSinceEmergencyCut = 0;
            state.monetary.credibility = MathUtil.Max(0f, state.monetary.credibility - 6f);
            return true;
        }
    }
}
