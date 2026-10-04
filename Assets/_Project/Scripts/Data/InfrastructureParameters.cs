using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 16 / 22.3. Six categories are tracked and shown; ONE weighted aggregate
    /// drives the productivity drag. This is the lever players under-fund first and
    /// regret most, which only works if the number is on screen the whole time.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_InfrastructureParameters", menuName = "Sovereign/Parameters/Infrastructure Parameters")]
    public class InfrastructureParameters : ScriptableObject
    {
        [Header("Starting health (0-100)")]
        [Range(0f, 100f)] public float startingHealth = 82f;

        [Header("Decay and repair")]
        [Tooltip("Points of health lost per year with zero spending.")]
        [Range(0f, 20f)] public float annualDegradationPoints = 3.5f;

        [Tooltip("Spending in $B/yr that merely holds health steady. The default budget sits just above it, so ignoring infrastructure costs you a point a year rather than collapsing it.")]
        public float maintenanceFloorBillions = 150f;

        [Tooltip("Cost in $B to raise aggregate health by one point, on top of the maintenance floor.")]
        public float repairCostPerPoint = 12f;

        [Header("Productivity drag")]
        [Tooltip("X = infrastructure health 0-100. Y = GDP multiplier. Should sit at 1.0 above 70 and fall away below it - GDD 16.")]
        public AnimationCurve productivityDragCurve = AnimationCurve.Linear(0f, 0.85f, 100f, 1f);

        [Tooltip("Health below which the drag becomes measurable. GDD 16 says 70.")]
        [Range(0f, 100f)] public float dragThreshold = 70f;

        [Header("Category weights into the aggregate (should sum to 1)")]
        [Range(0f, 1f)] public float roadsWeight = 0.28f;
        [Range(0f, 1f)] public float transitWeight = 0.14f;
        [Range(0f, 1f)] public float energyGridWeight = 0.24f;
        [Range(0f, 1f)] public float broadbandWeight = 0.12f;
        [Range(0f, 1f)] public float waterWeight = 0.14f;
        [Range(0f, 1f)] public float airportsWeight = 0.08f;
    }
}
