using System;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 4 and 20 Phase 1. Attached to the SimulationRunner GameObject that has
    /// existed since Phase 0. This is the clock: it owns the simulation objects,
    /// advances them, and raises the events every other system listens to.
    ///
    /// The economics live in SimulationCore. Nothing here computes anything - if a
    /// number is being decided in this file, it is in the wrong file.
    /// </summary>
    public class SimulationRunner : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("The one object that holds every ScriptableObject. Drag GameDatabase here.")]
        [SerializeField] GameDatabase database;

        [Header("Opening position")]
        [Tooltip("The scenario asset this run starts from. Empty falls back to the values below.")]
        [SerializeField] ScenarioDefinition scenario;
        [SerializeField] StartingConditions startingConditions = new StartingConditions();

        /// <summary>The opening this run boots from - the scenario asset if there is one.</summary>
        public StartingConditions Opening { get { return scenario != null ? scenario.conditions : startingConditions; } }

        /// <summary>Swapped by the tests, which measure the model against a calm opening.</summary>
        public ScenarioDefinition Scenario { get { return scenario; } set { scenario = value; } }

        [Header("Clock - GDD 4")]
        [Tooltip("Real seconds per in-game week at 1x. The GDD asks for one.")]
        [Range(0.05f, 10f)] [SerializeField] float secondsPerWeek = 1f;

        [Tooltip("Speed at boot. Starts at 1x so pressing Play does something; the pause button is right there.")]
        [SerializeField] GameSpeed speed = GameSpeed.Normal;

        [Tooltip("Run the clock automatically. Off means something else calls Step() - the headless tests do.")]
        [SerializeField] bool advanceAutomatically = true;

        public event Action<EconomyState> OnWeekTick;
        public event Action<EconomyState> OnQuarterTick;
        public event Action<EconomyState> OnYearTick;
        public event Action<EconomyState> OnBondAuction;
        public event Action<EconomyState> OnCreditRatingReview;

        /// <summary>GDD 4 and 14. Revolt ends the run: OnRevolt says why, OnGameOver
        /// says it is over, and the clock stops.</summary>
        public event Action<EconomyState> OnRevolt;
        public event Action<EconomyState> OnGameOver;
        /// <summary>The other ending: the scenario's victory conditions have held.</summary>
        public event Action<EconomyState> OnVictory;
        /// <summary>An achievement API name, the week it is earned.</summary>
        public event Action<string> OnAchievementUnlocked;

        bool _gameOverRaised;
        bool _victoryRaised;

        EconomySimulator _simulator;
        EconomyState _state;
        PolicyState _policy;
        float _accumulator;
        bool _booted;

        /// <summary>The assets this run was built from - views read display names from it.</summary>
        public GameDatabase Database { get { return database; } }

        public EconomyState State { get { return _state; } }
        public PolicyState Policy { get { return _policy; } }
        public EconomySimulator Simulator { get { return _simulator; } }
        public bool IsRunning { get { return speed != GameSpeed.Paused; } }

        public GameSpeed Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        void Awake()
        {
            Boot();
        }

        /// <summary>Builds the simulation from the assets. Safe to call again for a new run.</summary>
        public void Boot()
        {
            if (database == null)
            {
                Debug.LogError("SimulationRunner: no GameDatabase assigned. Drag it onto this component.", this);
                enabled = false;
                return;
            }

            StartingConditions opening = Opening;
            SimulationConfig config = SimulationConfigBuilder.Build(database, opening);
            _simulator = new EconomySimulator(config);
            _state = _simulator.CreateState();
            _policy = PolicyBootstrap.Create(database, opening);
            _simulator.Prepare(_state, _policy);
            _accumulator = 0f;
            _gameOverRaised = false;
            _victoryRaised = false;
            _booted = true;
        }

        void Update()
        {
            if (!_booted || !advanceAutomatically || speed == GameSpeed.Paused) return;

            _accumulator += Time.deltaTime * (int)speed;
            float interval = Mathf.Max(0.01f, secondsPerWeek);

            // Cap the catch-up so a stalled frame cannot fast-forward a year.
            int guard = 0;
            while (_accumulator >= interval && guard++ < 8)
            {
                _accumulator -= interval;
                Step();
            }
        }

        /// <summary>
        /// One week, plus whatever boundaries that week crosses. Quarter work happens
        /// AFTER the week's tick, so a policy change committed at the boundary shapes
        /// the next quarter rather than the one just finished.
        /// </summary>
        public void Step()
        {
            if (!_booted || _state.IsGameOver) return;

            int earned = _state.achievements.unlocked.Count;
            _simulator.Tick(_state, _policy);
            if (OnWeekTick != null) OnWeekTick(_state);

            if (_state.IsQuarterBoundary)
            {
                _simulator.TickQuarter(_state, _policy);
                if (OnQuarterTick != null) OnQuarterTick(_state);
                if (OnBondAuction != null) OnBondAuction(_state);
            }

            if (_state.IsYearBoundary)
            {
                _simulator.TickYear(_state, _policy);
                if (OnYearTick != null) OnYearTick(_state);
                if (OnCreditRatingReview != null) OnCreditRatingReview(_state);
            }

            for (int i = earned; i < _state.achievements.unlocked.Count; i++)
                if (OnAchievementUnlocked != null) OnAchievementUnlocked(_state.achievements.unlocked[i]);

            if (_state.prosperity && !_victoryRaised)
            {
                _victoryRaised = true;
                if (OnVictory != null) OnVictory(_state);
            }

            if (_state.IsGameOver && !_gameOverRaised)
            {
                _gameOverRaised = true;
                speed = GameSpeed.Paused;
                if (OnRevolt != null) OnRevolt(_state);
                if (OnGameOver != null) OnGameOver(_state);
            }
        }

        /// <summary>Runs a number of weeks immediately. Used by the headless tests and
        /// by anything that needs to fast-forward.</summary>
        public void Step(int weeks)
        {
            for (int i = 0; i < weeks; i++) Step();
        }

        /// <summary>
        /// Ends the run now, exactly as a revolt would. The Inspector menu item exists so
        /// the game-over screen can be checked without spending ten simulated years
        /// earning one.
        /// </summary>
        [ContextMenu("Debug: End Run With A Revolt")]
        public void DebugEndRun() { EndRun("Debug: ended from the Inspector."); }

        public void EndRun(string reason)
        {
            if (!_booted || _gameOverRaised) return;
            _state.approval.revolt = true;
            _state.approval.revoltReason = reason;
            _gameOverRaised = true;
            speed = GameSpeed.Paused;
            if (OnRevolt != null) OnRevolt(_state);
            if (OnGameOver != null) OnGameOver(_state);
        }

        /// <summary>The whole run as JSON - economy, policy, queue, events, dice.</summary>
        public string SaveToJson()
        {
            return _booted ? SaveGame.Write(_state, _policy, System.DateTime.UtcNow.ToString("o")) : null;
        }

        /// <summary>
        /// Rebuilds the simulation from the assets, then fills it from the save. Opens
        /// paused, and tells every view to redraw from the loaded state.
        /// </summary>
        public void LoadFromJson(string json)
        {
            string current = _booted ? SaveToJson() : null;
            Boot();
            if (!_booted) return;
            try { SaveGame.Read(json, _state, _policy); }
            catch
            {
                // A bad file must not cost the player the game they were playing.
                Boot();
                if (current != null) SaveGame.Read(current, _state, _policy);
                throw;
            }
            _gameOverRaised = _state.IsGameOver;
            speed = GameSpeed.Paused;
            if (OnWeekTick != null) OnWeekTick(_state);
        }

        /// <summary>GDD 12. Once a year, instant, and the market notices you panicked.</summary>
        public bool RequestEmergencyRateCut()
        {
            return _booted && _simulator.TryEmergencyRateCut(_state, _policy);
        }
    }
}
