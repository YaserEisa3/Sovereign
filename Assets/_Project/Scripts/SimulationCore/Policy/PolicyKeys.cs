namespace Sovereign.Core
{
    /// <summary>
    /// The handful of policy lines the economy reads by name. The keys match the
    /// ScriptableObject file suffixes (SO_Tax_CorporateIncome, SO_Spend_InfraRoads),
    /// so adding an asset adds a line the treasury tracks without touching the core,
    /// while the lines with real economic transmission stay named and greppable.
    /// </summary>
    public static class TaxKeys
    {
        public const string CorporateIncome = "CorporateIncome";
        public const string IncomeTop = "IncomeTop";
        public const string IncomeUpperMiddle = "IncomeUpperMiddle";
        public const string IncomeMiddle = "IncomeMiddle";
        public const string IncomeLower = "IncomeLower";
        public const string CapitalGains = "CapitalGains";
        public const string Payroll = "Payroll";
        public const string ValueAdded = "ValueAdded";
        public const string Carbon = "Carbon";
        public const string Estate = "Estate";
        public const string FinancialTransaction = "FinancialTransaction";
        public const string ImportTariff = "ImportTariff";
        public const string ChildCredit = "ChildCredit";
    }

    public static class SpendKeys
    {
        public const string Medicaid = "Medicaid";
        public const string Medicare = "Medicare";
        public const string PublicHealth = "PublicHealth";
        public const string UnemploymentInsurance = "UnemploymentInsurance";
        public const string FoodAssistance = "FoodAssistance";
        public const string DisasterReliefFund = "DisasterReliefFund";
        public const string EducationK12 = "EducationK12";
        public const string MilitaryPersonnel = "MilitaryPersonnel";
        public const string MilitaryOperations = "MilitaryOperations";
        public const string VeteransBenefits = "VeteransBenefits";
        public const string DebtService = "DebtService";
        public const string ScienceRnD = "ScienceRnD";

        /// <summary>The six infrastructure lines, which together drive GDD 16's gauge.</summary>
        public static readonly string[] Infrastructure =
        {
            "InfraRoads", "InfraTransit", "InfraEnergyGrid", "InfraBroadband", "InfraWater", "InfraAirports"
        };
    }
}
