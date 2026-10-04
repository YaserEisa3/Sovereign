using UnityEngine;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>The six parameter assets. Every value here is straight from the GDD.</summary>
    public static partial class ProjectBuilder
    {
        const string ParamPath = Root + "/Data/Parameters/";

        static UITheme BuildTheme()
        {
            return Asset<UITheme>(ParamPath + "SO_UITheme.asset", t =>
            {
                // Field initialisers already carry the GDD 18 palette. Fonts stay
                // empty until Roboto Mono and Inter are imported.
                t.name = "SO_UITheme";
            });
        }

        static MacroParameters BuildMacroParameters()
        {
            return Asset<MacroParameters>(ParamPath + "SO_MacroParameters.asset", m =>
            {
                // Anything NOT stamped here keeps whatever the asset was first serialised
                // with, so a field added later and tuned in code never reaches the game.
                // That cost an afternoon: three "experiments" changed code the
                // simulation was not reading.
                m.recoveryFromSlack = 0.03f;
                m.slackRecoveryChokeRate = 5f;
                m.fiscalMultiplier = 0.5f;
                m.fiscalAdjustmentSpeed = 0.02f;
                m.avoidanceCurvature = 2.5f;
                m.debtTrendPremium = 0.12f;
                m.debtTrendCredit = 3f;
                m.debtTrendPenalty = 5f;

                m.okunCoefficient = 0.5f;
                m.phillipsCoefficient = 0.3f;
                m.monetaryMultiplier = 0.8f;
                m.interestRateSensitivity = 0.4f;
                m.fdiCurrencySensitivity = 0.6f;
                m.tradeContagionMultiplier = 0.5f;
                m.financialContagionMultiplier = 0.7f;

                // GDD 21: risk premiums rise sharply above 100% debt/GDP, and the
                // curve past 1.2 is what makes a debt spiral hard to escape.
                m.debtRiskPremiumCurve = Curve(
                    new Keyframe(0.0f, 0f),
                    new Keyframe(0.6f, 0.1f),
                    new Keyframe(1.0f, 0.5f),
                    new Keyframe(1.2f, 1.2f),
                    new Keyframe(1.5f, 3.2f),
                    new Keyframe(1.8f, 6.5f),
                    new Keyframe(2.2f, 12f));
            });
        }

        static PopulationParameters BuildPopulationParameters()
        {
            return Asset<PopulationParameters>(ParamPath + "SO_PopulationParameters.asset", p =>
            {
                p.startingYouth = 73f;
                p.startingWorkingAge = 205f;
                p.startingRetired = 58f;
                p.startingVeterans = 16f;
                p.baseParticipationRate = 0.78f;
                p.veteranBenefitCostPerHead = 8400f;
                p.veteranMortality = 0.025f;
                p.militaryShareOfGovernment = 0.09f;
                p.childCreditBirthElasticity = 0.08f;
                p.defaultImmigrationInflow = 1.0f;
                p.immigrationWageSuppression = 0.6f;
                p.immigrationHousingPressure = 0.8f;
            });
        }

        static WarParameters BuildWarParameters()
        {
            return Asset<WarParameters>(ParamPath + "SO_WarParameters.asset", w =>
            {
                // X = enemy strength minus your military budget, normalised.
                // Outspending them (negative X) is cheap; underspending is not linear.
                w.casualtyCurve = Curve(
                    new Keyframe(-1.0f, 15f),
                    new Keyframe(-0.5f, 60f),
                    new Keyframe(0.0f, 240f),
                    new Keyframe(0.5f, 700f),
                    new Keyframe(1.0f, 1600f));
                w.rallyApprovalBonus = 5f;
                w.weeklyApprovalDrain = 0.3f;
                w.lateWarApprovalDrain = 0.8f;
                w.lateWarThresholdWeeks = 78;
            });
        }

        static InfrastructureParameters BuildInfrastructureParameters()
        {
            return Asset<InfrastructureParameters>(ParamPath + "SO_InfrastructureParameters.asset", i =>
            {
                i.startingHealth = 82f;
                i.annualDegradationPoints = 3.5f;
                i.dragThreshold = 70f;
                // These two were left out of the stamp, so a rebuild kept the old floor
                // and the default budget sat BELOW it - infrastructure fell 37 points in
                // ten years with the player doing nothing wrong.
                i.maintenanceFloorBillions = 150f;
                i.repairCostPerPoint = 12f;

                // Flat at 1.0 above 70, then a deepening drag - GDD 16.
                i.productivityDragCurve = Curve(
                    new Keyframe(0f, 0.80f),
                    new Keyframe(40f, 0.90f),
                    new Keyframe(70f, 1.00f),
                    new Keyframe(100f, 1.03f));
            });
        }

        static GeopoliticsParameters BuildGeopoliticsParameters()
        {
            return Asset<GeopoliticsParameters>(ParamPath + "SO_GeopoliticsParameters.asset", g =>
            {
                g.relationshipDrift = 0.02f; g.capitalFlightThreshold = 25f; g.swapLineMinimumRelationship = 30f;
            });
        }

        static EventParameters BuildEventParameters()
        {
            // Field initialisers carry the defaults; stamping keeps a rebuild honest.
            return Asset<EventParameters>(ParamPath + "SO_EventParameters.asset", e =>
            {
                e.severityMin = 0.7f; e.severityMax = 1.3f; e.mitigationCap = 0.9f;
                e.enemyStrengthMultiple = 1.1f; e.defaultDebtThreshold = 2.2f;
                e.safeHavenWeeklyCurrency = 0.05f;
            });
        }

        static ApprovalWeights BuildApprovalWeights()
        {
            // Every weight is a field initialiser carrying the GDD 14 tables, and
            // each block sums to 1. Nothing to override here.
            return Asset<ApprovalWeights>(ParamPath + "SO_ApprovalWeights.asset", a =>
            {
                // The weights are field initialisers carrying the GDD 14 tables. The
                // opening approvals are the GDD 18 dashboard's example.
                a.startingPoor = 61f; a.startingMiddle = 74f; a.startingWealthy = 52f;
                a.startingLeft = 58f; a.startingCentre = 72f; a.startingRight = 49f;
                a.adjustmentSpeed = 0.04f;
            });
        }
    }
}
