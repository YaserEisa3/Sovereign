using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>Diplomatic friction, a currency attack, a breakthrough, and people
    /// on the move. The small events that escalate into the large ones.</summary>
    public static partial class ProjectBuilder
    {
        static EventDefinition[] BuildDiplomaticEvents()
        {
            return new[]
            {
                Event("DiplomaticIncident", "Diplomatic Incident", EventCategory.DiplomaticIncident, 0, 0, 4, 26, e =>
                {
                    e.description = "An espionage accusation, an airspace violation, a summit walkout. Small on its own, and the first stage of a trade war if you answer it badly.";
                    e.baseAnnualProbability = 0.25f; e.escalationRisk = 0.30f;
                    e.impacts.Add(Hit(ImpactTarget.TradeVolume, -0.08f));
                }),

                Event("WtoComplaint", "WTO Complaint", EventCategory.DiplomaticIncident, 6, 4, 26, 78, e =>
                {
                    e.description = "Your export subsidies or your currency management have been noticed. A finding against you invites sanctioned retaliation.";
                    e.earlySignalHeadline = "Trade bloc requests consultations over subsidy practices";
                    e.imminentAdvisorLine = "A formal complaint is being filed. The subsidies are working - that is precisely the problem.";
                    e.baseAnnualProbability = 0.12f;
                    e.impacts.Add(Hit(ImpactTarget.TradeVolume, -0.15f));
                }),

                Event("CurrencyAttack", "Speculative Currency Attack", EventCategory.FinancialCrisis, 2, 1, 4, 26, e =>
                {
                    e.description = "The market has decided to find out whether you can defend your currency. A credible defence costs fewer reserves than a doubted one, and once reserves run low, defence stops being possible at all.";
                    e.earlySignalHeadline = "Short positions against the currency build sharply";
                    e.imminentAdvisorLine = "They are testing the floor. Half a defence costs more than no defence.";
                    e.baseAnnualProbability = 0.08f;
                    e.impacts.Add(Hit(ImpactTarget.CurrencyStrength, -1.0f, 0.6f));
                    e.impacts.Add(Hit(ImpactTarget.FxReserves, -10f, 0.5f));
                    e.impacts.Add(Hit(ImpactTarget.ShortYield, 0.15f));
                }),

                Event("TechRevolution", "Technology Breakthrough", EventCategory.TechRevolution, 0, 0, 52, 208, e =>
                {
                    e.description = "A genuine productivity jump. Growth without inflation for a while, concentrated in the sectors that were already winning - so watch the Gini.";
                    e.baseAnnualProbability = 0.08f;
                    e.impacts.Add(Hit(ImpactTarget.RealGdpGrowth, 0.05f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, 0.8f, SectorId.Technology));
                    e.impacts.Add(Hit(ImpactTarget.Gini, 0.0002f));
                    e.impacts.Add(Hit(ImpactTarget.Unemployment, 0.02f));
                }),

                Event("RefugeeInflow", "Refugee Inflow", EventCategory.PoliticalCrisis, 4, 2, 26, 104, e =>
                {
                    e.description = "Conflict abroad sends people across your border. Labour force up, social spending up, and the Conservative faction unhappy about both.";
                    e.earlySignalHeadline = "Displacement rises sharply along a conflict border";
                    e.imminentAdvisorLine = "Arrivals are accelerating. Housing and food assistance will absorb this whether or not you fund them.";
                    e.baseAnnualProbability = 0.14f;
                    e.impacts.Add(Hit(ImpactTarget.LaborForce, 0.5f));
                    e.impacts.Add(Hit(ImpactTarget.ApprovalOverall, -0.08f));
                    e.overlayLayerName = "UnrestLayer";
                }),
            };
        }
    }
}
