namespace Sovereign.Core
{
    /// <summary>
    /// GDD 19. A signed obligation to deliver a product to a nation, at an agreed price,
    /// for an agreed term.
    ///
    /// The commitment is the point. A flexible willingness to trade is a tap you open
    /// once and forget; a contract is a bet that you will still have the goods in five
    /// years. Sign to sell grain, then watch a drought halve the harvest, and you are
    /// still obliged - deliver and your own people go short, break it and the country
    /// you broke it with remembers.
    /// </summary>
    [System.Serializable]
    public class TradeContract
    {
        public ProductId product;
        /// <summary>Index into the geopolitics nation list.</summary>
        public int nation;
        public string nationName;

        /// <summary>$B a year at base prices - the same units production is measured in.</summary>
        public float volumeBillions;

        /// <summary>Price index agreed at signing. 100 is the world price the day it was
        /// signed; a contract does not reprice when the market does, which is the whole
        /// risk on both sides.</summary>
        public float agreedPrice;

        public int signedWeek;
        public int weeksRemaining;

        /// <summary>What this earns a year at the agreed price.</summary>
        public float EarningsPerYear { get { return volumeBillions * agreedPrice * 0.01f; } }

        public float YearsRemaining { get { return weeksRemaining / 52f; } }
    }
}
