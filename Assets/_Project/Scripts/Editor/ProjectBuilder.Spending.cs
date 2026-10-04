using System.Collections.Generic;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 8.2 and 11. Thirty spending lines. The lag column is the point: defence
    /// lands this quarter, infrastructure in two years, education in five, and no
    /// amount of panic spending shortens either.
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string SpendPath = Root + "/Data/Spending/";

        static SpendingEffect Fx(ImpactTarget target, float strengthPer100B)
        {
            return new SpendingEffect { target = target, strengthPer100B = strengthPer100B };
        }

        static SpendingEffect FxSector(ImpactTarget target, float strengthPer100B, SectorId sector)
        {
            return new SpendingEffect { target = target, strengthPer100B = strengthPer100B, sectorSpecific = true, sector = sector };
        }

        static SpendingCategoryDefinition Spend(string file, string name, SpendingGroup group,
                                                float defaultBillions, int lagQuarters, int order,
                                                System.Action<SpendingCategoryDefinition> extra = null)
        {
            return Asset<SpendingCategoryDefinition>(SpendPath + "SO_Spend_" + file + ".asset", s =>
            {
                s.displayName = name;
                s.group = group;
                s.defaultBillionsPerYear = defaultBillions;
                s.minimumBillions = 0f;
                s.maximumBillions = Mathf.Max(200f, defaultBillions * 3f);
                s.stepBillions = defaultBillions >= 400f ? 25f : 5f;
                s.lagQuarters = lagQuarters;
                // Every line explains itself; a line may still override this below.
                s.description = SpendExplanation(file);
                s.sortOrder = order;
                s.effects = new List<SpendingEffect>();
                extra?.Invoke(s);
            });
        }

        static SpendingCategoryDefinition[] BuildSpendingCategories()
        {
            List<SpendingCategoryDefinition> all = new List<SpendingCategoryDefinition>();
            all.AddRange(BuildSocialSpending());
            all.AddRange(BuildMilitarySpending());
            all.AddRange(BuildInfrastructureSpending());
            all.AddRange(BuildOtherSpending());
            return all.ToArray();
        }

        static SpendingCategoryDefinition[] BuildSocialSpending()
        {
            return new[]
            {
                Spend("Medicaid", "Medicaid", SpendingGroup.Social, 620f, 0, 10, s =>
                { s.effects.Add(Fx(ImpactTarget.PopulationDeathRate, -0.12f));
                  s.poorApproval = 1.8f; s.middleApproval = 0.4f; s.leftApproval = 1.5f; s.rightApproval = -0.6f;
                  s.isAutomaticStabiliser = true; s.stabiliserSensitivity = 14f; }),

                Spend("Medicare", "Medicare", SpendingGroup.Social, 1000f, 0, 20, s =>
                { s.effects.Add(Fx(ImpactTarget.PopulationDeathRate, -0.15f));
                  s.poorApproval = 1.0f; s.middleApproval = 1.2f; s.leftApproval = 1.0f; s.rightApproval = -0.3f; }),

                Spend("PublicHealth", "Public Health", SpendingGroup.Social, 95f, 0, 30, s =>
                { s.effects.Add(Fx(ImpactTarget.PopulationDeathRate, -0.20f));
                  s.effects.Add(Fx(ImpactTarget.PopulationBirthRate, 0.06f));
                  s.poorApproval = 0.8f; s.middleApproval = 0.6f; s.leftApproval = 1.2f; }),

                Spend("SocialSecurityRetirement", "Social Security Retirement", SpendingGroup.Social, 1350f, 0, 40, s =>
                { s.effects.Add(Fx(ImpactTarget.ApprovalOverall, 0.4f));
                  s.poorApproval = 1.4f; s.middleApproval = 1.0f; s.leftApproval = 0.8f; s.rightApproval = -0.2f; }),

                Spend("SocialSecurityDisability", "Social Security Disability", SpendingGroup.Social, 145f, 0, 50, s =>
                { s.poorApproval = 1.2f; s.leftApproval = 0.8f; s.rightApproval = -0.4f; }),

                Spend("UnemploymentInsurance", "Unemployment Insurance", SpendingGroup.Social, 35f, 0, 60, s =>
                { s.isAutomaticStabiliser = true; s.stabiliserSensitivity = 26f;
                  s.effects.Add(Fx(ImpactTarget.ConsumerConfidence, 0.5f));
                  s.poorApproval = 1.6f; s.middleApproval = 0.5f; s.leftApproval = 1.0f; s.rightApproval = -0.7f; }),

                Spend("HousingAssistance", "Housing Assistance", SpendingGroup.Social, 70f, 2, 70, s =>
                { s.poorApproval = 1.5f; s.middleApproval = 0.3f; s.leftApproval = 1.0f; s.rightApproval = -0.5f; }),

                Spend("FoodAssistance", "Food Assistance", SpendingGroup.Social, 120f, 0, 80, s =>
                { s.isAutomaticStabiliser = true; s.stabiliserSensitivity = 11f;
                  s.poorApproval = 1.8f; s.leftApproval = 1.0f; s.rightApproval = -0.6f; }),

                Spend("DisasterReliefFund", "Disaster Relief Fund", SpendingGroup.Social, 30f, 0, 90, s =>
                { s.effects.Add(Fx(ImpactTarget.InfrastructureHealth, 0.3f));
                  s.middleApproval = 0.4f; s.centreApproval = 0.6f; }),

                Spend("EducationK12", "K-12 Education", SpendingGroup.Social, 80f, 20, 100, s =>
                { s.effects.Add(Fx(ImpactTarget.RealGdpGrowth, 0.25f));
                  s.middleApproval = 1.2f; s.poorApproval = 0.8f; s.leftApproval = 1.0f; }),

                Spend("EducationHigher", "Higher Education", SpendingGroup.Social, 45f, 20, 110, s =>
                { s.effects.Add(FxSector(ImpactTarget.SectorHealth, 0.6f, SectorId.Technology));
                  s.middleApproval = 0.9f; s.leftApproval = 0.9f; }),
            };
        }
    }
}
