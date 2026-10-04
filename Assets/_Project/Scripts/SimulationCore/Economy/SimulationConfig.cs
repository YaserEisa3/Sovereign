namespace Sovereign.Core
{
    /// <summary>
    /// GDD 3.2. Configuration enters the simulation as plain structs at boot. Every
    /// value here originates in a ScriptableObject asset the designer can edit; the
    /// core never reaches back out to fetch one.
    /// </summary>
    public struct MacroConfig
    {
        public float okunCoefficient;
        public float phillipsCoefficient;
        public float monetaryMultiplier;
        public float interestRateSensitivity;
        public float fdiCurrencySensitivity;
        public float tradeContagionMultiplier;
        public float financialContagionMultiplier;
        public SampledCurve debtRiskPremium;
        public int monetaryLagQuarters;
        public int qeInflationLagQuarters;

        public float naturalUnemploymentRate;
        public float baseInflationPassThrough;
        public float unanchoredInflationPassThrough;
        public float expectationsUnanchorThreshold;
        public int expectationsUnanchorYears;
        public float wageStickiness;
        public float maxQuarterlyWageDriftPercent;

        public float startingCredibility;
        public float credibilityLossPerQeAtHighInflation;
        public float credibilityGainPerYearOnTarget;
        public float inflationTarget;

        // Demand composition and responses.
        public float consumptionShare;
        public float investmentShare;
        public float governmentShare;
        public float netExportShare;
        public float consumptionTaxSensitivity;
        public float investmentCorporateTaxSensitivity;
        public float confidenceAdjustmentSpeed;
        public float exportCurrencySensitivity;
        public float tariffTradeSensitivity;
        public float importedInflationSensitivity;

        // Stabilisers.
        public float unemploymentReversionSpeed;
        public float recoveryFromSlack;
        public float fiscalMultiplier, fiscalAdjustmentSpeed;
        public float debtTrendPremium, debtTrendCredit, debtTrendPenalty;
        public float slackRecoveryChokeRate;
        public float buybackShortYears, buybackMediumYears, buybackLongYears;
        public float buybackExecutionPremiumPer100B, buybackMinPrice, buybackMaxPrice, buybackForeignSellerShare, buybackMaxShareOfStock;
        public float currencyMeanReversion;
        public float downwardWageRigidity;
        public float expectationsFloor;
        public float phillipsSlackDamping;

        // Markets.
        public float neutralRealRate;
        public float termPremium;
        public float qeYieldEffectPer100B;
        public float auctionDemandSensitivity;
        public float comfortableAuctionCover;
        public float currencyRateDifferentialWeight;
        public float currencyCurrentAccountWeight;
        public float currencyInflationWeight;
        public float currencyQeWeight;
        public float interventionEffectPer100B;

        // GDD 9.5 rating premiums, indexed by CreditRating.
        public float[] ratingPremiums;
    }

    public struct SectorConfig
    {
        public SectorId id;
        public string name;
        public float gdpShare;
        public float employmentShare;
        public float baseAverageWage;
        public float exportShare;
        public float currencyExposure;
        public float foreignCompetitionExposure;

        public float corporateTaxSensitivity;
        public float capitalGainsTaxSensitivity;
        public float interestRateSensitivity;
        public float regulationSensitivity;
        public float subsidySensitivity;
        public float consumerConfidenceSensitivity;
        public float tariffSensitivity;
        public float carbonTaxSensitivity;
        public float governmentSpendingSensitivity;

        public float wageStickiness;
        public float unemploymentWageSensitivity;
        public float immigrationWageSuppression;
    }

    public struct InfrastructureConfig
    {
        public float startingHealth;
        public float annualDegradationPoints;
        public float maintenanceFloorBillions;
        public float repairCostPerPoint;
        public SampledCurve productivityDrag;
        public float dragThreshold;
    }

    /// <summary>Everything the economy needs to start. Built once, then handed to the simulator.</summary>
    public struct SimulationConfig
    {
        public MacroConfig macro;
        public SectorConfig[] sectors;
        public InfrastructureConfig infrastructure;
        public TreasuryConfig treasury;
        public PopulationConfig population;
        public ApprovalConfig approval;
        public EventsConfig events;
        public GeopoliticsConfig geopolitics;
        public AchievementConfig achievements;
        public VictoryConfig victory;

        // The opening position, when it is not business as usual - GDD 3.5 scenarios.
        public float startingOutputGapPercent;
        public float startingProductivityScar;
        public string openingHostileNation;
        public float openingHostileRelationship;
        public bool openingHostileSanctions;
        public string[] openingBriefing;

        // News: "TOPIC|headline" lines from the headline library text asset.
        public string[] headlines;
        public int headlineIntervalWeeks;
        public int headlineTopicRest;

        /// <summary>Starting nominal GDP in billions per year.</summary>
        public float startingNominalGdp;
        /// <summary>Starting sovereign debt as a share of GDP. 1.12 is the GDD 18 dashboard's 112%.</summary>
        public float startingDebtToGdp;
        /// <summary>Trend real growth in percent per year - what the economy does when nothing is pushing it.</summary>
        public float potentialGrowthRate;
        /// <summary>Starting unemployment in percent.</summary>
        public float startingUnemployment;
        /// <summary>Starting annual inflation in percent.</summary>
        public float startingInflation;
        /// <summary>Starting central bank policy rate in percent.</summary>
        public float startingBaseRate;
        /// <summary>Starting FX reserves in billions.</summary>
        public float startingFxReserves;
        /// <summary>Weighted average coupon on the existing debt stock, in percent.</summary>
        public float startingAverageCoupon;
        /// <summary>Share of debt held abroad, 0-1.</summary>
        public float startingForeignHolding;
        /// <summary>Trend growth of the rest of the world, in percent - the demand behind your exports.</summary>
        public float worldGrowthRate;
        /// <summary>Starting infrastructure health, 0-100.</summary>
        public float startingInfrastructureHealth;
        public float startingMaturingWithinOneYear, startingMaturingOneToFive;
    }
}
