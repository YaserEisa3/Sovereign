using System.Collections.Generic;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 17. Every event in the game is an asset, never a case in a switch
    /// statement. Warning weeks are the design: a telegraphed event tests whether
    /// you were already spending in the right place, and an un-telegraphed one is
    /// a pure shock.
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string EventPath = Root + "/Data/Events/";

        static EconomicImpact Hit(ImpactTarget target, float weekly, float mitigable = 0f)
        {
            return new EconomicImpact { target = target, weeklyMagnitude = weekly, mitigable = mitigable };
        }

        static EconomicImpact HitSector(ImpactTarget target, float weekly, SectorId sector, float mitigable = 0f)
        {
            return new EconomicImpact
            {
                target = target, weeklyMagnitude = weekly,
                sectorSpecific = true, sector = sector, mitigable = mitigable
            };
        }

        static TriggerConditionData When(ConditionMetric metric, Comparison comparison, float threshold)
        {
            return new TriggerConditionData { metric = metric, comparison = comparison, threshold = threshold };
        }

        static EventDefinition Event(string file, string title, EventCategory category,
                                     int warningWeeks, int earlySignalWeeks, int minWeeks, int maxWeeks,
                                     System.Action<EventDefinition> extra)
        {
            return Asset<EventDefinition>(EventPath + "SO_Event_" + file + ".asset", e =>
            {
                e.title = title;
                e.category = category;
                e.warningWeeksBeforeStrike = warningWeeks;
                e.earlySignalWeeks = earlySignalWeeks;
                e.minDurationWeeks = minWeeks;
                e.maxDurationWeeks = maxWeeks;
                e.impacts = new List<EconomicImpact>();
                e.conditions = new List<TriggerConditionData>();
                extra?.Invoke(e);
            });
        }

        static EventDefinition[] BuildEvents()
        {
            List<EventDefinition> all = new List<EventDefinition>();
            all.AddRange(BuildWarEvents());
            all.AddRange(BuildCrisisEvents());
            all.AddRange(BuildDisasterEvents());
            all.AddRange(BuildWorldEvents());
            return all.ToArray();
        }

        static EventDefinition[] BuildWarEvents()
        {
            return new[]
            {
                Event("RegionalWar", "Regional Conflict", EventCategory.War, 8, 5, 26, 130, e =>
                {
                    e.description = "Two nations abroad have gone to war. Not your war - yet. Oil moves, shipping reroutes, and refugees start moving.";
                    e.earlySignalHeadline = "Intelligence reports suggest military buildup on a contested border";
                    e.imminentAdvisorLine = "Probability of regional conflict is now high. Review defence spending and energy exposure before it starts, not after.";
                    e.baseAnnualProbability = 0.18f; e.escalationRisk = 0.12f;
                    e.impacts.Add(Hit(ImpactTarget.OilPrice, 0.9f));
                    e.impacts.Add(Hit(ImpactTarget.TradeVolume, -0.25f));
                    e.impacts.Add(Hit(ImpactTarget.BusinessInvestment, -0.15f));
                    e.overlayLayerName = "WarZoneLayer";
                }),

                Event("TradeWar", "Trade War", EventCategory.War, 12, 8, 52, 208, e =>
                {
                    e.description = "Tariffs and counter-tariffs. A 2-3% GDP drag, manufacturing disruption and an inflation spike, and it de-escalates only if you let it.";
                    e.earlySignalHeadline = "Trade delegation walks out; tariff threats exchanged";
                    e.imminentAdvisorLine = "Counter-tariffs are being drafted. Every point of tariff you hold now is a point they will answer.";
                    e.baseAnnualProbability = 0.15f; e.escalationRisk = 0.2f;
                    e.impacts.Add(Hit(ImpactTarget.RealGdpGrowth, -0.05f));
                    e.impacts.Add(Hit(ImpactTarget.Inflation, 0.04f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.3f, SectorId.Manufacturing));
                    e.impacts.Add(Hit(ImpactTarget.TradeVolume, -0.6f));
                }),

                Event("InvasionThreat", "War Against You", EventCategory.War, 16, 10, 104, 208, e =>
                {
                    e.description = "Late game only, and only after relations have been allowed to rot. Sustain defence spending for two to four years, or accept economic occupation.";
                    e.earlySignalHeadline = "Foreign military exercises staged unusually close to your border";
                    e.imminentAdvisorLine = "This is no longer posturing. Your defence budget over the next four quarters decides the casualty rate.";
                    e.baseAnnualProbability = 0.04f; e.earliestYear = 8; e.escalationRisk = 0.35f;
                    e.impacts.Add(Hit(ImpactTarget.BusinessInvestment, -0.4f));
                    e.impacts.Add(Hit(ImpactTarget.ConsumerConfidence, -0.5f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, 0.35f, SectorId.Manufacturing));
                    e.overlayLayerName = "WarZoneLayer";
                }),

                Event("Pandemic", "Novel Pathogen", EventCategory.Pandemic, 6, 4, 52, 156, e =>
                {
                    e.description = "Four stages: outbreak, spread, active pandemic, resolution. Your public health spending AT THE MOMENT it arrives sets the severity. Underfund it and the productivity scar is permanent.";
                    e.earlySignalHeadline = "Novel pathogen reported in a foreign region; WHO monitoring";
                    e.imminentAdvisorLine = "Community transmission confirmed abroad. Whatever your health system is now is what it will be when this lands.";
                    e.baseAnnualProbability = 0.05f; e.escalationRisk = 0.25f;
                    e.impacts.Add(Hit(ImpactTarget.LaborForce, -3f, 0.7f));
                    e.impacts.Add(Hit(ImpactTarget.PopulationDeathRate, 1.2f, 0.8f));
                    e.impacts.Add(HitSector(ImpactTarget.SectorHealth, -0.8f, SectorId.ServicesRetail, 0.4f));
                    e.impacts.Add(Hit(ImpactTarget.ConsumerConfidence, -0.6f, 0.3f));
                    e.overlayLayerName = "DisasterLayer";
                }),
            };
        }
    }
}
