namespace Sovereign.Core
{
    /// <summary>
    /// The satisfaction scores, 0-100, each one a plain reading of a condition. The
    /// absolute level barely matters - calibration at boot pins the opening approvals
    /// - so what these must get right is DIRECTION and relative steepness.
    /// </summary>
    public partial class ApprovalModel
    {
        float[] RawTargets(EconomyState s, PolicyState p) { return RawTargets(s, p, true); }

        float[] RawTargets(EconomyState s, PolicyState p, bool advanceInequality)
        {
            if (advanceInequality) UpdateInequality(s, p);

            // The score is the weighted average of exactly the components the drivers
            // list shows - one definition, so the bar and its explanation agree.
            System.Collections.Generic.List<ApprovalDriver>[] groups = Components(s, p);

            float immigrationShift = p.immigrationInflowMillions - s.approval.baselineImmigration;

            return new[]
            {
                Weigh(groups[0]),
                Weigh(groups[1]),
                Weigh(groups[2]),
                Weigh(groups[3]) + immigrationShift * _c.immigrationLeftApproval,
                Weigh(groups[4]),
                Weigh(groups[5]) + immigrationShift * _c.immigrationRightApproval
            };
        }

        /// <summary>
        /// Inequality moves slowly: unemployment and cuts to welfare widen it, a
        /// progressive top rate and capital gains tax narrow it. The Left scores on it.
        /// </summary>
        void UpdateInequality(EconomyState s, PolicyState p)
        {
            float target = 0.41f
                           + MathUtil.Max(0f, s.unemployment - 4.5f) * 0.004f
                           - (Ratio(Welfare(p), s.approval.baselineWelfare) - 1f) * 0.05f
                           - (p.Tax(TaxKeys.IncomeTop) - 37f) * 0.0015f
                           - (p.Tax(TaxKeys.CapitalGains) - 20f) * 0.001f;
            s.giniCoefficient = MathUtil.Clamp(MathUtil.Approach(s.giniCoefficient, target, 0.004f), 0.2f, 0.7f);
        }

        static float Score(float value) { return MathUtil.Clamp(value, 0f, 100f); }

        static float Ratio(float current, float baseline) { return baseline <= 0f ? 1f : current / baseline; }

    }
}
