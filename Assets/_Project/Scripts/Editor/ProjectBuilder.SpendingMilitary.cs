using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 8.2 military lines. Military Personnel is the one that decides,
    /// years later, how large the veteran population and its benefits bill become.</summary>
    public static partial class ProjectBuilder
    {
        static SpendingCategoryDefinition[] BuildMilitarySpending()
        {
            return new[]
            {
                // Forces and the factories behind them employ people, which sector health
                // alone cannot express - that only shuffles workers BETWEEN industries.
                // All below civilian research and schooling, because a soldier and a tank
                // are paid for and consumed, where a road or a graduate keeps paying.
                Spend("MilitaryPersonnel", "Military Personnel", SpendingGroup.Military, 180f, 0, 200, s =>
                { s.effects.Add(FxSector(ImpactTarget.SectorHealth, 1.2f, SectorId.GovernmentMilitary));
                  s.effects.Add(Fx(ImpactTarget.RealGdpGrowth, 0.15f));
                  s.rightApproval = 1.2f; s.leftApproval = -0.6f; }),

                Spend("MilitaryEquipment", "Military Equipment", SpendingGroup.Military, 165f, 4, 210, s =>
                { s.effects.Add(FxSector(ImpactTarget.SectorHealth, 1.0f, SectorId.Manufacturing));
                  s.effects.Add(Fx(ImpactTarget.RealGdpGrowth, 0.12f));
                  s.rightApproval = 1.0f; s.leftApproval = -0.5f; }),

                Spend("MilitaryRnD", "Military R and D", SpendingGroup.Military, 145f, 12, 220, s =>
                { s.effects.Add(FxSector(ImpactTarget.SectorHealth, 0.8f, SectorId.Technology));
                  // Defence research spills into civilian industry, so it earns nearly what
                  // civilian research does.
                  s.effects.Add(Fx(ImpactTarget.RealGdpGrowth, 0.15f));
                  s.rightApproval = 0.7f; }),

                Spend("MilitaryOperations", "Military Operations", SpendingGroup.Military, 300f, 0, 230, s =>
                { // Spent abroad and burned: the least productive money in the budget.
                  s.effects.Add(Fx(ImpactTarget.RealGdpGrowth, 0.03f));
                  s.rightApproval = 0.6f; s.leftApproval = -0.7f; }),

                Spend("HomelandSecurity", "Homeland Security", SpendingGroup.Military, 75f, 0, 240, s =>
                { s.effects.Add(Fx(ImpactTarget.RealGdpGrowth, 0.08f));
                  s.rightApproval = 0.8f; s.centreApproval = 0.3f; s.leftApproval = -0.4f; }),

                Spend("VeteransBenefits", "Veterans Benefits", SpendingGroup.Military, 135f, 0, 250, s =>
                { s.isAutomatic = true;
                  s.poorApproval = 0.6f; s.middleApproval = 0.5f; s.rightApproval = 1.0f; }),

                Spend("Intelligence", "Intelligence", SpendingGroup.Military, 90f, 2, 260, s =>
                { s.effects.Add(Fx(ImpactTarget.RealGdpGrowth, 0.05f));
                  s.rightApproval = 0.5f; s.leftApproval = -0.3f; }),

                Spend("ForeignAid", "Foreign Aid", SpendingGroup.Military, 60f, 4, 270, s =>
                { s.leftApproval = 0.6f; s.rightApproval = -0.9f; s.centreApproval = 0.2f; }),
            };
        }
    }
}
