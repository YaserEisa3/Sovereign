using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 18. How the economy turns into buildings on the map. Every threshold that
    /// decides "one more factory" or "these houses are towers now" is here, because
    /// this is the layer a designer tunes by eye until the map reads right.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_CityscapeParameters", menuName = "Sovereign/Parameters/Cityscape Parameters")]
    public class CityscapeParameters : ScriptableObject
    {
        [Header("Housing - GDD 21")]
        [Tooltip("Millions of people per house. Lower means a busier map.")]
        public float millionsPerHouse = 42f;
        [Tooltip("Most houses drawn for one country.")]
        public int maxHouses = 6;
        [Tooltip("Population (millions) at which cottages become apartment blocks.")]
        public float blockPopulation = 340f;
        [Tooltip("Population (millions) at which blocks become towers.")]
        public float towerPopulation = 420f;

        [Header("Sectors")]
        [Tooltip("Share of GDP one sector building stands for. 0.03 means a sector at 18% of GDP draws six.")]
        [Range(0.005f, 0.2f)] public float gdpSharePerBuilding = 0.06f;
        [Tooltip("Most buildings drawn for one sector.")]
        public int maxPerSector = 3;
        [Tooltip("Sector health below which a sector starts losing buildings, and at 0 keeps only one.")]
        [Range(0f, 100f)] public float healthFullOutput = 55f;

        [Header("Foreign nations")]
        [Tooltip("Share of your trade one foreign building stands for.")]
        [Range(0.01f, 0.5f)] public float tradeSharePerBuilding = 0.05f;
        [Tooltip("Most buildings drawn for a foreign nation.")]
        public int maxForeign = 4;

        [Header("Layout")]
        [Tooltip("World units between building slots across.")]
        public float slotSpacing = 0.3f;
        [Tooltip("World units between rows. Rows go downward, nearer the viewer.")]
        public float rowSpacing = 0.25f;
        [Tooltip("Slots per row before a new row starts.")]
        public int slotsPerRow = 6;
        [Tooltip("How far a building may wander from its slot, as a share of the spacing.")]
        [Range(0f, 0.5f)] public float jitter = 0.22f;
        [Tooltip("Where a cityscape sits relative to its site - or to the nation marker, if the nation has no authored sites.")]
        public Vector2 clusterOffset = new Vector2(0f, 0.09f);

        [Tooltip("Buildings of one kind that gather at a single city before spilling into the next one.")]
        public int buildingsPerSite = 3;
        [Tooltip("Slots across within one city, before a second row starts there.")]
        public int siteSlotsPerRow = 4;

        [Header("Scale and animation")]
        [Tooltip("Scale of a building at full health.")]
        public float buildingScale = 0.42f;
        [Tooltip("Scale of a foreign nation's buildings - yours is the country in focus.")]
        public float foreignScale = 0.8f;
        [Tooltip("Smallest scale a sick sector's buildings shrink to.")]
        [Range(0.2f, 1f)] public float minHealthScale = 0.55f;
        [Tooltip("Seconds a new building takes to rise, and a lost one to fall.")]
        public float growSeconds = 0.6f;
    }
}
