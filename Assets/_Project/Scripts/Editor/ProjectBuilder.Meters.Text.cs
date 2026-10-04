namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 18. What each meter means, for the player who has not read the GDD. Written
    /// to answer the only two questions a bar raises: what am I looking at, and what
    /// should I do about it?
    /// </summary>
    public static partial class ProjectBuilder
    {
        static string MeterExplanation(string series)
        {
            switch (series)
            {
                case "gdpGrowth":
                    return "How fast the economy is growing, per year, after inflation. Below zero is a recession; "
                         + "much above potential and you are overheating and will pay for it in prices.";
                case "inflation":
                    return "How fast prices are rising, per year. The target is 2%. Above it wages lose ground and "
                         + "the central bank is expected to act; below zero, people stop spending and wait for things to get cheaper.";
                case "unemployment":
                    return "Share of people who want work and cannot find it. Around 4.5% is full employment. "
                         + "High unemployment costs you benefits, tax revenue and, quickly, approval.";
                case "debtToGdp":
                    return "Everything you owe, as a share of one year's output. It is not a cliff, but the higher it "
                         + "climbs the more the bond market charges you, and the more of your budget goes to interest.";
                case "budgetBalance":
                    return "Revenue minus spending this year, as a share of GDP. Negative is a deficit, which you "
                         + "borrow to cover. Deficits are normal in a slump and dangerous in a boom.";
                case "longYield":
                    return "What the market charges to lend to you for thirty years. It prices your debt, your "
                         + "inflation and whether anyone believes your plan. Every new bond you issue costs about this.";
                case "currency":
                    return "What your money is worth abroad, where 100 is where you started. Weak helps exporters and "
                         + "makes imports dear; strong does the reverse and hurts your factories.";
                case "currentAccount":
                    return "Exports minus imports, as a share of GDP. A surplus means the world is paying you; a "
                         + "deficit means you are being funded by someone, and they can stop.";
                case "approvalOverall":
                    return "What the country thinks of you. Below 30% for long enough and the government falls - "
                         + "which ends the run, whatever the economy is doing.";
                case "unrest":
                    return "How close the streets are to trouble, driven by hardship rather than opinion. Unrest "
                         + "builds slowly under a squeeze and takes far longer to settle than approval does.";
                case "infrastructure":
                    return "The condition of roads, grid, rail and water, out of 100. Below 70 it quietly drags on "
                         + "everything the economy produces, and repairs cost more the longer you leave them.";
                case "realWage":
                    return "What a typical wage actually buys, against where it started. This is the number people "
                         + "feel. It can fall while the economy grows, and that is when approval collapses.";
                default:
                    return "";
            }
        }
    }
}
