namespace Sovereign.Core
{
    /// <summary>
    /// GDD 22. The floor under unemployment, and the player's one lever on it.
    ///
    /// A fixed natural rate made the endgame luck rather than policy: victory asks for
    /// 5.5% against a floor of 4.5%, so the last point had to come from growth alone
    /// while shocks kept pushing back, and a competent government won two runs in six.
    /// The floor is not a law of nature - it is how well matched people are to the work
    /// there is - and schooling and training move it. Spend on them and full employment
    /// means something lower; let them rot and the country cannot staff its own
    /// recovery however fast it grows.
    /// </summary>
    public partial class EconomySimulator
    {
        /// <summary>The floor as it stands this week, for everything that has to read it
        /// WITHOUT moving it - wages, confidence, the Phillips curve. Falls back to the
        /// authored rate before the first week has set one.</summary>
        public float SettledNaturalRate(EconomyState state)
        {
            return state.naturalRate > 0f ? state.naturalRate : _config.macro.naturalUnemploymentRate;
        }

        /// <summary>Where unemployment settles when growth is exactly at potential. Moves
        /// toward what the player's schooling budget earns, over years rather than at
        /// once.</summary>
        public float NaturalRate(EconomyState state, PolicyState policy)
        {
            MacroConfig m = _config.macro;

            float baseline = state.approval.baselineEducation;
            if (baseline <= 0f) return m.naturalUnemploymentRate;

            // Measured against what the country was spending the day the run opened, so
            // it asks "have you invested in people since?" rather than rewarding a
            // scenario for happening to start generous.
            float now = policy.Spending(SpendKeys.EducationK12) + policy.Spending("EducationHigher");
            float ratio = MathUtil.Max(0.05f, now / baseline);

            // DIMINISHING: the square root, not the ratio. Linear made schooling a win
            // button - a probe compounding the budget 8% a year drove the floor to its
            // minimum and finished at 0.7% unemployment, which no economy has ever run
            // at. Doubling the budget is worth about 0.6 points, quadrupling about 1.5:
            // worth doing, never sufficient on its own.
            float shift = MathUtil.Clamp((MathUtil.Pow(ratio, 0.5f) - 1f) * m.trainingNaturalRateEffect,
                                         -m.naturalRateRange, m.naturalRateRange);
            float target = MathUtil.Max(2.5f, m.naturalUnemploymentRate - shift);

            // SLOW: teaching people is a decade's work, not a budget line that pays this
            // quarter. Roughly three years to close most of the gap, so it is an
            // investment a government makes for its successor.
            if (state.naturalRate <= 0f) state.naturalRate = m.naturalUnemploymentRate;
            state.naturalRate = MathUtil.Approach(state.naturalRate, target, m.naturalRateAdjustmentSpeed);
            return state.naturalRate;
        }
    }
}
