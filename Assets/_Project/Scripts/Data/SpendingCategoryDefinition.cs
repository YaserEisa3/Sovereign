using System.Collections.Generic;
using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Data
{
    [System.Serializable]
    public struct SpendingEffect
    {
        [Tooltip("What this spending line pushes on once its lag has elapsed.")]
        public ImpactTarget target;

        [Tooltip("Effect per $100B per year of spending, in that target's own units.")]
        public float strengthPer100B;

        public bool sectorSpecific;
        public SectorId sector;
    }

    /// <summary>
    /// GDD 8.2 / 11. One asset per spending line. lagQuarters is the whole point:
    /// infrastructure pays back in 2 years, education in 5, and no amount of
    /// panic spending shortens either.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Spend_", menuName = "Sovereign/Spending Category Definition")]
    public class SpendingCategoryDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Spending Line";
        [TextArea(2, 4)] public string description = "";
        public SpendingGroup group = SpendingGroup.Social;
        public int sortOrder = 0;

        [Header("Control")]
        [Tooltip("Starting level in billions of dollars per year.")]
        public float defaultBillionsPerYear = 100f;
        public float minimumBillions = 0f;
        public float maximumBillions = 2000f;
        [Tooltip("How much one press of the stepper moves this line, in $B.")]
        public float stepBillions = 5f;

        [Header("Behaviour")]
        [Tooltip("Quarters before this spending shows its effect. GDD 11: defence 0, infrastructure 8, education 20.")]
        [Range(0, 24)] public int lagQuarters = 0;

        [Tooltip("The player cannot set this line - it is computed. Debt Service and Veterans Benefits.")]
        public bool isAutomatic = false;

        [Tooltip("This line rises by itself in a downturn - the automatic stabilisers of GDD 8.3.")]
        public bool isAutomaticStabiliser = false;

        [Tooltip("How strongly this line grows as unemployment rises, in $B per point of unemployment.")]
        public float stabiliserSensitivity = 0f;

        [Header("Infrastructure")]
        [Tooltip("Which infrastructure category this line repairs, if any. GDD 16 / 22.3: six categories are tracked, one aggregate drives the GDP drag.")]
        public InfrastructureCategory infrastructureCategory = InfrastructureCategory.None;

        [Header("Effects once the lag elapses")]
        public List<SpendingEffect> effects = new List<SpendingEffect>();

        [Header("Approval")]
        [Tooltip("Approval change per $100B/yr for each group. Cutting a popular line hurts in exactly the same proportion.")]
        public float poorApproval = 0f;
        public float middleApproval = 0f;
        public float wealthyApproval = 0f;
        public float leftApproval = 0f;
        public float centreApproval = 0f;
        public float rightApproval = 0f;
    }
}
