using UnityEngine;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 20 Phase 5. What it takes to earn each achievement, and what it is called.
    /// The API names are fixed in code because Steam keys on them; everything a
    /// designer might want to tune is here.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_AchievementParameters", menuName = "Sovereign/Parameters/Achievement Parameters")]
    public class AchievementParameters : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("Steam API name - must match AchievementIds and the Steamworks dashboard.")]
            public string apiName;
            public string title;
            [TextArea] public string description;
        }

        public Entry[] entries = new Entry[0];

        [Header("Soft Landing")]
        [Tooltip("Inflation (%) that counts as a problem to land from.")]
        public float softLandingFromInflation = 5f;
        [Tooltip("Inflation (%) that counts as landed.")]
        public float softLandingToInflation = 2.5f;
        [Tooltip("Unemployment (%) never exceeded on the way down.")]
        public float softLandingMaxUnemployment = 6.5f;

        [Header("Debt Hawk")]
        [Tooltip("Points of debt/GDP cut from the recent peak.")]
        public float debtHawkReductionPoints = 20f;
        [Tooltip("Weeks the peak is looked back over - one four-year term.")]
        public int debtHawkWindowWeeks = 208;

        [Header("Volcker Moment")]
        public float volckerRate = 10f;
        public float volckerInflation = 8f;
        public float volckerTamedInflation = 3f;

        [Header("Crisis Manager")]
        public int crisisManagerCount = 5;
        [Range(0f, 1f)] public float crisisManagerMitigation = 0.4f;

        [Header("Reserve Currency Defender")]
        [Tooltip("Index points either side of 100.")]
        public float reserveCurrencyBand = 10f;
        public int reserveCurrencyWeeks = 520;

        [Header("Trade War Veteran")]
        [Tooltip("A tariff on you (%) this high counts as a trade war.")]
        public float tradeWarTariff = 20f;

        [Header("The People's Champion")]
        public float peoplesChampionApproval = 70f;
        public int peoplesChampionWeeks = 104;
    }
}
