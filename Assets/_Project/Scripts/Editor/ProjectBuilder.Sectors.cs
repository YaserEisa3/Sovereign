using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>The seven sectors of GDD 6. GDP shares sum to 1.00 with Government
    /// at zero - the document lists its GDP share as a dash, because government
    /// output is spending, not private value added. Employment shares sum to 1.00.</summary>
    public static partial class ProjectBuilder
    {
        const string SectorPath = Root + "/Data/Sectors/";

        static SectorDefinition[] BuildSectors()
        {
            return new[]
            {
                Asset<SectorDefinition>(SectorPath + "SO_Sector_Technology.asset", s =>
                {
                    s.displayName = "Technology"; s.sectorId = SectorId.Technology;
                    s.gdpShare = 0.18f; s.employmentShare = 0.08f; s.baseAverageWage = 115000f;
                    s.exportShare = 0.35f; s.currencyExposure = 0f; s.foreignCompetitionExposure = 0.30f;
                    s.corporateTaxSensitivity = 0.45f; s.capitalGainsTaxSensitivity = 0.55f;
                    s.interestRateSensitivity = 0.55f; s.regulationSensitivity = 0.35f;
                    s.subsidySensitivity = 0.30f; s.consumerConfidenceSensitivity = 0.20f;
                    s.tariffSensitivity = 0.25f; s.carbonTaxSensitivity = 0.05f;
                    s.wageStickiness = 0.35f; s.unemploymentWageSensitivity = 0.30f;
                    s.immigrationWageSuppression = 0.25f;
                    s.chartColor = new Color32(0x4f, 0xc3, 0xf7, 0xff);
                }),

                Asset<SectorDefinition>(SectorPath + "SO_Sector_Finance.asset", s =>
                {
                    s.displayName = "Finance"; s.sectorId = SectorId.Finance;
                    s.gdpShare = 0.14f; s.employmentShare = 0.05f; s.baseAverageWage = 105000f;
                    s.exportShare = 0.20f; s.currencyExposure = -0.50f; s.foreignCompetitionExposure = 0.20f;
                    s.corporateTaxSensitivity = 0.40f; s.capitalGainsTaxSensitivity = 0.50f;
                    s.interestRateSensitivity = 0.70f; s.regulationSensitivity = 0.60f;
                    s.subsidySensitivity = 0.05f; s.consumerConfidenceSensitivity = 0.25f;
                    s.tariffSensitivity = 0.10f; s.carbonTaxSensitivity = 0.02f;
                    s.wageStickiness = 0.40f; s.unemploymentWageSensitivity = 0.35f;
                    s.immigrationWageSuppression = 0.10f;
                    s.chartColor = new Color32(0x9c, 0x6a, 0xde, 0xff);
                }),

                Asset<SectorDefinition>(SectorPath + "SO_Sector_Manufacturing.asset", s =>
                {
                    s.displayName = "Manufacturing"; s.sectorId = SectorId.Manufacturing;
                    s.gdpShare = 0.12f; s.employmentShare = 0.15f; s.baseAverageWage = 78000f;
                    // The one sector a weak currency genuinely helps - GDD 6 and 10.
                    s.exportShare = 0.45f; s.currencyExposure = 0.80f; s.foreignCompetitionExposure = 0.75f;
                    s.corporateTaxSensitivity = 0.55f; s.capitalGainsTaxSensitivity = 0.20f;
                    s.interestRateSensitivity = 0.45f; s.regulationSensitivity = 0.40f;
                    s.subsidySensitivity = 0.45f; s.consumerConfidenceSensitivity = 0.30f;
                    s.tariffSensitivity = 0.70f; s.carbonTaxSensitivity = 0.35f;
                    s.wageStickiness = 0.20f; s.unemploymentWageSensitivity = 0.70f;
                    s.immigrationWageSuppression = 0.50f;
                    s.chartColor = new Color32(0xf5, 0xa6, 0x23, 0xff);
                }),

                Asset<SectorDefinition>(SectorPath + "SO_Sector_Energy.asset", s =>
                {
                    s.displayName = "Energy"; s.sectorId = SectorId.Energy;
                    s.gdpShare = 0.10f; s.employmentShare = 0.06f; s.baseAverageWage = 92000f;
                    s.exportShare = 0.25f; s.currencyExposure = -0.60f; s.foreignCompetitionExposure = 0.35f;
                    s.corporateTaxSensitivity = 0.35f; s.capitalGainsTaxSensitivity = 0.15f;
                    s.interestRateSensitivity = 0.30f; s.regulationSensitivity = 0.65f;
                    s.subsidySensitivity = 0.50f; s.consumerConfidenceSensitivity = 0.15f;
                    s.tariffSensitivity = 0.30f; s.carbonTaxSensitivity = 0.90f;
                    s.wageStickiness = 0.25f; s.unemploymentWageSensitivity = 0.50f;
                    s.immigrationWageSuppression = 0.20f;
                    s.chartColor = new Color32(0xff, 0x70, 0x43, 0xff);
                }),

                Asset<SectorDefinition>(SectorPath + "SO_Sector_Agriculture.asset", s =>
                {
                    s.displayName = "Agriculture"; s.sectorId = SectorId.Agriculture;
                    s.gdpShare = 0.06f; s.employmentShare = 0.10f; s.baseAverageWage = 38000f;
                    s.exportShare = 0.40f; s.currencyExposure = 0.60f; s.foreignCompetitionExposure = 0.60f;
                    s.corporateTaxSensitivity = 0.20f; s.capitalGainsTaxSensitivity = 0.10f;
                    s.interestRateSensitivity = 0.35f; s.regulationSensitivity = 0.40f;
                    s.subsidySensitivity = 0.80f; s.consumerConfidenceSensitivity = 0.10f;
                    s.tariffSensitivity = 0.55f; s.carbonTaxSensitivity = 0.25f;
                    s.wageStickiness = 0.15f; s.unemploymentWageSensitivity = 0.80f;
                    s.immigrationWageSuppression = 0.90f;
                    s.chartColor = new Color32(0x7c, 0xb3, 0x42, 0xff);
                }),

                Asset<SectorDefinition>(SectorPath + "SO_Sector_ServicesRetail.asset", s =>
                {
                    s.displayName = "Services and Retail"; s.sectorId = SectorId.ServicesRetail;
                    s.gdpShare = 0.40f; s.employmentShare = 0.40f; s.baseAverageWage = 42000f;
                    s.exportShare = 0.08f; s.currencyExposure = -0.40f; s.foreignCompetitionExposure = 0.20f;
                    s.corporateTaxSensitivity = 0.25f; s.capitalGainsTaxSensitivity = 0.10f;
                    s.interestRateSensitivity = 0.35f; s.regulationSensitivity = 0.25f;
                    s.subsidySensitivity = 0.10f; s.consumerConfidenceSensitivity = 0.80f;
                    s.tariffSensitivity = 0.25f; s.carbonTaxSensitivity = 0.10f;
                    s.wageStickiness = 0.15f; s.unemploymentWageSensitivity = 0.90f;
                    s.immigrationWageSuppression = 0.80f;
                    s.chartColor = new Color32(0x7a, 0x9c, 0xc5, 0xff);
                }),

                Asset<SectorDefinition>(SectorPath + "SO_Sector_GovernmentMilitary.asset", s =>
                {
                    s.displayName = "Government and Military"; s.sectorId = SectorId.GovernmentMilitary;
                    s.gdpShare = 0f; s.employmentShare = 0.16f;
                    // Blend of the $58k civilian and $68k military wages in GDD 6.
                    s.baseAverageWage = 61000f;
                    s.exportShare = 0f; s.currencyExposure = 0f; s.foreignCompetitionExposure = 0f;
                    s.corporateTaxSensitivity = 0f; s.capitalGainsTaxSensitivity = 0f;
                    s.interestRateSensitivity = 0.05f; s.regulationSensitivity = 0.05f;
                    s.subsidySensitivity = 0f; s.consumerConfidenceSensitivity = 0.05f;
                    s.tariffSensitivity = 0f; s.carbonTaxSensitivity = 0.05f;
                    s.governmentSpendingSensitivity = 1f;
                    s.wageStickiness = 0.50f; s.unemploymentWageSensitivity = 0.10f;
                    s.immigrationWageSuppression = 0f;
                    s.chartColor = new Color32(0x8b, 0x97, 0xa8, 0xff);
                }),
            };
        }
    }
}
