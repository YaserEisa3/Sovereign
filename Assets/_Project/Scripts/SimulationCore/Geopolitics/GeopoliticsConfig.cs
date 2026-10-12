namespace Sovereign.Core
{
    /// <summary>Everything about a nation the AI needs, from its NationDefinition.</summary>
    public struct NationProfile
    {
        public string name;
        public NationArchetype archetype;
        public bool isPlayer;
        public float startingRelationship;
        public float tradeVolume;
        public float bondHolding;
        public float financialLinkage;
        public float trendGrowth;
        public float tariffRetaliationThreshold;
        public float quietBondReductionRelationship;
        public float activeBondDumpingRelationship;
        public float sanctionsRelationship;
        public float signatureMoveIntervalYears;
        public float aggression;

        /// <summary>GDD 19. What this country is good at making, and what it cannot make
        /// enough of. Geography and history, not policy: an oil state has crude and gas
        /// to sell and has to buy its food, and no amount of spending changes that.
        /// These are what a trade deal is FOR - selling your surplus to somebody whose
        /// ground will not grow it.</summary>
        public ProductId[] exports;
        public ProductId[] needs;
    }

    /// <summary>GeopoliticsParameters, flattened.</summary>
    public struct GeopoliticsTuning
    {
        public float relationshipDrift, tariffRelationshipPenalty, agreementBonus, sanctionPenalty;
        public float aidRelationshipPer10B, swapLineBonus, withdrawalScar;
        public float tradeLossPerTariffPoint, agreementTradeBonus, sanctionTradeLoss, retaliationMatch, tariffInflation;
        public float foreignDemandWhenBuying, foreignDemandWhenStopped;
        public float capitalFlightThreshold, capitalFlightPerPoint, capitalFlightCap;
        public float manipulationSurplus, rivalDevaluation, devaluationManufacturingHit;
        public float swapLineMinimumRelationship, swapLineCapacityBillions;
        public float exportSubsidyBoostPer100B, exportSubsidyWtoThreshold;
    }

    public struct GeopoliticsConfig
    {
        public NationProfile[] nations;
        public GeopoliticsTuning tuning;
    }
}
