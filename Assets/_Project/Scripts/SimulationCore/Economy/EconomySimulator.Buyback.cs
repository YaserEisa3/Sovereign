namespace Sovereign.Core
{
    /// <summary>
    /// GDD 9. Buying your own debt back. When your bonds trade below par - which is
    /// what high yields against a low coupon MEAN - a dollar retires more than a
    /// dollar of face value, and the debt shrinks faster than you spend. That is the
    /// one honest way out of a debt stock this size, and it is self-limiting: you are
    /// a big buyer, so the more you buy at once the worse the price you get, and the
    /// cash comes from issuing fresh debt at today's yields, which is what you were
    /// running from.
    /// </summary>
    public partial class EconomySimulator
    {
        /// <summary>Average years to maturity, from the profile the player has been
        /// issuing into. A long book moves further on the same yield gap.</summary>
        public float PortfolioDuration(EconomyState state)
        {
            MacroConfig m = _config.macro;
            BondMarketState b = state.bonds;
            return b.maturingWithinOneYear * m.buybackShortYears
                   + b.maturingOneToFive * m.buybackMediumYears
                   + b.maturingBeyondFive * m.buybackLongYears;
        }

        /// <summary>What a dollar of face costs right now, before you move the market.
        /// Below 1 means your debt is at a discount.</summary>
        public float BuybackPrice(EconomyState state, float cashBillions)
        {
            MacroConfig m = _config.macro;
            BondMarketState b = state.bonds;

            float gap = (b.yields.mediumTermYield - b.averageCoupon) * 0.01f;
            float price = 1f - gap * PortfolioDuration(state);
            // You are the market. Lifting the bid is what a large order does to a price.
            price += cashBillions * 0.01f * m.buybackExecutionPremiumPer100B;
            return MathUtil.Clamp(price, m.buybackMinPrice, m.buybackMaxPrice);
        }

        /// <summary>The most face value this quarter's operation can retire.</summary>
        public float BuybackFace(EconomyState state, float cashBillions)
        {
            if (cashBillions <= 0f) return 0f;
            float face = cashBillions / MathUtil.Max(0.05f, BuybackPrice(state, cashBillions));
            return MathUtil.Min(face, state.bonds.debtStockBillions * _config.macro.buybackMaxShareOfStock);
        }

        /// <summary>GDD 4: the order is placed now and executes at the quarter.</summary>
        void RunBuyback(EconomyState state, PolicyState policy)
        {
            float cash = policy.buybackBillions;
            policy.buybackBillions = 0f;      // one quarter, one operation
            if (cash <= 0f) return;

            BondMarketState b = state.bonds;
            float stock = b.debtStockBillions;
            if (stock <= 0f) return;

            float price = BuybackPrice(state, cash);
            float face = BuybackFace(state, cash);
            cash = face * price;              // what the retired face actually costs

            // The cash is raised the only way this government can raise it: by issuing
            // new debt at today's yields. Old, cheap coupons leave; new, dear ones arrive.
            float issuanceYield = b.yields.mediumTermYield;
            float remaining = MathUtil.Max(0f, stock - face);
            float newStock = remaining + cash;
            b.averageCoupon = (remaining * b.averageCoupon + cash * issuanceYield) / MathUtil.Max(1f, newStock);
            b.debtStockBillions = newStock;

            // Foreign creditors take part of the offer, so the share held abroad falls.
            float foreignSold = face * _config.macro.buybackForeignSellerShare;
            float foreignHeld = MathUtil.Max(0f, b.foreignHoldingPercent * stock - foreignSold);
            b.foreignHoldingPercent = MathUtil.Clamp(foreignHeld / MathUtil.Max(1f, newStock), 0f, 0.85f);
            ReduceNationHoldings(state, foreignSold / MathUtil.Max(1f, stock * MathUtil.Max(0.01f, b.foreignHoldingPercent)));

            b.lastBuybackPrice = price;
            b.lastBuybackFace = face;
            b.lastBuybackCost = cash;
            b.lastBuybackWeek = state.week;

            float net = face - cash;
            state.events.Post(state.week, net > 0f ? AlertLevel.Info : AlertLevel.Warning, AlertChannel.Ticker,
                "Bought back $" + Round(face) + "B of debt at " + (price * 100f).ToString("0") + " cents, for $"
                + Round(cash) + "B" + (net > 0f
                    ? ". Net debt down $" + Round(net) + "B."
                    : ". Paying above par cost $" + Round(-net) + "B."));
        }

        void ReduceNationHoldings(EconomyState state, float share)
        {
            if (share <= 0f) return;
            float keep = MathUtil.Clamp(1f - share, 0f, 1f);
            var holdings = state.bonds.holdingsByNation;
            var keys = new System.Collections.Generic.List<string>(holdings.Keys);
            foreach (string key in keys) holdings[key] = holdings[key] * keep;

            for (int i = 0; i < state.geopolitics.nations.Count; i++)
                state.geopolitics.nations[i].bondHolding *= keep;
        }

        static string Round(float billions) { return billions.ToString("0"); }
    }
}
