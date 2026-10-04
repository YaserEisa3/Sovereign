namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 8. What each tax IS, for the player who has never read a budget. One or
    /// two sentences: what it is charged on, who ends up paying it, and the catch.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static string TaxExplanation(string key)
        {
            switch (key)
            {
                case "CorporateIncome":
                    return "A tax on company profits. Paid by firms, but it lands on investment and wages too, "
                         + "and above 25% Island Finance starts quietly hosting your capital.";
                case "IncomeTop":
                    return "The rate on the highest band of earnings. Raises real money from few people, and the "
                         + "risk is that those people can leave.";
                case "IncomeUpperMiddle":
                    return "The rate on professional salaries - the band above the middle and below the top. "
                         + "A wide base that notices every point you add.";
                case "IncomeMiddle":
                    return "The rate on ordinary salaries. Hits consumption directly: the middle class spends what "
                         + "it keeps, so raising this slows the shops as well as filling the treasury.";
                case "IncomeLower":
                    return "The rate on the lowest band of earnings. It raises little and costs a lot of goodwill, "
                         + "because the people paying it have no margin.";
                case "CapitalGains":
                    return "A tax on profits from selling assets - shares, property, a business. Paid almost "
                         + "entirely by the wealthy, and the base moves abroad faster than wages can.";
                case "Payroll":
                    return "A tax on wages, charged as a share of every pay packet. A cost of employing people, "
                         + "and the funding line social programmes lean on - which is why it raises the most.";
                case "ValueAdded":
                    return "VAT, or sales tax: a tax on consumer spending, collected at each stage on the value a "
                         + "business adds and paid at the till. The biggest base you have, and regressive by "
                         + "construction - it takes the largest share from the people with the smallest margin.";
                case "Carbon":
                    return "A charge per tonne of emissions. It prices pollution, pushes energy to clean up, and "
                         + "passes straight into household bills while it does so.";
                case "Estate":
                    return "A tax on wealth passed on at death. Almost nobody pays it, the wealthy plan around it, "
                         + "and it collects barely half of what it looks like on paper.";
                case "FinancialTransaction":
                    return "A fraction of a percent on trades. Tiny rate, enormous base, and the base runs away "
                         + "fastest of all - trading moves to whichever market does not charge it.";
                case "ImportTariff":
                    return "A tax on everything you import, across the board. Protects manufacturing, raises "
                         + "consumer prices, and invites retaliation above 20%.";
                case "ChildCredit":
                    return "Money paid TO families per child, so it costs revenue rather than raising it. It moves "
                         + "the birth rate barely, and pays back in workers in twenty years. GDD 22.2.";
                default:
                    return "";
            }
        }
    }
}
