using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 17.6. Financial crises - the ones you cause yourself, arriving
    /// with little or no warning because markets do not send calendar invitations.</summary>
    public static partial class ProjectBuilder
    {
        static EventDefinition[] BuildCrisisEvents()
        {
            return new[]
            {
                Event("BankingCrisis", "Banking Crisis", EventCategory.FinancialCrisis, 3, 2, 26, 104, e =>
                {
                    e.description = "Low capital requirements, high household debt and an asset price collapse. Credit crunch, then investment collapse. Bail them out (hated, fast) or let them fail (popular, depression risk).";
                    e.earlySignalHeadline = "Interbank lending rates widen sharply overnight";
                    e.imminentAdvisorLine = "Two mid-size lenders are struggling to fund themselves. Capital requirements you set years ago decide how this ends.";
                    e.baseAnnualProbability = 0.07f; e.escalationRisk = 0.3f;
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -1.2f, SectorId.Finance, 0.5f));
                    e.impacts.Add(Hit(ImpactTarget.BusinessInvestment, -0.7f));
                    e.impacts.Add(Hit(ImpactTarget.RealGdpGrowth, -0.08f));
                    e.impacts.Add(Hit(ImpactTarget.Unemployment, 0.05f));
                }),

                Event("HousingCollapse", "Housing Collapse", EventCategory.FinancialCrisis, 0, 0, 104, 260, e =>
                {
                    e.description = "Rates held too low for too long, overleveraged households, a construction bubble. Wealth destruction, then a spending crash. Poor and middle hit hardest, and recovery takes three to five years minimum.";
                    e.baseAnnualProbability = 0.05f;
                    e.impacts.Add(Hit(ImpactTarget.ConsumerConfidence, -0.8f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.9f, SectorId.Finance));
                    e.impacts.Add(Hit(ImpactTarget.RealGdpGrowth, -0.06f));
                    e.impacts.Add(Hit(ImpactTarget.Gini, 0.0003f));
                }),

                Event("HyperinflationSpiral", "Hyperinflation Spiral", EventCategory.FinancialCrisis, 0, 0, 52, 208, e =>
                {
                    e.description = "Excessive QE, a monetised deficit and a credibility collapse. Self-reinforcing, and near-certain game over if it is not caught early.";
                    e.baseAnnualProbability = 0.02f; e.escalationRisk = 0.5f;
                    e.impacts.Add(Hit(ImpactTarget.Inflation, 0.6f));
                    e.impacts.Add(Hit(ImpactTarget.CurrencyStrength, -1.2f));
                    e.impacts.Add(Hit(ImpactTarget.ConsumerConfidence, -1.0f));
                    e.impacts.Add(Hit(ImpactTarget.LongYield, 0.25f));
                }),

                Event("DebtCrisis", "Sovereign Debt Crisis", EventCategory.FinancialCrisis, 4, 2, 52, 208, e =>
                {
                    e.description = "Debt above 180% of GDP, a junk rating, rising yields and a primary deficit. Austerity, an IMF bailout, or default. One of those ends the run.";
                    e.earlySignalHeadline = "Rating agency places sovereign debt on watch negative";
                    e.imminentAdvisorLine = "The next auction is the one that matters. If it fails, yields do not come back down on their own.";
                    e.baseAnnualProbability = 0.06f; e.escalationRisk = 0.4f;
                    e.impacts.Add(Hit(ImpactTarget.ShortYield, 0.2f));
                    e.impacts.Add(Hit(ImpactTarget.LongYield, 0.3f));
                    e.impacts.Add(Hit(ImpactTarget.CurrencyStrength, -0.5f));
                }),

                Event("BondDumpingAttack", "Foreign Bond Dumping", EventCategory.FinancialCrisis, 2, 1, 13, 52, e =>
                {
                    e.description = "A creditor nation stops buying, then starts selling. Yields spike, the currency comes under pressure and the market panics. Relations you let rot are now a balance sheet problem.";
                    e.earlySignalHeadline = "Foreign participation at this quarter's auction was unusually thin";
                    e.imminentAdvisorLine = "A major holder is reducing its position. If this becomes open selling, we lose control of the long end.";
                    e.baseAnnualProbability = 0.05f;
                    e.impacts.Add(Hit(ImpactTarget.LongYield, 0.35f));
                    e.impacts.Add(Hit(ImpactTarget.CurrencyStrength, -0.8f));
                    e.impacts.Add(Hit(ImpactTarget.FxReserves, -6f));
                    e.impacts.Add(Hit(ImpactTarget.ConsumerConfidence, -0.3f));
                }),
            };
        }
    }
}
