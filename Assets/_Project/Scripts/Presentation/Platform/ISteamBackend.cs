using System.Collections.Generic;
using UnityEngine;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 3.3 and 20 Phase 5. Everything the game asks of Steam, behind an interface,
    /// so the game builds and runs without the Steamworks SDK. The real backend is
    /// compiled in only when SOVEREIGN_STEAM is defined.
    /// </summary>
    public interface ISteamBackend
    {
        string Name { get; }
        bool Initialise(uint appId);
        void UnlockAchievement(string apiName);
        void SetRichPresence(string key, string value);
        void RunCallbacks();
        void Shutdown();
    }

    /// <summary>The default: no Steam, just a record of what would have been sent.</summary>
    public class LoggingSteamBackend : ISteamBackend
    {
        public readonly List<string> unlocked = new List<string>();
        public readonly Dictionary<string, string> presence = new Dictionary<string, string>();
        readonly bool _log;

        public LoggingSteamBackend(bool log) { _log = log; }

        public string Name { get { return "Offline (logging)"; } }
        public bool Initialise(uint appId) { return true; }

        public void UnlockAchievement(string apiName)
        {
            if (unlocked.Contains(apiName)) return;
            unlocked.Add(apiName);
            if (_log) Debug.Log("Sovereign Steam (offline): achievement " + apiName);
        }

        public void SetRichPresence(string key, string value)
        {
            presence[key] = value;
        }

        public void RunCallbacks() { }
        public void Shutdown() { }
    }
}
