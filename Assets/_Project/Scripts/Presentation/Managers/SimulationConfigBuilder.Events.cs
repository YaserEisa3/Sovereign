using System.Collections.Generic;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>Events, nations, the war model and the event tuning across the boundary.</summary>
    public static partial class SimulationConfigBuilder
    {
        static EventsConfig BuildEvents(GameDatabase database, StartingConditions starting)
        {
            EventsConfig config = new EventsConfig();

            EventDefinition[] definitions = database.Events;
            config.events = new EventConfig[definitions.Length];
            for (int i = 0; i < definitions.Length; i++) config.events[i] = BuildEvent(definitions[i]);

            NationDefinition[] nations = database.Nations;
            config.nations = new NationConfig[nations.Length];
            for (int i = 0; i < nations.Length; i++)
            {
                config.nations[i] = new NationConfig
                {
                    name = nations[i].displayName,
                    archetype = nations[i].archetype,
                    isPlayer = nations[i].isPlayerNation,
                    startingRelationship = nations[i].startingRelationship,
                    tradeVolume = nations[i].tradeVolumePercent,
                    bondHolding = nations[i].bondHoldingPercent
                };
            }

            config.war = BuildWar(database.War, database.EventParameters);
            config.tuning = BuildTuning(database.EventParameters);

            // 0 means "a new history every run"; anything else replays exactly.
            config.seed = starting.randomSeed != 0
                ? (uint)starting.randomSeed
                : (uint)System.Environment.TickCount | 1u;
            return config;
        }

        static EventConfig BuildEvent(EventDefinition d)
        {
            List<ImpactConfig> impacts = new List<ImpactConfig>();
            foreach (EconomicImpact impact in d.impacts)
                impacts.Add(new ImpactConfig
                {
                    target = impact.target, weeklyMagnitude = impact.weeklyMagnitude,
                    sectorSpecific = impact.sectorSpecific, sector = impact.sector, mitigable = impact.mitigable
                });

            List<TriggerCondition> conditions = new List<TriggerCondition>();
            if (d.conditions != null)
                foreach (TriggerConditionData c in d.conditions)
                    conditions.Add(new TriggerCondition { metric = c.metric, comparison = c.comparison, threshold = c.threshold });

            return new EventConfig
            {
                key = d.name.StartsWith("SO_Event_") ? d.name.Substring("SO_Event_".Length) : d.name,
                title = d.title,
                description = d.description,
                earlySignalHeadline = d.earlySignalHeadline,
                imminentAdvisorLine = d.imminentAdvisorLine,
                category = d.category,
                warningWeeks = d.warningWeeksBeforeStrike,
                earlySignalWeeks = d.earlySignalWeeks,
                minDurationWeeks = d.minDurationWeeks,
                maxDurationWeeks = Mathf.Max(d.minDurationWeeks, d.maxDurationWeeks),
                escalationRisk = d.escalationRisk,
                escalatesToKey = d.escalatesTo == null ? "" : d.escalatesTo.name.Replace("SO_Event_", ""),
                baseAnnualProbability = d.baseAnnualProbability,
                earliestYear = d.earliestYear,
                uniqueWhileActive = d.uniqueWhileActive,
                impacts = impacts.ToArray(),
                conditions = conditions.ToArray(),
                overlayLayerName = d.overlayLayerName
            };
        }

        static WarConfig BuildWar(WarParameters w, EventParameters e)
        {
            return new WarConfig
            {
                casualtyCurve = Sample(w.casualtyCurve, -1f, 1f),
                equipmentDegradationPercentPerWeek = w.equipmentDegradationPercentPerWeek,
                infrastructureDamagePerWeek = w.infrastructureDamagePerWeek,
                veteranConversionRate = w.veteranConversionRate,
                wartimeMinimumDefenceBillions = w.wartimeMinimumDefenceBillions,
                rallyApprovalBonus = w.rallyApprovalBonus,
                weeklyApprovalDrain = w.weeklyApprovalDrain,
                lateWarApprovalDrain = w.lateWarApprovalDrain,
                lateWarThresholdWeeks = w.lateWarThresholdWeeks,
                victoryApprovalBonus = w.victoryApprovalBonus,
                defeatApprovalPenalty = w.defeatApprovalPenalty,
                enemyStrengthMultiple = e.enemyStrengthMultiple
            };
        }

        static EventTuning BuildTuning(EventParameters e)
        {
            return new EventTuning
            {
                severityMin = e.severityMin, severityMax = e.severityMax, mitigationCap = e.mitigationCap,
                disasterFundHalfEffect = e.disasterFundHalfEffect,
                pandemicPublicHealthWeight = e.pandemicPublicHealthWeight, pandemicHealthWeight = e.pandemicHealthWeight,
                warDefenceWeight = e.warDefenceWeight,
                financialCapitalFloor = e.financialCapitalFloor, financialCapitalRange = e.financialCapitalRange,
                pandemicScarThreshold = e.pandemicScarThreshold, pandemicScarSize = e.pandemicScarSize,
                occupationGapThreshold = e.occupationGapThreshold, warReportEveryWeeks = e.warReportEveryWeeks,
                defaultDebtThreshold = e.defaultDebtThreshold, defaultAuctionCover = e.defaultAuctionCover,
                defaultFailedAuctions = e.defaultFailedAuctions,
                oilReversion = e.oilReversion, tradeReversion = e.tradeReversion, tradeNetExportWeight = e.tradeNetExportWeight,
                safeHavenCredibility = e.safeHavenCredibility, safeHavenWeeklyCurrency = e.safeHavenWeeklyCurrency,
                bondDumpingHoldingLoss = e.bondDumpingHoldingLoss, refugeeWorkingAgeWeekly = e.refugeeWorkingAgeWeekly
            };
        }
    }
}
