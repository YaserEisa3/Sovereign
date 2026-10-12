using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 19. The product table. Authored in code rather than as assets because these
    /// are the economy's nouns, not tuning: changing what a country makes is a design
    /// decision, and the shares inside a sector must sum to 1.
    ///
    /// needPer100M is $B a year a hundred million people get through. Grain and
    /// electricity are enormous and a shortage of either is a political emergency;
    /// arms and financial services are barely consumed at home, which is what makes
    /// them nearly pure export.
    /// </summary>
    public static partial class SimulationConfigBuilder
    {
        static ProductConfig[] BuildProducts()
        {
            return new[]
            {
                // Each sector's needs are calibrated against what that sector PRODUCES at
                // par health, at roughly 80-85% of it. The first pass set them per head
                // from intuition and every product sat in enormous surplus - agriculture
                // is 6% of GDP, so what a country spends on food has to be of that order
                // or farming can never feed anybody however healthy it gets.
                Product(ProductId.Grain, "Grain", SectorId.Agriculture, 0.45f, 200f, 1.0f),
                Product(ProductId.MeatDairy, "Meat and Dairy", SectorId.Agriculture, 0.35f, 155f, 0.8f),
                Product(ProductId.Produce, "Fruit and Vegetables", SectorId.Agriculture, 0.20f, 86f, 0.7f),

                Product(ProductId.CrudeOil, "Crude Oil", SectorId.Energy, 0.40f, 281f, 0.9f),
                Product(ProductId.NaturalGas, "Natural Gas", SectorId.Energy, 0.25f, 170f, 0.9f),
                Product(ProductId.Electricity, "Electricity", SectorId.Energy, 0.35f, 253f, 1.0f),

                Product(ProductId.Steel, "Steel", SectorId.Manufacturing, 0.30f, 237f, 0.4f),
                Product(ProductId.Vehicles, "Vehicles", SectorId.Manufacturing, 0.40f, 318f, 0.3f),
                Product(ProductId.Machinery, "Machinery", SectorId.Manufacturing, 0.30f, 238f, 0.4f),

                Product(ProductId.Semiconductors, "Semiconductors", SectorId.Technology, 0.45f, 474f, 0.3f),
                Product(ProductId.Software, "Software", SectorId.Technology, 0.55f, 583f, 0.1f),

                Product(ProductId.ConsumerGoods, "Consumer Goods", SectorId.ServicesRetail, 0.70f, 2516f, 0.6f),
                Product(ProductId.Tourism, "Tourism", SectorId.ServicesRetail, 0.30f, 524f, 0.1f),

                Product(ProductId.FinancialServices, "Financial Services", SectorId.Finance, 1.00f, 925f, 0.2f),

                Product(ProductId.Arms, "Arms", SectorId.GovernmentMilitary, 0.55f, 59f, 0.1f),
                Product(ProductId.DefenceServices, "Defence Services", SectorId.GovernmentMilitary, 0.45f, 50f, 0.1f),
            };
        }

        static ProductConfig Product(ProductId id, string name, SectorId sector,
                                     float sectorShare, float needPer100M, float shortageSeverity)
        {
            return new ProductConfig
            {
                id = id,
                name = name,
                sector = sector,
                sectorShare = sectorShare,
                needPer100M = needPer100M,
                shortageSeverity = shortageSeverity,
                basePrice = 100f
            };
        }
    }
}
