using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 19. Selling what the country has spare to the countries that cannot make it.
    ///
    /// Production said what there was; this is what happens to it. A contract takes goods
    /// out of the domestic pool whether or not they can be spared, pays for them in
    /// foreign currency, and holds the government to its word for years after the
    /// minister who signed it has gone.
    /// </summary>
    public class TradeModel
    {
        public const int MinimumTermYears = 2;
        public const int MaximumTermYears = 10;

        /// <summary>Relationship damage for walking away from a signed obligation.</summary>
        public const float BreachRelationshipCost = 22f;

        /// <summary>
        /// Will this country buy this product at all? Only if it genuinely cannot make
        /// enough of its own - that is what makes a deal worth having rather than a
        /// favour being asked.
        /// </summary>
        public static bool Wants(NationProfile nation, ProductId product)
        {
            if (nation.needs == null) return false;
            for (int i = 0; i < nation.needs.Length; i++) if (nation.needs[i] == product) return true;
            return false;
        }

        /// <summary>
        /// What they will pay, as a price index. The world price, improved by a good
        /// relationship and discounted by a bad one: a country that likes you pays a
        /// premium for the security of a long contract, and one that does not will take
        /// your goods only cheaply.
        /// </summary>
        public static float OfferedPrice(EconomyState state, NationProfile nation, int nationIndex, ProductId product)
        {
            ProductState p;
            float world = state.production.TryGetValue(product, out p) ? p.worldPrice : 100f;

            float relationship = nationIndex >= 0 && nationIndex < state.geopolitics.nations.Count
                ? state.geopolitics.nations[nationIndex].relationship : 0f;

            // -60 relationship knocks about a sixth off; +60 adds about a sixth.
            return world * (1f + MathUtil.Clamp(relationship, -80f, 80f) * 0.0022f);
        }

        /// <summary>
        /// The largest order that can be signed: what the country has spare of this
        /// product today. Promising goods that do not exist is refused outright rather
        /// than merely discouraged - a government cannot contract away food off its own
        /// table, and a rule that lives only in the UI is not a rule.
        /// </summary>
        public static float Spare(EconomyState state, ProductId product)
        {
            ProductState p;
            if (!state.production.TryGetValue(product, out p)) return 0f;
            return MathUtil.Max(0f, p.Balance);
        }

        public void TickWeek(EconomyState state)
        {
            if (state.IsGameOver) return;

            // Clear last week's commitments before counting this week's.
            foreach (KeyValuePair<ProductId, ProductState> entry in state.production) entry.Value.contracted = 0f;

            float earnings = 0f;
            for (int i = state.contracts.Count - 1; i >= 0; i--)
            {
                TradeContract contract = state.contracts[i];

                contract.weeksRemaining--;
                if (contract.weeksRemaining <= 0)
                {
                    state.events.Post(state.week, AlertLevel.Info, AlertChannel.Ticker,
                        "The " + contract.nationName + " contract for " + Name(contract.product)
                        + " has run its course. They are free to buy elsewhere.");
                    state.contracts.RemoveAt(i);
                    continue;
                }

                ProductState p;
                if (!state.production.TryGetValue(contract.product, out p)) continue;

                // The goods leave whether they can be spared or not. That is the risk.
                p.contracted += contract.volumeBillions;
                earnings += contract.EarningsPerYear;
            }

            state.contractEarningsPerYear = earnings;

            // Foreign currency, not tax revenue: exports are paid for abroad and land in
            // the reserves, which is what makes them worth having when the currency is
            // under pressure.
            state.currency.fxReservesBillions += earnings * (1f / WeeksPerYear);
        }

        /// <summary>
        /// Sign. Takes effect at once rather than at the quarter, because a contract is
        /// a negotiation that concluded, not a dial the treasury turns.
        /// </summary>
        public static TradeContract Sign(EconomyState state, NationProfile nation, int nationIndex,
                                         ProductId product, float volumeBillions, int termYears)
        {
            if (!Wants(nation, product) || volumeBillions <= 0f) return null;

            // Nothing may be promised that the country does not already have going spare.
            // Existing contracts have taken their share before this is asked.
            if (volumeBillions > Spare(state, product) + 0.01f) return null;

            TradeContract contract = new TradeContract
            {
                product = product,
                nation = nationIndex,
                nationName = nation.name,
                volumeBillions = volumeBillions,
                agreedPrice = OfferedPrice(state, nation, nationIndex, product),
                signedWeek = state.week,
                weeksRemaining = (int)(MathUtil.Clamp(termYears, MinimumTermYears, MaximumTermYears) * 52f)
            };
            state.contracts.Add(contract);

            state.events.Post(state.week, AlertLevel.Info, AlertChannel.Ticker,
                "Signed: " + volumeBillions.ToString("#,0") + "B of " + product
                + " a year to " + nation.name + " for " + termYears + " years at "
                + contract.agreedPrice.ToString("0") + ".");

            // Trading partners who need you think better of you for it.
            if (nationIndex >= 0 && nationIndex < state.geopolitics.nations.Count)
                state.geopolitics.nations[nationIndex].relationship =
                    MathUtil.Clamp(state.geopolitics.nations[nationIndex].relationship + 6f, -100f, 100f);

            return contract;
        }

        /// <summary>
        /// Break it. The goods stay home and the buyer remembers - a government that
        /// tears up a supply contract is not one anybody signs a long deal with again.
        /// </summary>
        public static void Break(EconomyState state, TradeContract contract)
        {
            if (contract == null || !state.contracts.Contains(contract)) return;
            state.contracts.Remove(contract);

            if (contract.nation >= 0 && contract.nation < state.geopolitics.nations.Count)
                state.geopolitics.nations[contract.nation].relationship =
                    MathUtil.Clamp(state.geopolitics.nations[contract.nation].relationship - BreachRelationshipCost,
                                   -100f, 100f);

            state.events.Post(state.week, AlertLevel.Warning, AlertChannel.Ticker,
                "You have torn up the " + contract.nationName + " contract for " + contract.product
                + ". They will not forget it.");
        }

        const float WeeksPerYear = 52f;

        static string Name(ProductId product) { return product.ToString(); }
    }
}
