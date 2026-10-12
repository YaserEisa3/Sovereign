using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 19. What the country makes, what it needs, and what that costs when the two
    /// do not match.
    ///
    /// Sectors used to be health and headcount and nothing else - agriculture in fine
    /// condition produced no food, and the trade balance was an index that moved on the
    /// exchange rate whether or not a single thing was grown. Output is the missing
    /// middle: a healthy sector makes goods, the population eats some of them, and the
    /// rest is a surplus worth selling.
    /// </summary>
    public class ProductionModel
    {
        readonly ProductConfig[] _products;

        public ProductionModel(ProductConfig[] products) { _products = products; }

        public ProductConfig[] Products { get { return _products; } }

        public void Prepare(EconomyState state)
        {
            state.production.Clear();
            for (int i = 0; i < _products.Length; i++)
                state.production[_products[i].id] = new ProductState { id = _products[i].id };
        }

        public void TickWeek(EconomyState state, PolicyState policy)
        {
            float people = state.population.Total;

            for (int i = 0; i < _products.Length; i++)
            {
                ProductConfig c = _products[i];
                ProductState p;
                if (!state.production.TryGetValue(c.id, out p)) continue;

                // A sector's output is its slice of the economy, scaled by how well it is
                // doing. Health 70 is par, which is where the sector model centres.
                EconomicSector sector = state.GetSector(c.sector);
                float condition = sector == null ? 1f : MathUtil.Clamp(sector.health / 70f, 0.2f, 1.4f);

                // The arms industry is not a share of GDP - it is as big as the state
                // pays for it to be. Government/Military carries a gdpShare of ZERO, so
                // treating it like the others left arms and defence services producing
                // nothing at all, forever. What the country builds weapons with is the
                // equipment and research budget, which also means a government that
                // funds its forces has something to sell.
                // REAL output, in opening dollars. Measured against nominal GDP, inflation
                // inflated apparent production while need stayed put - so a country could
                // cure a famine by printing money, and shortage-driven inflation fed a
                // spiral that pinned unemployment at its floor and halved real wages.
                // Bread is bread whatever it costs.
                float realGdp = state.nominalGdpBillions / MathUtil.Max(0.1f, state.priceLevel * 0.01f);
                float sectorOutput = c.sector == SectorId.GovernmentMilitary
                    ? (policy.Spending("MilitaryEquipment") + policy.Spending("MilitaryRnD")) * condition
                    : sector == null ? 0f : realGdp * sector.gdpShare * condition;

                // What the ground and the climate allow, before any budget is considered.
                p.produced = sectorOutput * c.sectorShare * c.capability;
                p.needed = c.needPer100M * people * 0.01f;

                // Scarcity at home, in prices. A country making half what it needs of
                // something pays through the nose for the difference, and the shortage
                // shows up in the cost of living long before anyone calls it a crisis.
                // Scarcity and abundance are NOT symmetric. A country short of bread pays
                // whatever it takes; a country with bread to spare gets a modest
                // discount and no more, because the surplus goes abroad rather than
                // being given away. Dividing straight through gave a third off the price
                // of food for being comfortably supplied, which no shopper has ever seen.
                float cover = p.needed <= 0.01f ? 2f : p.produced / p.needed;
                // IMPORT PARITY. A country short of oil does not watch its own price climb
                // forever - it buys oil abroad and pays the world price plus freight. Without
                // this a shortage the player CANNOT fix pressed on inflation permanently,
                // and permanent pressure unanchors expectations however hard the direct
                // contribution is capped: two seeds ended at 31% and 37% CPI with real
                // wages at a third of pre-war, purely because the country has no oil.
                // Scarcity is expensive, not unbounded.
                float scarcity = MathUtil.Min(250f, 100f / MathUtil.Max(0.4f, cover));
                float importParity = p.worldPrice * 1.2f;
                float target = cover >= 1f
                    ? 100f - 10f * MathUtil.Clamp01(cover - 1f)
                    : MathUtil.Min(scarcity, importParity);
                p.domesticPrice = MathUtil.Approach(p.domesticPrice, target, 0.04f);
            }
        }

        /// <summary>
        /// How much the cost of living is above normal because of what the country
        /// cannot supply itself - weighted by how much a shortage of each thing
        /// actually hurts. Bread and power, not software.
        /// </summary>
        public float ShortagePressure(EconomyState state)
        {
            float weighted = 0f, weight = 0f;
            for (int i = 0; i < _products.Length; i++)
            {
                ProductState p;
                if (!state.production.TryGetValue(_products[i].id, out p)) continue;
                weighted += MathUtil.Max(0f, p.domesticPrice - 100f) * _products[i].shortageSeverity;
                weight += _products[i].shortageSeverity;
            }
            return weight <= 0f ? 0f : weighted / weight;
        }

        /// <summary>Everything the country has spare, at base prices - what stage 2 will
        /// be allowed to sell.</summary>
        public float ExportableBillions(EconomyState state)
        {
            float spare = 0f;
            foreach (KeyValuePair<ProductId, ProductState> entry in state.production)
                spare += MathUtil.Max(0f, entry.Value.Balance);
            return spare;
        }
    }
}
