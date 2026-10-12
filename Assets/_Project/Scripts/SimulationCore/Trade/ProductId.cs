namespace Sovereign.Core
{
    /// <summary>
    /// GDD 19. What the country actually makes, two or three things per sector. The
    /// point of naming them is that a sector in good health is no longer an abstract
    /// number: it is grain in silos or chips in crates, which somebody else may need
    /// and your own people certainly do.
    /// </summary>
    public enum ProductId
    {
        Grain, MeatDairy, Produce,
        CrudeOil, NaturalGas, Electricity,
        Steel, Vehicles, Machinery,
        Semiconductors, Software,
        ConsumerGoods, Tourism,
        FinancialServices,
        Arms, DefenceServices
    }
}
