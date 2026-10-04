using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>Nations and the geopolitics tuning across the boundary.</summary>
    public static partial class SimulationConfigBuilder
    {
        static GeopoliticsConfig BuildGeopolitics(GameDatabase database)
        {
            NationDefinition[] nations = database.Nations;
            GeopoliticsConfig config = new GeopoliticsConfig { nations = new NationProfile[nations.Length] };

            for (int i = 0; i < nations.Length; i++)
            {
                NationDefinition n = nations[i];
                config.nations[i] = new NationProfile
                {
                    name = n.displayName, archetype = n.archetype, isPlayer = n.isPlayerNation,
                    startingRelationship = n.startingRelationship, tradeVolume = n.tradeVolumePercent,
                    bondHolding = n.bondHoldingPercent, financialLinkage = n.financialLinkage, trendGrowth = n.trendGrowthRate,
                    tariffRetaliationThreshold = n.tariffRetaliationThreshold,
                    quietBondReductionRelationship = n.quietBondReductionRelationship,
                    activeBondDumpingRelationship = n.activeBondDumpingRelationship,
                    sanctionsRelationship = n.sanctionsRelationship,
                    signatureMoveIntervalYears = n.signatureMoveIntervalYears, aggression = n.aggression
                };
            }

            GeopoliticsParameters g = database.GeopoliticsParameters;
            config.tuning = new GeopoliticsTuning
            {
                relationshipDrift = g.relationshipDrift, tariffRelationshipPenalty = g.tariffRelationshipPenalty,
                agreementBonus = g.agreementBonus, sanctionPenalty = g.sanctionPenalty,
                aidRelationshipPer10B = g.aidRelationshipPer10B, swapLineBonus = g.swapLineBonus, withdrawalScar = g.withdrawalScar,
                tradeLossPerTariffPoint = g.tradeLossPerTariffPoint, agreementTradeBonus = g.agreementTradeBonus,
                sanctionTradeLoss = g.sanctionTradeLoss, retaliationMatch = g.retaliationMatch, tariffInflation = g.tariffInflation,
                foreignDemandWhenBuying = g.foreignDemandWhenBuying, foreignDemandWhenStopped = g.foreignDemandWhenStopped,
                capitalFlightThreshold = g.capitalFlightThreshold, capitalFlightPerPoint = g.capitalFlightPerPoint,
                capitalFlightCap = g.capitalFlightCap, manipulationSurplus = g.manipulationSurplus,
                rivalDevaluation = g.rivalDevaluation, devaluationManufacturingHit = g.devaluationManufacturingHit,
                swapLineMinimumRelationship = g.swapLineMinimumRelationship, swapLineCapacityBillions = g.swapLineCapacityBillions,
                exportSubsidyBoostPer100B = g.exportSubsidyBoostPer100B, exportSubsidyWtoThreshold = g.exportSubsidyWtoThreshold
            };
            return config;
        }
    }
}
