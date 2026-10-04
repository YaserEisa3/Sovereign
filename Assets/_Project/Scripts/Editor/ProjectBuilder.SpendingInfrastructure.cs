using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 8.2, 16 and 22.3. Six infrastructure lines, each repairing one category.
    /// Six categories are tracked and shown; ONE weighted aggregate drives the GDP
    /// drag, so the breakdown tells you where to spend without adding a second
    /// economic system. Plus the "Other" lines and Debt Service, which you cannot cut.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static SpendingCategoryDefinition[] BuildInfrastructureSpending()
        {
            return new[]
            {
                Spend("InfraRoads", "Roads", SpendingGroup.Infrastructure, 75f, 8, 300, s =>
                { s.infrastructureCategory = InfrastructureCategory.Roads;
                  s.effects.Add(Fx(ImpactTarget.InfrastructureHealth, 1.4f));
                  s.middleApproval = 0.5f; s.centreApproval = 0.4f; }),

                Spend("InfraTransit", "Transit", SpendingGroup.Infrastructure, 25f, 8, 310, s =>
                { s.infrastructureCategory = InfrastructureCategory.Transit;
                  s.effects.Add(Fx(ImpactTarget.InfrastructureHealth, 1.2f));
                  s.middleApproval = 0.4f; s.leftApproval = 0.6f; }),

                Spend("InfraEnergyGrid", "Energy Grid", SpendingGroup.Infrastructure, 30f, 8, 320, s =>
                { s.infrastructureCategory = InfrastructureCategory.EnergyGrid;
                  s.effects.Add(Fx(ImpactTarget.InfrastructureHealth, 1.3f));
                  s.effects.Add(FxSector(ImpactTarget.SectorHealth, 0.8f, SectorId.Energy));
                  s.centreApproval = 0.4f; }),

                Spend("InfraBroadband", "Broadband", SpendingGroup.Infrastructure, 15f, 8, 330, s =>
                { s.infrastructureCategory = InfrastructureCategory.Broadband;
                  s.effects.Add(Fx(ImpactTarget.InfrastructureHealth, 1.0f));
                  s.effects.Add(FxSector(ImpactTarget.SectorHealth, 0.9f, SectorId.Technology)); }),

                Spend("InfraWater", "Water", SpendingGroup.Infrastructure, 20f, 8, 340, s =>
                { s.infrastructureCategory = InfrastructureCategory.Water;
                  s.effects.Add(Fx(ImpactTarget.InfrastructureHealth, 1.1f));
                  s.effects.Add(FxSector(ImpactTarget.SectorHealth, 0.6f, SectorId.Agriculture));
                  s.poorApproval = 0.4f; }),

                Spend("InfraAirports", "Airports", SpendingGroup.Infrastructure, 12f, 8, 350, s =>
                { s.infrastructureCategory = InfrastructureCategory.Airports;
                  s.effects.Add(Fx(ImpactTarget.InfrastructureHealth, 0.8f));
                  s.wealthyApproval = 0.3f; }),
            };
        }

        static SpendingCategoryDefinition[] BuildOtherSpending()
        {
            return new[]
            {
                Spend("ScienceRnD", "Science and R and D", SpendingGroup.Other, 90f, 12, 400, s =>
                { s.effects.Add(FxSector(ImpactTarget.SectorHealth, 1.1f, SectorId.Technology));
                  s.effects.Add(Fx(ImpactTarget.RealGdpGrowth, 0.18f));
                  s.middleApproval = 0.4f; s.leftApproval = 0.5f; }),

                Spend("SpaceProgram", "Space Program", SpendingGroup.Other, 25f, 12, 410, s =>
                { s.effects.Add(FxSector(ImpactTarget.SectorHealth, 0.7f, SectorId.Technology));
                  s.middleApproval = 0.3f; s.centreApproval = 0.2f; }),

                Spend("AgriculturalSubsidies", "Agricultural Subsidies", SpendingGroup.Other, 30f, 2, 420, s =>
                { s.effects.Add(FxSector(ImpactTarget.SectorHealth, 2.2f, SectorId.Agriculture));
                  s.rightApproval = 0.5f; s.poorApproval = 0.2f; }),

                Spend("EnergySubsidies", "Energy Subsidies", SpendingGroup.Other, 18f, 2, 430, s =>
                { s.effects.Add(FxSector(ImpactTarget.SectorHealth, 1.8f, SectorId.Energy));
                  s.effects.Add(Fx(ImpactTarget.Inflation, -0.06f));
                  s.rightApproval = 0.3f; s.leftApproval = -0.4f; }),

                Spend("DebtService", "Debt Service", SpendingGroup.Automatic, 880f, 0, 500, s =>
                { s.isAutomatic = true; }),
            };
        }
    }
}
