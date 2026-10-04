#if SOVEREIGN_STEAM
using UnityEngine;

namespace Sovereign.Presentation
{
    /// <summary>
    /// The live backend, on Facepunch.Steamworks. To switch it on: add the
    /// Facepunch.Steamworks package, put your steam_appid.txt next to the build, and
    /// add SOVEREIGN_STEAM to Project Settings - Player - Scripting Define Symbols.
    /// </summary>
    public class FacepunchSteamBackend : ISteamBackend
    {
        public string Name { get { return "Steamworks"; } }

        public bool Initialise(uint appId)
        {
            try
            {
                Steamworks.SteamClient.Init(appId, false);
                return Steamworks.SteamClient.IsValid;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Sovereign: Steam did not start (" + e.Message + ") - playing offline.");
                return false;
            }
        }

        public void UnlockAchievement(string apiName)
        {
            Steamworks.Data.Achievement achievement = new Steamworks.Data.Achievement(apiName);
            if (!achievement.State) achievement.Trigger(true);
        }

        public void SetRichPresence(string key, string value)
        {
            Steamworks.SteamFriends.SetRichPresence(key, value);
        }

        public void RunCallbacks() { Steamworks.SteamClient.RunCallbacks(); }
        public void Shutdown() { Steamworks.SteamClient.Shutdown(); }
    }
}
#endif
