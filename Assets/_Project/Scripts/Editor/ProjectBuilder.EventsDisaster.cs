using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 17.5. Five disaster types. Damage is reduced by the Disaster Relief Fund
    /// balance at the moment of impact - which is why every impact here is mitigable.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static EventDefinition[] BuildDisasterEvents()
        {
            return new[]
            {
                Event("Earthquake", "Major Earthquake", EventCategory.NaturalDisaster, 0, 0, 8, 26, e =>
                {
                    e.description = "No warning. Infrastructure destruction, a reconstruction bill, and regional output lost for months.";
                    e.baseAnnualProbability = 0.06f;
                    e.impacts.Add(Hit(ImpactTarget.InfrastructureHealth, -0.9f, 0.7f));
                    e.impacts.Add(Hit(ImpactTarget.RealGdpGrowth, -0.05f, 0.4f));
                    e.impacts.Add(Hit(ImpactTarget.PopulationDeathRate, 0.08f, 0.6f));
                    e.overlayLayerName = "DisasterLayer";
                }),

                Event("HurricaneSeason", "Hurricane Season", EventCategory.NaturalDisaster, 3, 2, 4, 13, e =>
                {
                    e.description = "Coastal damage, agriculture and energy disruption. Two to four weeks of warning is enough to top up the relief fund - if you have the fiscal room.";
                    e.earlySignalHeadline = "Meteorological service forecasts an above-average storm season";
                    e.imminentAdvisorLine = "Landfall expected within the fortnight. The relief fund balance today is the damage reduction you get.";
                    e.baseAnnualProbability = 0.35f;
                    e.impacts.Add(Hit(ImpactTarget.InfrastructureHealth, -0.5f, 0.8f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.7f, SectorId.Agriculture, 0.6f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.5f, SectorId.Energy, 0.6f));
                    e.overlayLayerName = "DisasterLayer";
                }),

                Event("Drought", "Drought", EventCategory.NaturalDisaster, 10, 7, 26, 78, e =>
                {
                    e.description = "Agriculture collapses slowly, food prices rise, and rural unrest follows. The longest warning of any disaster, and the easiest to ignore.";
                    e.earlySignalHeadline = "Reservoir levels fall below seasonal norms across the farm belt";
                    e.imminentAdvisorLine = "The harvest forecast has been cut again. Expect food price inflation and pressure on farm subsidies.";
                    e.baseAnnualProbability = 0.18f;
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.9f, SectorId.Agriculture, 0.5f));
                    e.impacts.Add(Hit(ImpactTarget.Inflation, 0.05f, 0.3f));
                    e.overlayLayerName = "DisasterLayer";
                }),

                Event("Flooding", "Major Flooding", EventCategory.NaturalDisaster, 2, 1, 4, 17, e =>
                {
                    e.description = "Infrastructure damage, displacement and lost harvest. Water and roads take the worst of it.";
                    e.earlySignalHeadline = "River levels rising; flood defences under review";
                    e.imminentAdvisorLine = "Evacuation orders are being drafted. Water and road infrastructure will take the damage.";
                    e.baseAnnualProbability = 0.22f;
                    e.impacts.Add(Hit(ImpactTarget.InfrastructureHealth, -0.6f, 0.75f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.6f, SectorId.Agriculture, 0.5f));
                    e.overlayLayerName = "DisasterLayer";
                }),

                Event("WildfireSeason", "Wildfire Season", EventCategory.NaturalDisaster, 3, 2, 8, 17, e =>
                {
                    e.description = "Energy and agriculture disruption, and an insurance bill the finance sector has to absorb. A cluster of these under lax banking regulation is how a disaster becomes a financial crisis.";
                    e.earlySignalHeadline = "Fire risk index reaches extreme across three regions";
                    e.imminentAdvisorLine = "Conditions are critical. Insurers are already re-pricing; watch the finance sector, not just the relief fund.";
                    e.baseAnnualProbability = 0.28f;
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.5f, SectorId.Energy, 0.6f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.4f, SectorId.Finance, 0.4f));
                    e.impacts.Add(Hit(ImpactTarget.InfrastructureHealth, -0.3f, 0.7f));
                    e.overlayLayerName = "DisasterLayer";
                }),
            };
        }
    }
}
