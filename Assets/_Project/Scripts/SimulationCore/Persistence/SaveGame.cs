namespace Sovereign.Core
{
    /// <summary>
    /// GDD 20 Phase 5: a whole run as one JSON document - the economy (which carries
    /// population, approval, events, war and geopolitics), the player's policy
    /// including everything queued for the quarter, and a version to refuse saves
    /// this build cannot read.
    /// </summary>
    public class SaveGame
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAt = "";
        public EconomyState economy;
        public PolicyState policy;

        public static string Write(EconomyState economy, PolicyState policy, string savedAt)
        {
            return StateWriter.ToJson(new SaveGame { economy = economy, policy = policy, savedAt = savedAt ?? "" });
        }

        /// <summary>
        /// Fills the live state and policy from a save, in place. The version is read
        /// first, so a save this build cannot read is refused before it touches anything.
        /// </summary>
        public static void Read(string json, EconomyState economy, PolicyState policy)
        {
            System.Collections.Generic.Dictionary<string, object> root =
                JsonParser.Parse(json) as System.Collections.Generic.Dictionary<string, object>;
            object version;
            if (root == null || !root.TryGetValue("version", out version) || !(version is JsonNumber))
                throw new System.FormatException("This is not a Sovereign save.");
            int found = int.Parse(((JsonNumber)version).text, System.Globalization.CultureInfo.InvariantCulture);
            if (found != CurrentVersion)
                throw new System.FormatException("This save is version " + found + "; this build reads version " + CurrentVersion + ".");

            StateReader.Populate(new SaveGame { economy = economy, policy = policy }, json);
        }
    }
}
