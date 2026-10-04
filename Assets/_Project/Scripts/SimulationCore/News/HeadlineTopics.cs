using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// What the news can be about this week. These thresholds are narrative - when a
    /// number is interesting enough to print - not economic coefficients; nothing in
    /// the simulation reads them. They live in one table so a writer tuning the
    /// ticker has a single place to look.
    /// </summary>
    public static class HeadlineTopics
    {
        public const string Fallback = "MarketCalm";

        public static List<string> TrueThisWeek(EconomyState s, PolicyState p, HeadlineFacts f)
        {
            List<string> t = new List<string>();
            float growth = s.realGdpGrowth;

            if (growth > 3.5f) t.Add("GrowthBoom");
            else if (growth >= 1.5f) t.Add("GrowthSteady");
            else if (growth >= 0f) t.Add("GrowthSlow");
            else t.Add("Recession");

            if (s.inflation > 5f) t.Add("InflationHot");
            else if (s.inflation < 0.5f) t.Add("Deflation");
            else if (s.inflation >= 1f && s.inflation <= 3f) t.Add("InflationTarget");

            if (s.unemployment < 4.5f) t.Add("JobsStrong");
            if (s.unemployment > 6.5f) t.Add("JobsWeak");
            if (s.monetary.centralBankRate > 7f) t.Add("RatesHigh");
            if (s.monetary.centralBankRate < 2f) t.Add("RatesLow");
            if (s.bonds.yields.IsInverted) t.Add("CurveInverted");
            if (s.bonds.yields.longTermYield > 9f) t.Add("YieldsSpiking");
            if (s.DebtToGdp > 1.3f) t.Add("DebtWorry");
            if (s.bonds.creditRating >= CreditRating.BB) t.Add("RatingJunk");
            if (s.currency.exchangeRateIndex > 112f) t.Add("CurrencyStrong");
            if (s.currency.exchangeRateIndex < 90f) t.Add("CurrencyWeak");
            if (s.oilPriceIndex > 130f) t.Add("OilHigh");
            if (s.oilPriceIndex < 85f) t.Add("OilLow");
            if (s.consumerConfidence > 65f) t.Add("ConfidenceHigh");
            if (s.consumerConfidence < 35f) t.Add("ConfidenceLow");
            if (s.infrastructureHealth < 65f) t.Add("InfrastructureCrumbling");
            if (s.approval.overall > 70f) t.Add("ApprovalHigh");
            if (s.approval.overall < 40f) t.Add("ApprovalLow");
            if (s.approval.HighestUnrest > 40f) t.Add("UnrestRising");
            if (s.population.DependencyRatio > 0.70f) t.Add("PopulationAging");
            if (p.immigrationInflowMillions > 2f) t.Add("ImmigrationDebate");
            if (f.tariffNation != null) t.Add("TradeTension");
            if (f.walkingCreditor != null) t.Add("CreditorWalks");
            if (f.warmestNation != null) t.Add("AllyWarm");
            if (f.coldestNation != null) t.Add("RivalCold");
            if (f.strongSector != null) t.Add("SectorBoom");
            if (f.weakSector != null) t.Add("SectorSlump");
            if (s.events.war.active) t.Add("AtWar");
            if (s.currency.fdiInflowRate > 100f) t.Add("FdiBoom");
            t.Add(Fallback);
            return t;
        }

        /// <summary>Points the headline at the right nation or sector for its topic.</summary>
        public static HeadlineFacts Focus(string topic, HeadlineFacts f)
        {
            switch (topic)
            {
                case "AllyWarm": f.nation = f.warmestNation; break;
                case "RivalCold": f.nation = f.coldestNation; break;
                case "TradeTension": f.nation = f.tariffNation; break;
                case "CreditorWalks": f.nation = f.walkingCreditor; break;
                case "SectorBoom": f.sector = f.strongSector; break;
                case "SectorSlump": f.sector = f.weakSector; break;
            }
            return f;
        }
    }
}
