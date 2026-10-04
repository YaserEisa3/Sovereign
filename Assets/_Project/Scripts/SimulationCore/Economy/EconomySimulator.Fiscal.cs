namespace Sovereign.Core
{
    /// <summary>
    /// The debt arithmetic. Interest compounds on the stock, the deficit adds to it,
    /// and nominal growth is the only thing quietly shrinking the ratio - which is
    /// why inflation is a debtor's friend right up until it is not.
    /// </summary>
    public partial class EconomySimulator
    {
        void UpdateFiscal(EconomyState state, PolicyState policy)
        {
            BondMarketState bonds = state.bonds;

            // New borrowing reprices toward current market yields as old debt matures.
            // Short-heavy issuance is cheaper now and reprices faster - GDD 9.2's
            // rollover risk, without needing a scripted event to punish it.
            float issuanceYield = policy.maturityPreference == BondMaturityPreference.ShortHeavy
                ? bonds.yields.shortTermYield
                : policy.maturityPreference == BondMaturityPreference.LongHeavy
                    ? bonds.yields.longTermYield
                    : bonds.yields.mediumTermYield;

            float rolloverSpeed = bonds.maturingWithinOneYear * Weekly;
            bonds.averageCoupon = MathUtil.Approach(bonds.averageCoupon, issuanceYield, rolloverSpeed);

            // GDD 8: revenue and spending line by line. The budget is set in today's
            // dollars and spent in tomorrow's, so spending is indexed to the price level
            // inside the treasury - without that, revenue outgrew a frozen budget and
            // the debt quietly paid itself off.
            _treasury.Update(state, policy);
            state.spendingBillions = state.treasury.totalSpending;
            state.revenueBillions = state.treasury.totalRevenue;

            float balance = state.revenueBillions - state.spendingBillions;
            state.budgetBalancePercentGdp = balance / MathUtil.Max(1f, state.nominalGdpBillions) * 100f;

            bonds.debtStockBillions = MathUtil.Max(0f, bonds.debtStockBillions - balance * Weekly);

            // The maturity profile drifts toward whatever the player keeps issuing.
            float shortTarget = policy.maturityPreference == BondMaturityPreference.ShortHeavy ? 0.40f
                              : policy.maturityPreference == BondMaturityPreference.LongHeavy ? 0.12f : 0.22f;
            float longTarget = policy.maturityPreference == BondMaturityPreference.LongHeavy ? 0.55f : 0.35f;

            bonds.maturingWithinOneYear = MathUtil.Approach(bonds.maturingWithinOneYear, shortTarget, 0.002f);
            bonds.maturingBeyondFive = MathUtil.Approach(bonds.maturingBeyondFive, longTarget, 0.002f);
            bonds.maturingOneToFive = MathUtil.Clamp(
                1f - bonds.maturingWithinOneYear - bonds.maturingBeyondFive, 0.05f, 0.9f);
        }
    }
}
