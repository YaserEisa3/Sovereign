using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 13 and 15. Your choices toward each nation - tariff, trade agreement,
    /// sanctions, swap line, share of foreign aid - keyed by nation name. Like every
    /// other control they queue to the quarter boundary.
    /// </summary>
    public partial class PolicyState
    {
        public const string NationTariff = "tariff";
        public const string NationAgreement = "agreement";
        public const string NationSanction = "sanction";
        public const string NationSwapLine = "swap";
        public const string NationAidShare = "aid";

        readonly Dictionary<string, float> _nations = new Dictionary<string, float>();
        readonly Dictionary<string, float> _pendingNations = new Dictionary<string, float>();

        static string NationKey(string field, string nation) { return field + "|" + nation; }

        public float Nation(string field, string nation)
        {
            float value;
            return _nations.TryGetValue(NationKey(field, nation), out value) ? value : 0f;
        }

        public bool NationFlag(string field, string nation) { return Nation(field, nation) >= 0.5f; }

        public float PendingNation(string field, string nation)
        {
            float value;
            return _pendingNations.TryGetValue(NationKey(field, nation), out value) ? value : Nation(field, nation);
        }

        public bool HasPendingNation(string field, string nation) { return _pendingNations.ContainsKey(NationKey(field, nation)); }

        public void SetNation(string field, string nation, float value) { _nations[NationKey(field, nation)] = value; }

        public void QueueNation(string field, string nation, float value)
        {
            string key = NationKey(field, nation);
            if (value == Nation(field, nation)) _pendingNations.Remove(key); else _pendingNations[key] = value;
        }

        void CommitNations()
        {
            foreach (KeyValuePair<string, float> entry in _pendingNations) _nations[entry.Key] = entry.Value;
            _pendingNations.Clear();
        }

        int PendingNationCount { get { return _pendingNations.Count; } }
    }
}
