namespace Sovereign.Core
{
    /// <summary>
    /// GDD 6 and 16. Sector health and the infrastructure gauge.
    /// </summary>
    public partial class EconomySimulator
    {
        void UpdateSectors(EconomyState state, PolicyState policy)
        {
            float depreciation = (100f - state.currency.exchangeRateIndex) * 0.01f;
            float rateGap = state.RealInterestRate - _config.macro.neutralRealRate;
            float cycle = (state.realGdpGrowth - Potential(state)) * 2f;

            for (int i = 0; i < state.sectors.Count; i++)
            {
                EconomicSector sector = state.sectors[i];
                SectorConfig c = sector.config;

                // Regulation now comes from the Regulatory drawer: environmental and labour
                // rules for everyone, capital requirements weighing on finance.
                sector.regulationBurden = (policy.environmentalRegulation + policy.labourRegulation) * 0.5f
                                          + (c.id == SectorId.Finance ? (policy.bankingCapitalRequirement - 10f) * 3f : 0f);

                float target = 70f + cycle;
                target -= (policy.Tax(TaxKeys.CorporateIncome) - 21f) * c.corporateTaxSensitivity * 0.5f;
                target -= (policy.Tax(TaxKeys.CapitalGains) - 20f) * c.capitalGainsTaxSensitivity * 0.4f;
                target -= rateGap * c.interestRateSensitivity * 4f;
                target -= (sector.regulationBurden - 40f) * c.regulationSensitivity * 0.3f;
                target -= policy.Tax(TaxKeys.Carbon) * c.carbonTaxSensitivity * 0.06f;
                target += (state.consumerConfidence - 50f) * c.consumerConfidenceSensitivity * 0.3f;
                target += sector.subsidyLevel * c.subsidySensitivity * 0.08f;

                // GDD 10: a weak currency helps the exporters and punishes everyone who
                // buys abroad. One number, opposite signs.
                target += depreciation * c.currencyExposure * 35f;

                // A tariff shelters the sector it protects and raises costs elsewhere.
                float tariff = policy.Tax(TaxKeys.ImportTariff) + state.geopolitics.importTariffWeighted;
                target += c.id == SectorId.Manufacturing ? tariff * 0.25f : -tariff * c.tariffSensitivity * 0.12f;

                if (c.governmentSpendingSensitivity > 0f)
                {
                    float govShare = policy.TotalSpendingBillions / MathUtil.Max(1f, state.nominalGdpBillions);
                    target += (govShare - _config.macro.governmentShare) * 100f * c.governmentSpendingSensitivity;
                }

                sector.health = MathUtil.Clamp(MathUtil.Approach(sector.health, target, 0.04f), 0f, 100f);

                float competitionTarget = c.foreignCompetitionExposure * 40f - depreciation * 60f * c.currencyExposure;
                sector.foreignCompetition = MathUtil.Clamp(
                    MathUtil.Approach(sector.foreignCompetition, competitionTarget, 0.02f), 0f, 100f);
            }
        }

        /// <summary>GDD 16. Degrades every year whatever you do, and only the six
        /// spending lines hold it up. The lever players under-fund first.</summary>
        void UpdateInfrastructure(EconomyState state, PolicyState policy)
        {
            InfrastructureConfig c = _config.infrastructure;

            // Also measured in real terms, so inflation erodes the repair budget too.
            float realSpend = policy.InfrastructureSpendingBillions;
            float aboveFloor = realSpend - c.maintenanceFloorBillions;
            float repairPoints = aboveFloor / MathUtil.Max(1f, c.repairCostPerPoint);
            float annualChange = repairPoints - c.annualDegradationPoints;

            state.infrastructureHealth = MathUtil.Clamp(state.infrastructureHealth + annualChange * Weekly, 0f, 100f);
        }
    }
}
