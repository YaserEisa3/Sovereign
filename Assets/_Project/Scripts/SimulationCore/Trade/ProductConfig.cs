namespace Sovereign.Core
{
    /// <summary>One product: who makes it, who needs it, what the world pays.</summary>
    public struct ProductConfig
    {
        public ProductId id;
        public string name;
        public SectorId sector;

        /// <summary>Share of its sector's output that is this product. The products of a
        /// sector sum to 1.</summary>
        public float sectorShare;

        /// <summary>$B a year of this product a country of 100 million people needs to go
        /// about its life. Grain and electricity are enormous; arms are nearly nothing,
        /// which is why they are almost pure export.</summary>
        public float needPer100M;

        /// <summary>How hard a shortage bites. Nobody riots over a shortage of software;
        /// a shortage of bread or power is a political emergency.</summary>
        public float shortageSeverity;

        /// <summary>Starting world price index. Always 100 at the opening - scarcity moves
        /// it from there.</summary>
        public float basePrice;
    }

    /// <summary>What one product is doing this week.</summary>
    public class ProductState
    {
        public ProductId id;

        /// <summary>$B a year, at base prices, so output is comparable across products.</summary>
        public float produced;
        public float needed;

        /// <summary>Positive is a surplus to sell; negative is a hole to import or suffer.</summary>
        public float Balance { get { return produced - needed; } }

        /// <summary>Committed to contracts, and so not available at home however short
        /// the country gets. Stage 2 fills this in; until then it is zero.</summary>
        public float contracted;

        /// <summary>100 is what it cost the day the run opened. Rises as the country runs
        /// short of it.</summary>
        public float domesticPrice = 100f;
        public float worldPrice = 100f;
    }
}
