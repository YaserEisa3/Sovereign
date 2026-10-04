using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 3.5: an opening position as an asset. The country you inherit - its output,
    /// its debts, its ruins and who it just fought - is authored here, not in code, so
    /// a new scenario is a new asset rather than a new build.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Scenario", menuName = "Sovereign/Scenario")]
    public class ScenarioDefinition : ScriptableObject
    {
        [Tooltip("Shown to the player when the run starts.")]
        public string displayName = "Scenario";

        [TextArea] public string summary = "";

        public StartingConditions conditions = new StartingConditions();
    }
}
