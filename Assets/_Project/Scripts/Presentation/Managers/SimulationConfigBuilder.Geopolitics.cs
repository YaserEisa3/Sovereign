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
                    signatureMoveIntervalYears = n.signatureMoveIntervalYears, aggression = n.aggression,
                    exports = ExportsOf(n.archetype), needs = NeedsOf(n.archetype)
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
        /// <summary>
        /// GDD 19. What each kind of country has to sell. Set by archetype rather than
        /// authored per nation, because it is the archetype that says what a place IS:
        /// an oil state sells crude and gas and will never grow its own grain, and a
        /// financial haven sells services and buys literally everything physical.
        /// </summary>
        static ProductId[] ExportsOf(NationArchetype archetype)
        {
            switch (archetype)
            {
                case NationArchetype.OilState:
                    return new[] { ProductId.CrudeOil, ProductId.NaturalGas };
                case NationArchetype.ExportManufacturer:
                    return new[] { ProductId.Vehicles, ProductId.Machinery, ProductId.Semiconductors, ProductId.ConsumerGoods };
                case NationArchetype.FinancialHaven:
                    return new[] { ProductId.FinancialServices, ProductId.Tourism };
                case NationArchetype.ResourceDemocracy:
                    return new[] { ProductId.Grain, ProductId.MeatDairy, ProductId.Steel, ProductId.CrudeOil };
                case NationArchetype.EmergingDebtor:
                    return new[] { ProductId.Produce, ProductId.Steel, ProductId.Tourism };
                default:
                    return new[] { ProductId.Software, ProductId.Vehicles, ProductId.FinancialServices };
            }
        }

        /// <summary>And what it cannot make enough of, which is what it will pay for.</summary>
        static ProductId[] NeedsOf(NationArchetype archetype)
        {
            switch (archetype)
            {
                case NationArchetype.OilState:
                    return new[] { ProductId.Grain, ProductId.MeatDairy, ProductId.Produce, ProductId.Machinery, ProductId.Arms };
                case NationArchetype.ExportManufacturer:
                    return new[] { ProductId.CrudeOil, ProductId.NaturalGas, ProductId.Grain, ProductId.Steel };
                case NationArchetype.FinancialHaven:
                    return new[] { ProductId.Grain, ProductId.Produce, ProductId.Electricity, ProductId.ConsumerGoods, ProductId.Vehicles };
                case NationArchetype.ResourceDemocracy:
                    return new[] { ProductId.Semiconductors, ProductId.Machinery, ProductId.Software, ProductId.DefenceServices };
                case NationArchetype.EmergingDebtor:
                    return new[] { ProductId.Electricity, ProductId.Machinery, ProductId.Semiconductors, ProductId.Arms };
                default:
                    return new[] { ProductId.CrudeOil, ProductId.Semiconductors, ProductId.Grain };
            }
        }

    }
}
