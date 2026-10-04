namespace Sovereign.Core
{
    /// <summary>
    /// GDD 9. Money, the yield curve, auctions and the rating. This is the half of
    /// the model that disciplines the other half: the bond market prices your fiscal
    /// policy before the electorate does.
    /// </summary>
    public partial class EconomySimulator
    {
        void UpdateMoney(EconomyState state, PolicyState policy)
        {
            MacroConfig m = _config.macro;

            float weeklyQe = policy.qeAmountPerQuarter / 13f;
            float weeklyQt = policy.qtAmountPerQuarter / 13f;
            float net = weeklyQe - weeklyQt;

            state.monetary.qeBalanceBillions = MathUtil.Max(0f, state.monetary.qeBalanceBillions + net);

            // A well-behaved money supply grows with real output plus the target, NOT with
            // realised nominal GDP. Tracking nominal growth made the money term a
            // function of inflation itself: above target it pushed inflation higher,
            // which pushed money higher. QE and QT are what move it off that path.
            float organicGrowth = state.realGdpGrowth + m.inflationTarget;
            float reserveDrag = (policy.reserveRequirement - 8f) * 0.25f;
            float qeContribution = net * WeeksPerYear / MathUtil.Max(1f, state.monetary.moneySupplyM2Billions) * 100f;

            float targetGrowth = organicGrowth + qeContribution - reserveDrag;
            state.monetary.moneySupplyGrowth = MathUtil.Approach(state.monetary.moneySupplyGrowth, targetGrowth, 0.15f);
            state.monetary.moneySupplyM2Billions *= 1f + state.monetary.moneySupplyGrowth * 0.01f * Weekly;

            state.monetary.centralBankRate = policy.centralBankRate;
            state.monetary.guidance = policy.guidance;
            state.monetary.weeksSinceEmergencyCut++;
        }

        /// <summary>GDD 9.1. Three maturities, three different questions.</summary>
        void UpdateYieldCurve(EconomyState state, PolicyState policy) { UpdateYieldCurve(state, policy, false); }

        /// <param name="snap">Set each yield straight to its target instead of approaching it.
        /// Used at boot: starting all three at 0% and letting them converge at different
        /// speeds made the short end overtake the long end for the first weeks of every
        /// run - a spurious inversion, a recession warning and a confidence hit on day one.</param>
        void UpdateYieldCurve(EconomyState state, PolicyState policy, bool snap)
        {
            MacroConfig m = _config.macro;
            YieldCurve curve = state.bonds.yields;

            state.bonds.riskPremium = CalculateRiskPremium(state);

            float qeSuppression = state.monetary.qeBalanceBillions * 0.01f * m.qeYieldEffectPer100B
                                  * (state.monetary.credibility / 100f);

            // Short: tied tightly to the policy rate, as GDD 9.1 says.
            float shortTarget = state.monetary.centralBankRate + state.bonds.riskPremium * 0.3f - qeSuppression * 0.5f;

            // Medium: the average expected policy rate plus the inflation outlook.
            float guidanceShift = state.monetary.guidance == ForwardGuidance.Hawkish ? 0.4f
                                : state.monetary.guidance == ForwardGuidance.Dovish ? -0.4f : 0f;
            float expectedPath = MathUtil.Lerp(state.monetary.centralBankRate,
                                               m.neutralRealRate + m.inflationTarget, 0.45f);
            float mediumTarget = expectedPath + state.monetary.inflationExpectations * 0.25f
                                 + m.termPremium * 0.5f + state.bonds.riskPremium * 0.7f
                                 - qeSuppression + guidanceShift;

            // Long: inflation expectations and whether anyone believes your fiscal plan.
            float longTarget = m.neutralRealRate + state.monetary.inflationExpectations
                               + m.termPremium + state.bonds.riskPremium
                               - qeSuppression * 0.5f + guidanceShift * 0.5f;

            curve.shortTermYield = MathUtil.Clamp(MathUtil.Approach(curve.shortTermYield, shortTarget, snap ? 1f : 0.35f), 0f, 60f);
            curve.mediumTermYield = MathUtil.Clamp(MathUtil.Approach(curve.mediumTermYield, mediumTarget, snap ? 1f : 0.20f), 0f, 60f);
            curve.longTermYield = MathUtil.Clamp(MathUtil.Approach(curve.longTermYield, longTarget, snap ? 1f : 0.12f), 0f, 60f);
        }

        /// <summary>
        /// What the market charges you on top of the risk-free path: your debt burden
        /// (non-linear above 120%, GDD 21), your rating, your credibility, and how
        /// the last auction went.
        /// </summary>
        float CalculateRiskPremium(EconomyState state)
        {
            MacroConfig m = _config.macro;

            float debtPremium = m.debtRiskPremium.Evaluate(state.DebtToGdp);
            float ratingPremium = RatingPremium(state.bonds.creditRating);
            float credibilityPenalty = (100f - state.monetary.credibility) * 0.02f;
            // Continuous, not a cliff at 1.0: weaker-than-comfortable demand nudges yields
            // up, so a creditor who stops buying shows as a drift before anything fails.
            float auctionPenalty = MathUtil.Max(0f, m.comfortableAuctionCover - state.bonds.lastAuctionCover)
                                   * m.auctionDemandSensitivity * 2f;

            // Foreign-currency debt is cheap until the currency moves, then it is not
            // (GDD 9.3, the emerging-market trap modelled honestly).
            float mismatch = state.bonds.foreignCurrencyDebtPercent
                             * MathUtil.Max(0f, (100f - state.currency.exchangeRateIndex) * 0.03f);

            // Where the debt is HEADING, not only where it is. A government visibly paying
            // its debt down earns a cheaper curve within the year; one letting it run is
            // charged for that too. Without this the player could do everything right and
            // watch yields sit still, which is the opposite of a game.
            TimeSeries debtHistory = state.Series("debtToGdp");
            float trend = debtHistory.Count > 52 ? debtHistory.Latest - debtHistory.Ago(52) : 0f;
            float pathPremium = MathUtil.Clamp(trend * m.debtTrendPremium, -m.debtTrendCredit, m.debtTrendPenalty);

            return MathUtil.Clamp(debtPremium + ratingPremium + credibilityPenalty + auctionPenalty
                                  + mismatch + pathPremium, 0f, 45f);
        }

        float RatingPremium(CreditRating rating)
        {
            float[] premiums = _config.macro.ratingPremiums;
            if (premiums == null || premiums.Length == 0) return 0f;
            int index = (int)rating;
            if (index < 0) index = 0;
            if (index >= premiums.Length) index = premiums.Length - 1;
            return premiums[index];
        }

        /// <summary>
        /// GDD 9.2, quarterly. Demand depends on the real yield on offer, your
        /// credibility and your fiscal outlook. Weak demand means yields must rise -
        /// the market disciplining fiscal irresponsibility, without a scripted event.
        /// </summary>
        void RunBondAuction(EconomyState state, PolicyState policy)
        {
            // Buyers judge the RISK-ADJUSTED yield. The risk premium is compensation for
            // the chance of not being repaid, not a reward - counting it as attraction
            // made a 260%-debt auction 2.5x oversubscribed, so a debt crisis could
            // never produce the failed auctions that end one.
            float realYield = state.bonds.yields.mediumTermYield - state.monetary.inflationExpectations
                              - state.bonds.riskPremium;
            float deficitShare = -state.budgetBalancePercentGdp;

            float demand = 1f
                           + realYield * 0.18f
                           + (state.monetary.credibility - 60f) * 0.006f
                           - MathUtil.Max(0f, state.DebtToGdp - 1f) * 0.45f
                           - MathUtil.Max(0f, deficitShare - 3f) * 0.05f;

            // GDD 9.3: foreign issuance helps while your creditors are buying and hurts
            // once they stop - offering bonds abroad is leverage handed to the buyer.
            GeopoliticsTuning g = _config.geopolitics.tuning;
            float appetite = state.geopolitics.foreignAppetite;
            float foreignDemand = policy.foreignIssuanceShare
                                  * (g.foreignDemandWhenBuying * appetite + g.foreignDemandWhenStopped * (1f - appetite));
            demand += foreignDemand;
            state.bonds.lastForeignDemand = foreignDemand;

            state.bonds.lastAuctionCover = MathUtil.Clamp(demand, 0.3f, 2.5f);

            float shortfall = MathUtil.Max(0f, 1f - state.bonds.lastAuctionCover);
            if (shortfall > 0f)
            {
                float push = shortfall * _config.macro.auctionDemandSensitivity;
                state.bonds.yields.mediumTermYield += push;
                state.bonds.yields.longTermYield += push * 1.2f;
            }

            state.bonds.foreignHoldingPercent = MathUtil.Clamp(
                MathUtil.Approach(state.bonds.foreignHoldingPercent, policy.foreignIssuanceShare, 0.08f), 0f, 0.85f);

            if (policy.issueInForeignCurrency)
                state.bonds.foreignCurrencyDebtPercent = MathUtil.Clamp(state.bonds.foreignCurrencyDebtPercent + 0.02f, 0f, 0.9f);
            else
                state.bonds.foreignCurrencyDebtPercent = MathUtil.Max(0f, state.bonds.foreignCurrencyDebtPercent - 0.005f);
        }
    }
}
