using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 20 Phase 5. On the SteamManager GameObject from Phase 0. Passes earned
    /// achievements to Steam and keeps rich presence current - "Year 2031 - Rating:
    /// AA - Approval: 67%". Without SOVEREIGN_STEAM it runs the offline backend, so
    /// everything upstream is exercised in every build.
    /// </summary>
    public class SteamManager : MonoBehaviour
    {
        [SerializeField] SimulationRunner runner;
        [Tooltip("Your Steam App ID. 480 is Valve's public test app, Spacewar.")]
        [SerializeField] uint appId = 480;
        [Tooltip("Rich presence text. {0} year, {1} credit rating, {2} approval percent.")]
        [SerializeField] string presenceFormat = "Year {0} \u2014 Rating: {1} \u2014 Approval: {2}%";
        [Tooltip("Weeks between rich presence updates - Steam rate-limits them.")]
        [SerializeField] int presenceIntervalWeeks = 4;
        [SerializeField] bool logOfflineCalls = true;

        ISteamBackend _backend;
        bool _running;
        int _synced = -1;

        public ISteamBackend Backend { get { return _backend; } }
        public string LastPresence { get; private set; }

        void Awake()
        {
#if SOVEREIGN_STEAM
            _backend = new FacepunchSteamBackend();
#else
            _backend = new LoggingSteamBackend(logOfflineCalls);
#endif
            _running = _backend.Initialise(appId);
            if (!_running)
            {
                _backend = new LoggingSteamBackend(logOfflineCalls);
                _running = _backend.Initialise(appId);
            }
        }

        void OnEnable()
        {
            if (runner == null) return;
            runner.OnAchievementUnlocked += Unlock;
            runner.OnWeekTick += WeekTick;
        }

        void OnDisable()
        {
            if (runner == null) return;
            runner.OnAchievementUnlocked -= Unlock;
            runner.OnWeekTick -= WeekTick;
        }

        void Update() { if (_running) _backend.RunCallbacks(); }

        void OnApplicationQuit() { if (_running) _backend.Shutdown(); _running = false; }

        void Unlock(string apiName) { if (_running) _backend.UnlockAchievement(apiName); }

        void WeekTick(EconomyState state)
        {
            if (!_running) return;
            // A loaded save may hold achievements this Steam account has not seen yet.
            if (state.achievements.unlocked.Count != _synced)
            {
                foreach (string id in state.achievements.unlocked) _backend.UnlockAchievement(id);
                _synced = state.achievements.unlocked.Count;
            }
            if (presenceIntervalWeeks > 0 && state.week % presenceIntervalWeeks != 0 && LastPresence != null) return;

            LastPresence = string.Format(presenceFormat, state.Year, state.bonds.creditRating,
                                         Mathf.RoundToInt(state.approval.overall));
            _backend.SetRichPresence("steam_display", LastPresence);
            _backend.SetRichPresence("status", LastPresence);
        }
    }
}
