using System;
using System.IO;
using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 3.3 and 20 Phase 5. On the SaveLoadManager GameObject from Phase 0. Saves
    /// to the player's persistent data folder - which is also where Steam Cloud picks
    /// files up, once auto-cloud is pointed at it in the Steamworks settings - and
    /// autosaves once a year of game time.
    /// </summary>
    public class SaveLoadManager : MonoBehaviour
    {
        [SerializeField] SimulationRunner runner;
        [SerializeField] string saveFileName = "sovereign_save.json";
        [SerializeField] string autosaveFileName = "sovereign_autosave.json";
        [Tooltip("Write the autosave every in-game year.")]
        [SerializeField] bool autosaveYearly = true;

        /// <summary>Tests point this at a throwaway file so they never touch a real save.</summary>
        public string SaveFileName { get { return saveFileName; } set { saveFileName = value; } }

        public string SavePath { get { return Path.Combine(Application.persistentDataPath, saveFileName); } }
        public string AutosavePath { get { return Path.Combine(Application.persistentDataPath, autosaveFileName); } }
        public bool HasSave { get { return File.Exists(SavePath); } }

        void OnEnable() { if (runner != null) runner.OnYearTick += Autosave; }
        void OnDisable() { if (runner != null) runner.OnYearTick -= Autosave; }

        [ContextMenu("Save Now")]
        public bool Save() { return Write(SavePath, "Game saved"); }

        [ContextMenu("Load Save")]
        public bool Load()
        {
            string path = HasSave ? SavePath : AutosavePath;
            if (!File.Exists(path)) { Notify("No save to load"); return false; }
            try
            {
                runner.LoadFromJson(File.ReadAllText(path));
                Notify("Loaded " + (path == SavePath ? "save" : "autosave") + " - week " + runner.State.week + ", paused");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Sovereign: could not load " + path + ": " + e.Message, this);
                Notify("Load failed: " + e.Message);
                return false;
            }
        }

        void Autosave(EconomyState state)
        {
            if (autosaveYearly && !state.IsGameOver) Write(AutosavePath, null);
        }

        bool Write(string path, string message)
        {
            if (runner == null || runner.State == null) return false;
            try
            {
                string json = runner.SaveToJson();
                string temp = path + ".tmp";
                File.WriteAllText(temp, json);
                // Write then swap, so a crash mid-save never leaves a half-written file.
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                if (message != null) Notify(message + " (" + (json.Length / 1024) + " KB)");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Sovereign: could not save to " + path + ": " + e.Message, this);
                Notify("Save failed: " + e.Message);
                return false;
            }
        }

        void Notify(string text)
        {
            if (runner != null && runner.State != null)
                runner.State.events.Post(runner.State.week, AlertLevel.Info, AlertChannel.Ticker, text);
        }
    }
}
