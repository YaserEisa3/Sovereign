using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>The treasury's slice of the boundary: every tax and spending asset,
    /// flattened into plain line configs keyed the same way PolicyBootstrap keys them.</summary>
    public static partial class SimulationConfigBuilder
    {
        static TreasuryConfig BuildTreasury(GameDatabase database, StartingConditions starting)
        {
            TreasuryConfig treasury = new TreasuryConfig();
            treasury.openingRevenueShareOfGdp = starting.openingRevenueShareOfGdp;
            treasury.avoidanceCurvature = database.Macro != null ? database.Macro.avoidanceCurvature : 0f;
            treasury.veteranBenefitCostPerHead = database.Population != null ? database.Population.veteranBenefitCostPerHead : 0f;

            TaxDefinition[] taxes = database.Taxes;
            treasury.taxes = new TaxLineConfig[taxes.Length];
            for (int i = 0; i < taxes.Length; i++)
            {
                TaxDefinition t = taxes[i];
                treasury.taxes[i] = new TaxLineConfig
                {
                    key = PolicyBootstrap.KeyFor(t, "SO_Tax_"),
                    name = t.displayName,
                    revenueBase = t.revenueBase,
                    unit = t.unit,
                    baseShare = t.baseShare,
                    collectionEfficiency = t.collectionEfficiency,
                    baseElasticity = t.baseElasticity,
                    isCredit = t.isCredit,
                    defaultValue = t.defaultValue,
                    minimumValue = t.minimumValue,
                    maximumValue = t.maximumValue
                };
            }

            SpendingCategoryDefinition[] spending = database.SpendingCategories;
            treasury.spending = new SpendLineConfig[spending.Length];
            for (int i = 0; i < spending.Length; i++)
            {
                SpendingCategoryDefinition s = spending[i];
                treasury.spending[i] = new SpendLineConfig
                {
                    key = PolicyBootstrap.KeyFor(s, "SO_Spend_"),
                    name = s.displayName,
                    group = s.group,
                    isAutomatic = s.isAutomatic,
                    isAutomaticStabiliser = s.isAutomaticStabiliser,
                    stabiliserSensitivity = s.stabiliserSensitivity,
                    lagQuarters = s.lagQuarters
                };
            }

            MacroParameters m = database.Macro;
            treasury.bases = new TaxBaseConfig
            {
                taxableIncomeShare = m.taxableIncomeShare,
                corporateProfitShare = m.corporateProfitShare,
                capitalGainsShare = m.capitalGainsShare,
                payrollShare = m.payrollShare,
                consumerSpendingShare = m.consumerSpendingShare,
                importShare = m.importShare,
                estateShare = m.estateShare,
                financialTurnoverMultiple = m.financialTurnoverMultiple,
                carbonEmissionsGigatonnes = m.carbonEmissionsGigatonnes,
                childrenMillions = database.Population != null ? database.Population.startingYouth : 73f
            };

            return treasury;
        }
    }
}
