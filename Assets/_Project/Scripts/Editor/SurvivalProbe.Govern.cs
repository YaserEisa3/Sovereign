using Sovereign.Core;
using Sovereign.Data;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// The probe used to set policy once and leave it for thirty years, which measured
    /// a government that never reacts to anything - and then the balance was tuned to
    /// suit it. A real player with no debt and 8% unemployment spends into it.
    ///
    /// This is a deliberately simple reaction, not an optimiser: once a year, look at
    /// the two things the scenario is judged on and lean against whichever is worse.
    /// If a competent government cannot win with this, the shortfall is in the model
    /// rather than in the player.
    /// </summary>
    public static partial class SurvivalProbe
    {
        static void Govern(SimulationRunner runner, EconomyState state)
        {
            if (state.IsGameOver) return;

            VictoryConfig goal = runner.Simulator.Victory.Config;
            float debt = state.DebtToGdp;
            bool roomToSpend = debt < 0.9f && state.budgetBalancePercentGdp > -4f;

            // Unemployment above target with the books in order: push demand. Growth
            // above potential is the only thing that moves unemployment now, and this
            // is how a government buys it - infrastructure first, because it raises
            // what the economy can produce as well as what it is producing.
            if (state.unemployment > goal.unemployment && roomToSpend)
            {
                foreach (string line in SpendKeys.Infrastructure)
                    if (runner.Policy.spendingBillions.ContainsKey(line))
                        runner.Policy.spendingBillions[line] *= 1.08f;

                // ...and on people, which lowers the floor itself rather than pushing
                // the economy harder against a fixed one.
                foreach (string line in new[] { SpendKeys.EducationK12, "EducationHigher" })
                    if (runner.Policy.spendingBillions.ContainsKey(line))
                        runner.Policy.spendingBillions[line] *= 1.08f;

                foreach (TaxDefinition tax in runner.Database.Taxes)
                {
                    if (tax.displayName.Contains("Credit")) continue;
                    string key = PolicyBootstrap.KeyFor(tax, "SO_Tax_");
                    float rate = runner.Policy.taxRates[key];
                    runner.Policy.taxRates[key] = MathUtil.Max(tax.defaultValue, rate - 0.4f);
                }
            }

            // Debt above what winning asks for: lean against it. The old rule only woke
            // at 110%, while victory needs 60% - so the probe was measuring a government
            // that stops caring long before the target, and debt parking at 80% looked
            // like the economy refusing rather than nobody asking.
            // ...but not while unemployment is still the worse problem and the debt is
            // not yet dangerous. Cutting taxes for jobs and raising them for debt in the
            // same year cancels out, which is what parked every run at 8% unemployment.
            // Growth shrinks debt/GDP through the denominator anyway: fix the jobs first,
            // then the books.
            bool jobsFirst = state.unemployment > goal.unemployment && debt < 0.9f;
            if (debt > goal.debtToGdp && !jobsFirst)
            {
                foreach (TaxDefinition tax in runner.Database.Taxes)
                {
                    if (tax.displayName.Contains("Credit")) continue;
                    string key = PolicyBootstrap.KeyFor(tax, "SO_Tax_");
                    float headroom = tax.maximumValue - tax.defaultValue;
                    // Harder the further from the target, so a mild overshoot is a nudge
                    // and a spiral is a wrench.
                    float ceiling = tax.defaultValue + headroom * (debt > 1.1f ? 0.5f : 0.3f);
                    runner.Policy.taxRates[key] = MathUtil.Min(ceiling, runner.Policy.taxRates[key] + (debt > 1.1f ? 0.3f : 0.12f));
                }
            }

            // And keep money loose while there is slack to use, tightening only when
            // inflation is genuinely getting away.
            runner.Policy.centralBankRate = state.inflation > 6f ? 6f
                                          : state.inflation > 4f ? 4f
                                          : 2.5f;
        }
    }
}
