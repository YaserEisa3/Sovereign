using System.Collections.Generic;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 17.7 and 15. The world acting on you: contagion, resource shocks,
    /// diplomacy, and the occasional piece of good news.</summary>
    public static partial class ProjectBuilder
    {
        static EventDefinition[] BuildWorldEvents()
        {
            List<EventDefinition> events = new List<EventDefinition>
            {
                Event("ForeignRecession", "Foreign Recession", EventCategory.ForeignContagion, 6, 4, 26, 104, e =>
                {
                    e.description = "A major partner contracts. Five channels reach you: trade, financial, commodity, confidence and currency. Whether it helps or hurts depends on your position, and that ambiguity is the point.";
                    e.earlySignalHeadline = "Leading indicators turn negative in a major trading partner";
                    e.imminentAdvisorLine = "A partner economy is contracting. Export orders thin first, bank losses second.";
                    e.baseAnnualProbability = 0.22f;
                    e.impacts.Add(Hit(ImpactTarget.TradeVolume, -0.4f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.5f, SectorId.Manufacturing));
                    e.impacts.Add(Hit(ImpactTarget.BusinessInvestment, -0.2f));
                    e.impacts.Add(Hit(ImpactTarget.OilPrice, -0.4f));
                }),

                Event("EmergingSouthDefault", "Sovereign Default Abroad", EventCategory.ForeignContagion, 8, 5, 26, 78, e =>
                {
                    e.description = "Emerging South asks to restructure. Refuse and it defaults - your exposed banks take the loss either way, and the contagion arrives financially instead of diplomatically.";
                    e.earlySignalHeadline = "Emerging South requests debt restructuring talks";
                    e.imminentAdvisorLine = "They cannot roll their maturities. Our banks hold the paper whatever we decide.";
                    e.baseAnnualProbability = 0.10f;
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.7f, SectorId.Finance));
                    e.impacts.Add(Hit(ImpactTarget.BusinessInvestment, -0.2f));
                }),

                Event("OilPriceShock", "Oil Supply Shock", EventCategory.ResourceShock, 2, 1, 13, 52, e =>
                {
                    e.description = "Petro-Gulf cuts supply. Energy costs spike and import inflation follows. There is nothing monetary policy can do about a supply shock except make the recession worse.";
                    e.earlySignalHeadline = "Petro-Gulf signals production quota review";
                    e.imminentAdvisorLine = "A supply cut is coming. Raising rates into this will not lower the oil price, only employment.";
                    e.baseAnnualProbability = 0.14f;
                    e.impacts.Add(Hit(ImpactTarget.OilPrice, 1.4f));
                    e.impacts.Add(Hit(ImpactTarget.Inflation, 0.08f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.4f, SectorId.ServicesRetail));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, 0.5f, SectorId.Energy));
                }),

                Event("DomesticUnrest", "Domestic Unrest", EventCategory.PoliticalCrisis, 4, 3, 8, 52, e =>
                {
                    e.description = "Strikes and demonstrations. It does not arrive out of nowhere - it arrives after months of an approval number you were already watching.";
                    e.earlySignalHeadline = "Union federations coordinate strike ballots across three sectors";
                    e.imminentAdvisorLine = "Approval has been under forty for a month. This is what that looks like on the street.";
                    e.baseAnnualProbability = 0.16f; e.escalationRisk = 0.25f;
                    e.impacts.Add(Hit(ImpactTarget.RealGdpGrowth, -0.04f));
                    e.impacts.Add(Hit(ImpactTarget.ConsumerConfidence, -0.4f));
                    e.impacts.Add(Hit(ImpactTarget.ApprovalOverall, -0.2f));
                    e.overlayLayerName = "UnrestLayer";
                }),
            };
            events.AddRange(BuildDiplomaticEvents());
            return events.ToArray();
        }
    }
}
