using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 7. The demographic engine. Section 21: demography is destiny, slowly -
    /// which is why the elasticities here are deliberately small.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_PopulationParameters", menuName = "Sovereign/Parameters/Population Parameters")]
    public class PopulationParameters : ScriptableObject
    {
        [Header("Starting population (millions)")]
        public float startingYouth = 73f;
        public float startingWorkingAge = 205f;
        public float startingRetired = 58f;
        [Tooltip("Veterans at game start, in millions. Visible on the population screen - GDD 22.4.")]
        public float startingVeterans = 16f;

        [Header("Age bands - GDD 7.1")]
        [Range(0, 30)] public int youthUpperAge = 17;
        [Range(40, 80)] public int retirementAge = 65;

        [Header("Births and deaths (per 1000 per year)")]
        public float baseBirthRate = 11.5f;
        public float baseDeathRate = 8.8f;

        [Tooltip("Death rate reduction per $100B/yr of health spending above the baseline.")]
        [Range(0f, 2f)] public float healthSpendingDeathElasticity = 0.35f;

        [Tooltip("Birth rate response to healthcare quality.")]
        [Range(0f, 2f)] public float healthSpendingBirthElasticity = 0.15f;

        [Tooltip("Birth rate reduction per point of cost-of-living index above baseline.")]
        [Range(0f, 2f)] public float costOfLivingBirthElasticity = 0.2f;

        [Header("Child tax credit - GDD 7.5 / 22.2")]
        [Tooltip("Extra births per 1000 per year per $1000 of child credit. Deliberately SMALL: real cash transfers move fertility by fractions of a percent. If you raise this until the lever feels rewarding you have built a cheat code.")]
        [Range(0f, 1f)] public float childCreditBirthElasticity = 0.08f;

        [Header("Labour force - GDD 7.3")]
        [Tooltip("Share of the WORKING-AGE band (18-64) in the labour force, 0-1. Not the headline 63%, which is measured against everyone over 16 including retirees - applied to 18-64 it undercounts the workforce by a fifth.")]
        [Range(0f, 1f)] public float baseParticipationRate = 0.78f;

        [Tooltip("How far participation falls in a long downturn - the hidden unemployment of GDD 5.")]
        [Range(0f, 0.5f)] public float discouragedWorkerEffect = 0.08f;

        [Header("Immigration - GDD 7.6 / 22.1")]
        [Tooltip("Default net working-age inflow, in millions per year.")]
        [Range(0f, 5f)] public float defaultImmigrationInflow = 1.0f;
        [Range(0f, 5f)] public float maximumImmigrationInflow = 3.0f;
        [Tooltip("How much one press of the immigration stepper moves the inflow, in millions/yr.")]
        public float immigrationStep = 0.1f;

        [Tooltip("Share of the inflow that is skilled and routes to Technology rather than Services and Agriculture.")]
        [Range(0f, 1f)] public float defaultSkilledShare = 0.2f;

        [Tooltip("GDD 22.1 - immigration must cost something or it is a free GDP button. Downward pressure on affected sector wages, in percent per million of annual inflow.")]
        [Range(0f, 5f)] public float immigrationWageSuppression = 0.6f;

        [Tooltip("Upward pressure on the housing cost index per million of annual inflow. Reaches the poor through real wages, not through a faction opinion.")]
        [Range(0f, 5f)] public float immigrationHousingPressure = 0.8f;

        [Header("Veterans")]
        [Tooltip("Veterans benefits per veteran per year, in dollars. Times the veteran count this IS the Veterans Benefits line. 8,400 x 16 million matches the $135B opening budget.")]
        public float veteranBenefitCostPerHead = 8400f;

        [Tooltip("Share of veterans lost each year to old age. The veteran count only rises when wars and a large military feed it.")]
        [Range(0f, 0.2f)] public float veteranMortality = 0.025f;

        [Tooltip("Share of the Government and Military sector that is military - the people who become veterans.")]
        [Range(0f, 1f)] public float militaryShareOfGovernment = 0.09f;
    }
}
