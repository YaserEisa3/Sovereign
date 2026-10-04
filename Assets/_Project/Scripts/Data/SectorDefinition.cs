using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 6. One asset per sector, seven in total. baseAverageWage is the STARTING
    /// wage - the runtime value floats around it (GDD 22.5) so wages can chase prices.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Sector_", menuName = "Sovereign/Sector Definition")]
    public class SectorDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Sector";
        public SectorId sectorId = SectorId.Technology;
        [Tooltip("Colour for this sector everywhere in the UI.")]
        public Color chartColor = Color.cyan;

        [Header("Share of the economy")]
        [Tooltip("Share of GDP at game start, 0-1. The seven sectors should sum to 1.")]
        [Range(0f, 1f)] public float gdpShare = 0.1f;

        [Tooltip("Share of total employment at game start, 0-1. The seven sectors should sum to 1.")]
        [Range(0f, 1f)] public float employmentShare = 0.1f;

        [Tooltip("Starting average annual wage in dollars. Headcount x this wage is this sector's contribution to the national taxable income pool (GDD 7.4).")]
        public float baseAverageWage = 50000f;

        [Header("External exposure")]
        [Tooltip("Share of this sector's output that is exported, 0-1.")]
        [Range(0f, 1f)] public float exportShare = 0.2f;

        [Tooltip("Exchange rate exposure. Positive means a weak currency HELPS this sector (Manufacturing, Agriculture); negative means a weak currency HURTS it (Energy, Services). GDD 10.")]
        [Range(-1f, 1f)] public float currencyExposure = 0f;

        [Tooltip("How exposed this sector is to import competition when the currency is strong.")]
        [Range(0f, 1f)] public float foreignCompetitionExposure = 0.3f;

        [Header("Policy sensitivities (0 = immune, 1 = fully exposed)")]
        [Range(0f, 1f)] public float corporateTaxSensitivity = 0.4f;
        [Range(0f, 1f)] public float capitalGainsTaxSensitivity = 0.2f;
        [Range(0f, 1f)] public float interestRateSensitivity = 0.4f;
        [Range(0f, 1f)] public float regulationSensitivity = 0.3f;
        [Range(0f, 1f)] public float subsidySensitivity = 0.2f;
        [Range(0f, 1f)] public float consumerConfidenceSensitivity = 0.3f;
        [Range(0f, 1f)] public float tariffSensitivity = 0.3f;
        [Range(0f, 1f)] public float carbonTaxSensitivity = 0.1f;
        [Tooltip("How strongly direct government spending drives this sector's headcount. 1 for Government and Military.")]
        [Range(0f, 1f)] public float governmentSpendingSensitivity = 0f;

        [Header("Wage dynamics - GDD 22.5")]
        [Tooltip("Per-sector override of how fast wages close the gap to target.")]
        [Range(0.01f, 1f)] public float wageStickiness = 0.25f;

        [Tooltip("How strongly labour-market slack pulls this sector's wages. High for Services, low for Technology.")]
        [Range(0f, 2f)] public float unemploymentWageSensitivity = 0.6f;

        [Tooltip("How much immigration inflow suppresses this sector's wages. GDD 22.1 - immigration has to cost something.")]
        [Range(0f, 2f)] public float immigrationWageSuppression = 0f;
    }
}
