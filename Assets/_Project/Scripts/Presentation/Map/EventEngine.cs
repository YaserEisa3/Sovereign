using System.Collections.Generic;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 3.3, 3.4 and 17. The presentation half of the events system, on the
    /// EventEngine GameObject from Phase 0. The logic lives in SimulationCore; this
    /// puts it on the map - an instance of each event's marker prefab, under the
    /// overlay layer placed for it in the scene, at the nation it happened to. It
    /// also plants factory icons on the home nation while a weak currency is pulling
    /// in foreign investment (GDD 10).
    /// </summary>
    public class EventEngine : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] SimulationRunner runner;
        [SerializeField] GameDatabase database;

        [Header("Overlay layers - GDD 3.3")]
        [SerializeField] Transform warZoneLayer;
        [SerializeField] Transform disasterLayer;
        [SerializeField] Transform unrestLayer;
        [SerializeField] Transform fdiLayer;

        [Header("Nations, in GameDatabase order")]
        [SerializeField] Transform[] nationMarkers;

        [Header("Placement")]
        [Tooltip("Offset from the nation marker, so an event sits beside the country rather than on top of it.")]
        [SerializeField] Vector2 markerOffset = new Vector2(1.05f, 1.75f);
        [Tooltip("Extra spacing when several events hit the same country.")]
        [SerializeField] float stackSpacing = 0.42f;
        [SerializeField] float markerDepth = -0.3f;

        [Header("Foreign investment - GDD 10")]
        [SerializeField] GameObject factoryIconPrefab;
        [Tooltip("FDI inflow ($B/yr) per factory icon shown on the home nation.")]
        [SerializeField] float fdiPerIcon = 60f;
        [SerializeField] int maxFactoryIcons = 5;

        readonly Dictionary<int, GameObject> _markers = new Dictionary<int, GameObject>();
        readonly List<GameObject> _factories = new List<GameObject>();
        EconomyState _trackedState;

        void OnEnable()
        {
            if (runner != null) runner.OnWeekTick += Sync;
        }

        void OnDisable()
        {
            if (runner != null) runner.OnWeekTick -= Sync;
        }

        void Start()
        {
            if (runner != null && runner.State != null) Sync(runner.State);
        }

        void Sync(EconomyState state)
        {
            // A new run is a new world: clear the old one's markers.
            if (!ReferenceEquals(state, _trackedState)) { ClearAll(); _trackedState = state; }

            HashSet<int> live = new HashSet<int>();
            Dictionary<int, int> perNation = new Dictionary<int, int>();

            foreach (EventInstance e in state.events.live)
            {
                if (!e.IsActive) continue;
                live.Add(e.id);

                int stack;
                perNation.TryGetValue(e.nationIndex, out stack);
                perNation[e.nationIndex] = stack + 1;

                if (!_markers.ContainsKey(e.id)) Spawn(e, stack);
            }

            List<int> gone = new List<int>();
            foreach (KeyValuePair<int, GameObject> marker in _markers) if (!live.Contains(marker.Key)) gone.Add(marker.Key);
            foreach (int id in gone) { if (_markers[id] != null) Destroy(_markers[id]); _markers.Remove(id); }

            SyncFactories(state);
        }

        void Spawn(EventInstance e, int stack)
        {
            EventDefinition definition = database.Events[e.configIndex];
            if (definition.markerPrefab == null) return;

            Transform parent = Layer(definition.overlayLayerName);
            GameObject marker = Instantiate(definition.markerPrefab, parent);
            marker.name = "Marker_" + e.key + "_" + e.id;
            marker.transform.position = Place(e.nationIndex, stack);

            // Generic icons say what they are; the war zone keeps its own label.
            TextMesh label = marker.GetComponentInChildren<TextMesh>();
            if (label != null && definition.markerPrefab.name == "MapIcon") label.text = ShortName(definition.title);

            _markers[e.id] = marker;
        }

        Vector3 Place(int nationIndex, int stack)
        {
            Vector3 anchor = nationIndex >= 0 && nationIndex < nationMarkers.Length && nationMarkers[nationIndex] != null
                ? nationMarkers[nationIndex].position
                : Vector3.zero;
            return anchor + new Vector3(markerOffset.x, markerOffset.y - stack * stackSpacing, markerDepth);
        }

        void SyncFactories(EconomyState state)
        {
            if (factoryIconPrefab == null || fdiLayer == null) return;
            int wanted = Mathf.Clamp(Mathf.FloorToInt(state.currency.fdiInflowRate / Mathf.Max(1f, fdiPerIcon)), 0, maxFactoryIcons);

            while (_factories.Count < wanted)
            {
                GameObject icon = Instantiate(factoryIconPrefab, fdiLayer);
                icon.name = "FactoryIcon_" + _factories.Count;
                icon.transform.position = HomePosition() + new Vector3(-1.2f - _factories.Count * 0.55f, -0.2f, markerDepth);
                _factories.Add(icon);
            }
            while (_factories.Count > wanted)
            {
                Destroy(_factories[_factories.Count - 1]);
                _factories.RemoveAt(_factories.Count - 1);
            }
        }

        Vector3 HomePosition()
        {
            NationDefinition[] nations = database.Nations;
            for (int i = 0; i < nations.Length && i < nationMarkers.Length; i++)
                if (nations[i].isPlayerNation && nationMarkers[i] != null) return nationMarkers[i].position;
            return Vector3.zero;
        }

        Transform Layer(string name)
        {
            switch (name)
            {
                case "WarZoneLayer": return warZoneLayer;
                case "UnrestLayer": return unrestLayer;
                case "FDILayer": return fdiLayer;
                default: return disasterLayer;
            }
        }

        void ClearAll()
        {
            foreach (GameObject marker in _markers.Values) if (marker != null) Destroy(marker);
            _markers.Clear();
            foreach (GameObject icon in _factories) if (icon != null) Destroy(icon);
            _factories.Clear();
        }

        static string ShortName(string title)
        {
            string upper = title.ToUpperInvariant();
            return upper.Length <= 16 ? upper : upper.Substring(0, 15) + ".";
        }

        /// <summary>For tests and the debug menu: how many event markers are on the map.</summary>
        public int MarkerCount { get { return _markers.Count; } }
    }
}
