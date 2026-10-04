using System.Collections.Generic;
using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 3.3 and 20 Phase 5. Attached to each Route_* object. The curve comes from
    /// the route's own Waypoint_* children - drag one in the scene and the ships
    /// follow - and the ships are TradeShip prefab instances parented to the route.
    /// How many sail tracks how much you trade with that nation; a sanction in either
    /// direction empties the lane.
    /// </summary>
    public class TradeRouteView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] SimulationRunner runner;
        [Tooltip("The partner nation's position in GameDatabase.Nations.")]
        [SerializeField] int nationIndex;
        [SerializeField] GameObject shipPrefab;

        [Header("Traffic")]
        [Tooltip("Ships on the busiest lane at normal trade volume.")]
        [Range(1, 10)] [SerializeField] int maxShips = 5;
        [Tooltip("Fraction of the route a ship covers per real second at 1x.")]
        [Range(0.005f, 0.5f)] [SerializeField] float shipSpeed = 0.06f;
        [Tooltip("The trade share treated as 'busiest lane'. Lanes below it get proportionally fewer ships.")]
        [Range(0.05f, 1f)] [SerializeField] float busiestShare = 0.30f;

        readonly List<Transform> _waypoints = new List<Transform>();
        readonly List<Ship> _ships = new List<Ship>();

        /// <summary>A ship on the lane. Direction flips at each end, so a route reads as
        /// traffic going both ways rather than a one-way conveyor.</summary>
        class Ship { public Transform transform; public float t; public float direction = 1f; }

        void Awake()
        {
            // The route's own children, in name order - the authored curve.
            foreach (Transform child in transform)
                if (child.name.StartsWith("Waypoint_")) _waypoints.Add(child);
            _waypoints.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        void OnEnable() { if (runner != null) runner.OnWeekTick += Refresh; }
        void OnDisable() { if (runner != null) runner.OnWeekTick -= Refresh; }
        void Start() { if (runner != null && runner.State != null) Refresh(runner.State); }

        void Refresh(EconomyState state)
        {
            if (runner.Simulator == null || nationIndex < 0 || nationIndex >= state.geopolitics.nations.Count) return;
            NationProfile profile = runner.Simulator.Geopolitics.Config.nations[nationIndex];
            NationState nation = state.geopolitics.nations[nationIndex];

            bool blocked = nation.sanctioningYou || runner.Policy.NationFlag(PolicyState.NationSanction, profile.name);

            // Ships ARE the trade: this nation's share of it, what tariffs and deals have
            // done to that lane since, and the state of world trade overall.
            float volume = profile.tradeVolume / Mathf.Max(0.01f, busiestShare)
                           * nation.tradeIndex
                           * state.tradeVolumeIndex / 100f;
            int wanted = blocked ? 0 : Mathf.Clamp(Mathf.RoundToInt(maxShips * volume), 1, maxShips);
            SetShipCount(wanted);
        }

        void SetShipCount(int wanted)
        {
            while (_ships.Count < wanted && shipPrefab != null)
            {
                GameObject ship = Instantiate(shipPrefab, transform);
                ship.name = "TradeShip_" + _ships.Count;
                // Spread new ships along the lane rather than stacking them at port.
                // Spread along the lane, and half of them heading home.
                _ships.Add(new Ship
                {
                    transform = ship.transform,
                    t = (float)_ships.Count / Mathf.Max(1, wanted),
                    direction = _ships.Count % 2 == 0 ? 1f : -1f
                });
            }
            while (_ships.Count > wanted)
            {
                Destroy(_ships[_ships.Count - 1].transform.gameObject);
                _ships.RemoveAt(_ships.Count - 1);
            }
        }

        void Update()
        {
            if (_waypoints.Count < 2 || runner == null) return;
            float speed = runner.IsRunning ? shipSpeed * (int)runner.Speed : 0f;

            foreach (Ship ship in _ships)
            {
                // Sail to the far end, then turn around and bring something back.
                ship.t += speed * ship.direction * Time.deltaTime;
                if (ship.t >= 1f) { ship.t = 1f; ship.direction = -1f; }
                else if (ship.t <= 0f) { ship.t = 0f; ship.direction = 1f; }

                Vector3 position = Evaluate(ship.t);
                Vector3 ahead = (Evaluate(Mathf.Clamp01(ship.t + 0.01f * ship.direction)) - position) * ship.direction;
                ship.transform.position = position + new Vector3(0f, 0f, -0.15f);
                if (ahead.sqrMagnitude > 1e-6f)
                    ship.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(ahead.y, ahead.x) * Mathf.Rad2Deg);
            }
        }

        /// <summary>Catmull-Rom through the waypoints: the curve passes through every
        /// point the designer placed, which a bezier would not.</summary>
        Vector3 Evaluate(float t)
        {
            int segments = _waypoints.Count - 1;
            float scaled = Mathf.Clamp01(t) * segments;
            int i = Mathf.Min(Mathf.FloorToInt(scaled), segments - 1);
            float u = scaled - i;

            Vector3 p0 = _waypoints[Mathf.Max(0, i - 1)].position;
            Vector3 p1 = _waypoints[i].position;
            Vector3 p2 = _waypoints[i + 1].position;
            Vector3 p3 = _waypoints[Mathf.Min(segments, i + 2)].position;

            return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u);
        }

        public int ShipCount { get { return _ships.Count; } }

        /// <summary>How many are outbound right now - the rest are on their way home.</summary>
        public int OutboundShips
        {
            get
            {
                int outbound = 0;
                foreach (Ship ship in _ships) if (ship.direction > 0f) outbound++;
                return outbound;
            }
        }
    }
}
