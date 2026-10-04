// Pure C# — this assembly has noEngineReferences:true, so the compiler itself
// enforces GDD Section 2's boundary rule: SimulationCore cannot see UnityEngine.
// Shared vocabulary lives here so Data (ScriptableObjects) and Presentation can
// speak the same language as the simulation without the dependency running backwards.
namespace Sovereign.Core
{
    public enum SectorId
    {
        Technology, Finance, Manufacturing, Energy, Agriculture, ServicesRetail, GovernmentMilitary
    }

    public enum NationArchetype
    {
        HomeNation, LargeDiversified, ExportManufacturer, OilState, EmergingDebtor, FinancialHaven, ResourceDemocracy
    }

    public enum IncomeClassId { Poor, Middle, Wealthy }

    public enum FactionId { ProgressiveLeft, Centrist, ConservativeRight }

    public enum AgeBandId { Youth, WorkingAge, Retired }

    public enum EventWarningLevel { None, EarlySignal, Imminent, Active }

    public enum EventCategory
    {
        War, Pandemic, NaturalDisaster, FinancialCrisis,
        PoliticalCrisis, ForeignContagion, TechRevolution,
        ResourceShock, DiplomaticIncident
    }

    public enum CreditRating { AAA, AA, A, BBB, BB, B, CCC, D }

    public enum BondMaturityPreference { ShortHeavy, Balanced, LongHeavy }

    public enum ForwardGuidance { Dovish, Neutral, Hawkish }

    /// <summary>GDD 4. One real second is one in-game week at 1x.</summary>
    public enum GameSpeed { Paused = 0, Normal = 1, Double = 2, Quadruple = 4 }

    public enum SpendingGroup { Social, Military, Infrastructure, Other, Automatic }

    public enum InfrastructureCategory { None, Roads, Transit, EnergyGrid, Broadband, Water, Airports }

    /// <summary>What a tax is levied on — the pool the rate multiplies against.</summary>
    public enum RevenueBase
    {
        TaxableIncomePool, CorporateProfits, CapitalGains, Payroll,
        ConsumerSpending, CarbonEmissions, Imports, Estates, FinancialTransactions,
        PerChildCredit
    }

    /// <summary>Units a policy control is expressed in — drives the stepper display.</summary>
    public enum PolicyUnit { Percent, DollarsPerYearBillions, DollarsPerUnit, DollarsPerTon }

    /// <summary>Everything an event or a spending line is allowed to push on.</summary>
    public enum ImpactTarget
    {
        RealGdpGrowth, Inflation, Unemployment, ConsumerConfidence, BusinessInvestment,
        CurrencyStrength, ShortYield, LongYield, OilPrice, TradeVolume,
        InfrastructureHealth, PopulationDeathRate, PopulationBirthRate, LaborForce,
        SectorHealth, FxReserves, DebtStock, ApprovalOverall, Gini, MoneySupply
    }
}
