using System;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 18: every control shows its approval impact by class BEFORE the change is
    /// queued. This is the DIRECT effect - what each group thinks of the decision
    /// itself. The knock-on effects through growth and prices arrive later, and the
    /// preview says so rather than pretending to forecast them.
    /// </summary>
    public partial class ApprovalModel
    {
        /// <summary>Long-run approval shift per group if <paramref name="apply"/> were
        /// in force: poor, middle, wealthy, left, centre, right. Policy is restored.</summary>
        public float[] PreviewShift(EconomyState state, PolicyState policy, Action<PolicyState> apply, Action<PolicyState> restore)
        {
            float[] before = RawTargets(state, policy, false);
            apply(policy);
            float[] after = RawTargets(state, policy, false);
            restore(policy);

            float[] shift = new float[6];
            for (int i = 0; i < 6; i++) shift[i] = after[i] - before[i];
            return shift;
        }

        public float[] PreviewTax(EconomyState state, PolicyState policy, string key, float value)
        {
            float original = policy.Tax(key);
            return PreviewShift(state, policy, p => p.taxRates[key] = value, p => p.taxRates[key] = original);
        }

        public float[] PreviewSpending(EconomyState state, PolicyState policy, string key, float value)
        {
            float original = policy.Spending(key);
            return PreviewShift(state, policy, p => p.spendingBillions[key] = value, p => p.spendingBillions[key] = original);
        }

        /// <summary>"mid -4 rich +2" - only the groups that move by half a point or more.</summary>
        public static string Describe(float[] shift)
        {
            string[] names = { "poor", "mid", "rich", "left", "ctr", "right" };
            string text = "";
            for (int i = 0; i < 3; i++)
            {
                if (MathUtil.Abs(shift[i]) < 0.5f) continue;
                text += (text.Length > 0 ? " " : "") + names[i] + " " + (shift[i] > 0f ? "+" : "") + shift[i].ToString("0");
            }
            return text.Length == 0 ? "no one notices" : text;
        }
    }
}
