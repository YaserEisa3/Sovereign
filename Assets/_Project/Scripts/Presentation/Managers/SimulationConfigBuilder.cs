using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 3.2, the boundary. This is the ONLY place ScriptableObject values become
    /// plain structs. SimulationCore never sees a UnityEngine type - its assembly
    /// definition forbids it - so AnimationCurves are sampled into float arrays here
    /// and the shape crosses over without the type.
    /// </summary>
    public static partial class SimulationConfigBuilder
    {
        const int CurveSamples = 64;

        public static SimulationConfig Build(GameDatabase database, StartingConditions starting)
        {
            SimulationConfig config = new SimulationConfig();

            config.macro = BuildMacro(database.Macro);
            config.infrastructure = BuildInfrastructure(database.Infrastructure, starting);
            config.sectors = BuildSectors(database.Sectors);
            config.treasury = BuildTreasury(database, starting);
            config.population = BuildPopulation(database);
            config.approval = BuildApproval(database.ApprovalWeights);
            config.events = BuildEvents(database, starting);
            config.geopolitics = BuildGeopolitics(database);
            config.achievements = BuildAchievements(database.AchievementParameters);
            config.startingOutputGapPercent = starting.outputGapPercent;
            config.startingProductivityScar = starting.productivityScar;
            config.openingHostileNation = starting.hostileNation;
            config.openingHostileRelationship = starting.hostileRelationship;
            config.openingHostileSanctions = starting.hostileSanctions;
            config.openingBriefing = starting.briefing;

            config.victory = new VictoryConfig
            {
                years = starting.victoryYears,
                title = starting.victoryTitle,
                summary = starting.victorySummary,
                debtToGdp = starting.victoryDebtToGdp,
                unemployment = starting.victoryUnemployment,
                infrastructure = starting.victoryInfrastructure,
                realWage = starting.victoryRealWage,
                approval = starting.victoryApproval
            };
            config.headlines = database.HeadlineLibrary == null ? new string[0] : database.HeadlineLibrary.text.Split('\n');
            config.headlineIntervalWeeks = database.EventParameters.headlineIntervalWeeks;
            config.headlineTopicRest = database.EventParameters.headlineTopicRest;

            config.startingNominalGdp = starting.nominalGdpBillions;
            config.startingDebtToGdp = starting.debtToGdp;
            config.potentialGrowthRate = starting.potentialGrowthRate;
            config.startingUnemployment = starting.unemployment;
            config.startingInflation = starting.inflation;
            config.startingBaseRate = starting.centralBankRate;
            config.startingFxReserves = starting.fxReservesBillions;
            config.startingAverageCoupon = starting.averageCoupon;
            config.startingForeignHolding = starting.foreignHoldingShare;
            config.worldGrowthRate = starting.worldGrowthRate;
            config.startingMaturingWithinOneYear = starting.maturingWithinOneYear;
            config.startingMaturingOneToFive = starting.maturingOneToFive;
            config.startingInfrastructureHealth = starting.infrastructureHealth >= 0f
                ? starting.infrastructureHealth
                : database.Infrastructure.startingHealth;

            return config;
        }

        static InfrastructureConfig BuildInfrastructure(InfrastructureParameters p, StartingConditions starting)
        {
            InfrastructureConfig c = new InfrastructureConfig();
            c.startingHealth = p.startingHealth;
            c.annualDegradationPoints = p.annualDegradationPoints;
            // The floor is a big economy's maintenance bill. A smaller country's roads
            // cost less to hold steady, so it scales with the scenario's budget.
            c.maintenanceFloorBillions = p.maintenanceFloorBillions * starting.spendingScale;
            c.repairCostPerPoint = p.repairCostPerPoint;
            c.dragThreshold = p.dragThreshold;
            c.productivityDrag = Sample(p.productivityDragCurve, 0f, 100f);
            return c;
        }

        /// <summary>An AnimationCurve authored in the Inspector, flattened into floats
        /// the Unity-free assembly can read.</summary>
        static SampledCurve Sample(AnimationCurve curve, float minX, float maxX)
        {
            SampledCurve sampled = new SampledCurve();
            sampled.minX = minX;
            sampled.maxX = maxX;
            sampled.samples = new float[CurveSamples];

            for (int i = 0; i < CurveSamples; i++)
            {
                float t = (float)i / (CurveSamples - 1);
                sampled.samples[i] = curve.Evaluate(Mathf.Lerp(minX, maxX, t));
            }
            return sampled;
        }

        static MacroConfig BuildMacro(MacroParameters p)
        {
            MacroConfig m = new MacroConfig();

            m.okunCoefficient = p.okunCoefficient;
            m.phillipsCoefficient = p.phillipsCoefficient;
            m.monetaryMultiplier = p.monetaryMultiplier;
            m.interestRateSensitivity = p.interestRateSensitivity;
            m.fdiCurrencySensitivity = p.fdiCurrencySensitivity;
            m.tradeContagionMultiplier = p.tradeContagionMultiplier;
            m.financialContagionMultiplier = p.financialContagionMultiplier;
            m.monetaryLagQuarters = p.monetaryLagQuarters;
            m.qeInflationLagQuarters = p.qeInflationLagQuarters;

            // X is debt/GDP, so 0 to 2.5 covers prudent through doomed.
            m.debtRiskPremium = Sample(p.debtRiskPremiumCurve, 0f, 2.5f);

            m.naturalUnemploymentRate = p.naturalUnemploymentRate;
            m.baseInflationPassThrough = p.baseInflationPassThrough;
            m.unanchoredInflationPassThrough = p.unanchoredInflationPassThrough;
            m.expectationsUnanchorThreshold = p.expectationsUnanchorThreshold;
            m.expectationsUnanchorYears = p.expectationsUnanchorYears;
            m.wageStickiness = p.wageStickiness;
            m.maxQuarterlyWageDriftPercent = p.maxQuarterlyWageDriftPercent;

            m.startingCredibility = p.startingCredibility;
            m.credibilityLossPerQeAtHighInflation = p.credibilityLossPerQeAtHighInflation;
            m.credibilityGainPerYearOnTarget = p.credibilityGainPerYearOnTarget;
            m.inflationTarget = p.inflationTarget;

            m.consumptionShare = p.consumptionShare;
            m.investmentShare = p.investmentShare;
            m.governmentShare = p.governmentShare;
            m.netExportShare = p.netExportShare;
            m.consumptionTaxSensitivity = p.consumptionTaxSensitivity;
            m.investmentCorporateTaxSensitivity = p.investmentCorporateTaxSensitivity;
            m.confidenceAdjustmentSpeed = p.confidenceAdjustmentSpeed;
            m.exportCurrencySensitivity = p.exportCurrencySensitivity;
            m.tariffTradeSensitivity = p.tariffTradeSensitivity;
            m.importedInflationSensitivity = p.importedInflationSensitivity;

            m.unemploymentReversionSpeed = p.unemploymentReversionSpeed;
            m.reversionReferenceGap = p.reversionReferenceGap;
            m.trainingNaturalRateEffect = p.trainingNaturalRateEffect;
            m.naturalRateRange = p.naturalRateRange;
            m.naturalRateAdjustmentSpeed = p.naturalRateAdjustmentSpeed;
            m.recoveryFromSlack = p.recoveryFromSlack;
            m.debtTrendPremium = p.debtTrendPremium;
            m.debtTrendCredit = p.debtTrendCredit;
            m.debtTrendPenalty = p.debtTrendPenalty;
            m.fiscalMultiplier = p.fiscalMultiplier;
            m.fiscalAdjustmentSpeed = p.fiscalAdjustmentSpeed;
            m.growthAdjustmentSpeed = p.growthAdjustmentSpeed;
            m.slackRecoveryChokeRate = p.slackRecoveryChokeRate;
            m.buybackShortYears = p.buybackShortYears;
            m.buybackMediumYears = p.buybackMediumYears;
            m.buybackLongYears = p.buybackLongYears;
            m.buybackExecutionPremiumPer100B = p.buybackExecutionPremiumPer100B;
            m.buybackMinPrice = p.buybackMinPrice;
            m.buybackMaxPrice = p.buybackMaxPrice;
            m.buybackForeignSellerShare = p.buybackForeignSellerShare;
            m.buybackMaxShareOfStock = p.buybackMaxShareOfStock;
            m.currencyMeanReversion = p.currencyMeanReversion;
            m.downwardWageRigidity = p.downwardWageRigidity;
            m.expectationsFloor = p.expectationsFloor;
            m.phillipsSlackDamping = p.phillipsSlackDamping;

            m.neutralRealRate = p.neutralRealRate;
            m.termPremium = p.termPremium;
            m.qeYieldEffectPer100B = p.qeYieldEffectPer100B;
            m.auctionDemandSensitivity = p.auctionDemandSensitivity;
            m.comfortableAuctionCover = p.comfortableAuctionCover;
            m.currencyRateDifferentialWeight = p.currencyRateDifferentialWeight;
            m.currencyCurrentAccountWeight = p.currencyCurrentAccountWeight;
            m.currencyInflationWeight = p.currencyInflationWeight;
            m.currencyQeWeight = p.currencyQeWeight;
            m.interventionEffectPer100B = p.interventionEffectPer100B;

            // Indexed by CreditRating, so this order must match the enum.
            m.ratingPremiums = new[]
            {
                p.premiumAAA, p.premiumAA, p.premiumA, p.premiumBBB,
                p.premiumBB, p.premiumB, p.premiumCCC, p.premiumCCC * 2f
            };

            return m;
        }

        static SectorConfig[] BuildSectors(SectorDefinition[] definitions)
        {
            SectorConfig[] sectors = new SectorConfig[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                SectorDefinition d = definitions[i];
                SectorConfig c = new SectorConfig();
                c.id = d.sectorId;
                c.name = d.displayName;
                c.gdpShare = d.gdpShare;
                c.employmentShare = d.employmentShare;
                c.baseAverageWage = d.baseAverageWage;
                c.exportShare = d.exportShare;
                c.currencyExposure = d.currencyExposure;
                c.foreignCompetitionExposure = d.foreignCompetitionExposure;
                c.corporateTaxSensitivity = d.corporateTaxSensitivity;
                c.capitalGainsTaxSensitivity = d.capitalGainsTaxSensitivity;
                c.interestRateSensitivity = d.interestRateSensitivity;
                c.regulationSensitivity = d.regulationSensitivity;
                c.subsidySensitivity = d.subsidySensitivity;
                c.consumerConfidenceSensitivity = d.consumerConfidenceSensitivity;
                c.tariffSensitivity = d.tariffSensitivity;
                c.carbonTaxSensitivity = d.carbonTaxSensitivity;
                c.governmentSpendingSensitivity = d.governmentSpendingSensitivity;
                c.wageStickiness = d.wageStickiness;
                c.unemploymentWageSensitivity = d.unemploymentWageSensitivity;
                c.immigrationWageSuppression = d.immigrationWageSuppression;
                sectors[i] = c;
            }
            return sectors;
        }
    }
}
