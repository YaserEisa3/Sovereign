using System.Collections.Generic;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 18. A country's economy as buildings on its own territory. Houses appear
    /// as the population grows and upgrade from cottages to blocks to towers; each
    /// sector raises its own silhouette, as many as its share of GDP is worth, shrunk
    /// when its health is poor. Foreign nations show what their archetype is known
    /// for, sized by how much of your trade runs through them.
    ///
    /// Buildings are spread across the CitySite_* children placed on the nation - a
    /// sector settles in one city and spills into the next as it grows, while housing
    /// spreads across all of them, so a growing country fills up rather than piling
    /// everything under its flag.
    ///
    /// Presentation only: it reads state and moves transforms. Nothing here decides
    /// anything about the economy.
    /// </summary>
    public partial class CityscapeView : MonoBehaviour
    {
        [SerializeField] SimulationRunner runner;
        [SerializeField] CityscapeParameters parameters;

        [Tooltip("Index into the nation list. 0 is the player's own country.")]
        [SerializeField] int nationIndex;
        [SerializeField] bool isHomeNation;

        [Tooltip("Cottage, block, tower - in that order.")]
        [SerializeField] GameObject[] housingPrefabs = new GameObject[0];
        [Tooltip("One per sector, in SectorId order.")]
        [SerializeField] GameObject[] sectorPrefabs = new GameObject[0];
        [Tooltip("Worked fields. Agriculture draws these alongside its barns.")]
        [SerializeField] GameObject farmlandPrefab;
        [Tooltip("Which sector a foreign nation shows: its archetype's trade.")]
        [SerializeField] SectorId foreignSector = SectorId.Manufacturing;

        [Tooltip("Mesh_Land - plots are nudged inland until they stand on it, so nothing is built at sea.")]
        [SerializeField] Mesh landMesh;

        class Building
        {
            public GameObject instance;
            public float target;      // scale it is growing toward, 0 means it is leaving
            public float current;
            public int site, slot;
        }

        /// <summary>One town: a place on the map and the plots laid out around it.</summary>
        class Site
        {
            public Transform root;
            public int next;
            public readonly List<int> free = new List<int>();
        }

        readonly List<Building> _buildings = new List<Building>();
        readonly Dictionary<string, List<Building>> _wanted = new Dictionary<string, List<Building>>();
        readonly List<Site> _sites = new List<Site>();

        void Awake()
        {
            // Every authored city gets its own container, so the hierarchy reads as
            // "this country, these towns, these buildings".
            foreach (Transform child in transform)
                if (child.name.StartsWith("CitySite_")) _sites.Add(NewSite(child));

            // A nation with no authored cities still builds, beside its flag.
            if (_sites.Count == 0) _sites.Add(NewSite(transform));
            CacheLand();
        }

        Site NewSite(Transform anchor)
        {
            Transform root = anchor.Find("Cityscape");
            if (root == null)
            {
                root = new GameObject("Cityscape").transform;
                root.SetParent(anchor, false);
            }
            root.localPosition = parameters == null ? Vector3.zero : (Vector3)parameters.clusterOffset;
            return new Site { root = root };
        }

        void OnEnable() { if (runner != null) runner.OnWeekTick += Refresh; }
        void OnDisable() { if (runner != null) runner.OnWeekTick -= Refresh; }
        void Start() { if (runner != null && runner.State != null) Refresh(runner.State); }

        void Refresh(EconomyState state)
        {
            if (parameters == null || state == null) return;
            if (isHomeNation) RefreshHome(state); else RefreshForeign(state);
        }

        void RefreshHome(EconomyState state)
        {
            float population = state.population.Total;
            int tier = population >= parameters.towerPopulation ? 2
                     : population >= parameters.blockPopulation ? 1 : 0;

            int houses = Mathf.Clamp(Mathf.RoundToInt(population / Mathf.Max(1f, parameters.millionsPerHouse)),
                                     1, parameters.maxHouses);

            // A tier change replaces the houses rather than stacking two kinds. Housing
            // takes one plot per town, so people live everywhere.
            for (int t = 0; t < housingPrefabs.Length; t++)
                Want("housing" + t, housingPrefabs[t], t == tier ? houses : 0, 1f, 1);

            foreach (EconomicSector sector in state.sectors)
            {
                GameObject prefab = PrefabFor(sector.id);
                if (prefab == null) continue;

                int wanted = Mathf.Clamp(Mathf.RoundToInt(sector.gdpShare / Mathf.Max(0.001f, parameters.gdpSharePerBuilding)),
                                         0, parameters.maxPerSector);
                // A failing sector loses buildings AND the survivors shrink, so decline
                // reads as decline rather than as a sudden demolition.
                float health = Mathf.Clamp01(sector.health / Mathf.Max(1f, parameters.healthFullOutput));
                if (wanted > 0) wanted = Mathf.Max(1, Mathf.RoundToInt(wanted * Mathf.Clamp01(0.4f + health * 0.6f)));

                float scale = Mathf.Lerp(parameters.minHealthScale, 1f, health);

                // Farming is fields first and buildings second, so half of what
                // agriculture raises is worked land rather than another barn.
                if (sector.id == SectorId.Agriculture && farmlandPrefab != null)
                {
                    int fields = wanted - wanted / 2;
                    Want("farmland", farmlandPrefab, fields, scale, parameters.buildingsPerSite);
                    wanted -= fields;
                }

                Want("sector" + (int)sector.id, prefab, wanted, scale, parameters.buildingsPerSite);
            }

            Apply();
        }

        void RefreshForeign(EconomyState state)
        {
            if (nationIndex < 0 || nationIndex >= state.geopolitics.nations.Count || runner.Simulator == null) return;

            NationProfile profile = runner.Simulator.Geopolitics.Config.nations[nationIndex];
            NationState nation = state.geopolitics.nations[nationIndex];

            int wanted = Mathf.Clamp(Mathf.RoundToInt(profile.tradeVolume / Mathf.Max(0.001f, parameters.tradeSharePerBuilding)),
                                     1, parameters.maxForeign);
            // Sanctions close the shutters: a partner you cannot trade with goes quiet.
            if (nation.sanctioningYou) wanted = Mathf.Max(1, wanted / 2);

            // A farming nation shows fields, like yours does.
            if (foreignSector == SectorId.Agriculture && farmlandPrefab != null)
            {
                int fields = wanted - wanted / 2;
                Want("foreignFarmland", farmlandPrefab, fields, parameters.foreignScale, parameters.buildingsPerSite);
                wanted -= fields;
            }

            Want("foreign", PrefabFor(foreignSector), wanted, parameters.foreignScale, parameters.buildingsPerSite);
            Want("foreignHousing", housingPrefabs.Length > 0 ? housingPrefabs[0] : null, 2, parameters.foreignScale, 1);
            Apply();
        }

        GameObject PrefabFor(SectorId id)
        {
            int index = (int)id;
            return index >= 0 && index < sectorPrefabs.Length ? sectorPrefabs[index] : null;
        }

        /// <summary>
        /// Records how many of one kind the country should have and at what scale, and
        /// places any new ones. perSite decides whether this kind gathers in one town
        /// (a sector) or takes a plot in each of them (housing).
        /// </summary>
        void Want(string kind, GameObject prefab, int count, float scale, int perSite)
        {
            if (prefab == null) return;

            List<Building> mine;
            if (!_wanted.TryGetValue(kind, out mine)) { mine = new List<Building>(); _wanted[kind] = mine; }

            while (mine.Count < count)
            {
                int index = mine.Count;
                // Each kind starts in its own town, so farms, works and banks are not
                // all neighbours; then it spills into the next town as it grows.
                int site = (KindHash(kind) + index / Mathf.Max(1, perSite)) % _sites.Count;
                int slot = TakeSlot(site);

                GameObject instance = Instantiate(prefab, _sites[site].root);
                instance.name = prefab.name + "_" + index;
                instance.transform.localPosition = OnDryLand(_sites[site].root, SlotPosition(site, slot));
                instance.transform.localScale = Vector3.zero;

                Building building = new Building { instance = instance, site = site, slot = slot, current = 0f };
                mine.Add(building);
                _buildings.Add(building);
            }

            for (int i = 0; i < mine.Count; i++)
                mine[i].target = i < count ? scale * parameters.buildingScale : 0f;
        }

        static int KindHash(string kind)
        {
            int hash = 0;
            foreach (char c in kind) hash = (hash * 31 + c) & 0x7fffffff;
            return hash;
        }

        int TakeSlot(int site)
        {
            Site town = _sites[site];
            if (town.free.Count == 0) return town.next++;
            int slot = town.free[town.free.Count - 1];
            town.free.RemoveAt(town.free.Count - 1);
            return slot;
        }

        /// <summary>A jittered grid within one town, deterministic per plot, so
        /// buildings never dance between frames and the same economy always draws the
        /// same country.</summary>
        Vector3 SlotPosition(int site, int slot)
        {
            int perRow = Mathf.Max(1, parameters.siteSlotsPerRow);
            int row = slot / perRow;
            int column = slot % perRow;

            int seed = slot + 1 + site * 97 + nationIndex * 131;
            float hashX = Mathf.Repeat(Mathf.Sin(seed * 12.9898f) * 43758.5453f, 1f);
            float hashY = Mathf.Repeat(Mathf.Sin(seed * 39.3468f) * 24634.6345f, 1f);

            // Columns are centred on the site: a half-built town should straddle its own
            // city, not trail off to the west of it and into the sea.
            float centred = (column - (perRow - 1) * 0.5f) * parameters.slotSpacing;
            return new Vector3(
                centred + (hashX - 0.5f) * parameters.slotSpacing * parameters.jitter,
                -row * parameters.rowSpacing + (hashY - 0.5f) * parameters.rowSpacing * parameters.jitter,
                // Lower rows draw in front, so a town has depth.
                row * -0.01f);
        }

        /// <summary>Clears out what has finished falling and gives its plot back, so a
        /// country that has swapped cottages for towers a dozen times does not sprawl
        /// across the map with the ghosts of old slots.</summary>
        void Apply()
        {
            foreach (List<Building> kind in _wanted.Values)
                for (int i = kind.Count - 1; i >= 0; i--)
                {
                    Building building = kind[i];
                    if (building.target > 0f || building.current > 0.001f) continue;
                    if (building.instance != null) Destroy(building.instance);
                    _sites[building.site].free.Add(building.slot);
                    _buildings.Remove(building);
                    kind.RemoveAt(i);
                }
        }

        void Update()
        {
            if (parameters == null) return;
            float step = Time.deltaTime / Mathf.Max(0.01f, parameters.growSeconds);

            for (int i = _buildings.Count - 1; i >= 0; i--)
            {
                Building building = _buildings[i];
                if (building.instance == null) { _buildings.RemoveAt(i); continue; }
                if (Mathf.Approximately(building.current, building.target)) continue;

                building.current = Mathf.MoveTowards(building.current, building.target, step);
                building.instance.transform.localScale = Vector3.one * building.current;
                building.instance.SetActive(building.current > 0.001f);
            }
        }

        /// <summary>What is standing right now - the smoke test counts these.</summary>
        public int BuildingCount
        {
            get
            {
                int standing = 0;
                foreach (Building building in _buildings)
                    if (building.instance != null && building.target > 0f) standing++;
                return standing;
            }
        }

        /// <summary>How many of this country's towns have anything in them.</summary>
        public int OccupiedSites
        {
            get
            {
                HashSet<int> sites = new HashSet<int>();
                foreach (Building building in _buildings)
                    if (building.instance != null && building.target > 0f) sites.Add(building.site);
                return sites.Count;
            }
        }
    }
}
