namespace Sovereign.Core
{
    /// <summary>What a trigger condition measures.</summary>
    public enum ConditionMetric
    {
        DebtToGdp, Inflation, Unemployment, RealInterestRate, RealRateThreeYearAverage,
        CreditRating, ForeignHolding, BankCapitalRequirement, ExpectationsUnanchored,
        FxReserves, CurrencyIndex, ApprovalOverall, Year
    }

    public enum Comparison { Above, Below, AtLeast, AtMost }

    /// <summary>One gate on an event. Every gate on an event must hold for it to be
    /// able to fire at all - GDD 17.6: a banking crisis needs weak banks, a
    /// hyperinflation needs unanchored expectations.</summary>
    public struct TriggerCondition
    {
        public ConditionMetric metric;
        public Comparison comparison;
        public float threshold;
    }

    public struct ImpactConfig
    {
        public ImpactTarget target;
        public float weeklyMagnitude;
        public bool sectorSpecific;
        public SectorId sector;
        public float mitigable;
    }

    /// <summary>One EventDefinition, flattened at boot.</summary>
    public struct EventConfig
    {
        public string key;
        public string title;
        public string description;
        public string earlySignalHeadline;
        public string imminentAdvisorLine;
        public EventCategory category;
        public int warningWeeks;
        public int earlySignalWeeks;
        public int minDurationWeeks;
        public int maxDurationWeeks;
        public float escalationRisk;
        public string escalatesToKey;
        public float baseAnnualProbability;
        public int earliestYear;
        public bool uniqueWhileActive;
        public ImpactConfig[] impacts;
        public TriggerCondition[] conditions;
        public string overlayLayerName;
    }

    /// <summary>Just enough about each nation for an event to happen somewhere.</summary>
    public struct NationConfig
    {
        public string name;
        public NationArchetype archetype;
        public float startingRelationship;
        public bool isPlayer;
        public float tradeVolume;
        public float bondHolding;
    }

    /// <summary>GDD 17.3's war model, from WarParameters.</summary>
    public struct WarConfig
    {
        public SampledCurve casualtyCurve;
        public float equipmentDegradationPercentPerWeek;
        public float infrastructureDamagePerWeek;
        public float veteranConversionRate;
        public float wartimeMinimumDefenceBillions;
        public float rallyApprovalBonus;
        public float weeklyApprovalDrain;
        public float lateWarApprovalDrain;
        public int lateWarThresholdWeeks;
        public float victoryApprovalBonus;
        public float defeatApprovalPenalty;
        /// <summary>How strong an attacker is against your opening defence budget.</summary>
        public float enemyStrengthMultiple;
    }

    public struct EventsConfig
    {
        public EventConfig[] events;
        public NationConfig[] nations;
        public WarConfig war;
        public EventTuning tuning;
        public uint seed;
    }
}

namespace Sovereign.Core
{
    /// <summary>EventParameters, flattened.</summary>
    public struct EventTuning
    {
        public float severityMin, severityMax, mitigationCap;
        public float disasterFundHalfEffect, pandemicPublicHealthWeight, pandemicHealthWeight;
        public float warDefenceWeight, financialCapitalFloor, financialCapitalRange;
        public float pandemicScarThreshold, pandemicScarSize;
        public float occupationGapThreshold;
        public int warReportEveryWeeks;
        public float defaultDebtThreshold, defaultAuctionCover;
        public int defaultFailedAuctions;
        public float oilReversion, tradeReversion, tradeNetExportWeight;
        public float safeHavenCredibility, safeHavenWeeklyCurrency;
        public float bondDumpingHoldingLoss, refugeeWorkingAgeWeekly;
    }
}
