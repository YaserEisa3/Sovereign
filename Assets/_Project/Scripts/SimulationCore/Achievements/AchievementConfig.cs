using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>GDD 20 Phase 5. The ids are also the Steam API names, so they never change.</summary>
    public static class AchievementIds
    {
        public const string SoftLanding = "SOFT_LANDING";
        public const string DebtHawk = "DEBT_HAWK";
        public const string VolckerMoment = "VOLCKER_MOMENT";
        public const string CrisisManager = "CRISIS_MANAGER";
        public const string ReserveCurrencyDefender = "RESERVE_CURRENCY_DEFENDER";
        public const string TradeWarVeteran = "TRADE_WAR_VETERAN";
        public const string PeoplesChampion = "PEOPLES_CHAMPION";
        public const string Unanchored = "UNANCHORED";
    }

    /// <summary>Thresholds and display text, copied from SO_AchievementParameters.</summary>
    public class AchievementConfig
    {
        public readonly Dictionary<string, string> titles = new Dictionary<string, string>();

        public float softLandingFromInflation, softLandingToInflation, softLandingMaxUnemployment;
        public float debtHawkReductionPoints;
        public int debtHawkWindowWeeks;
        public float volckerRate, volckerInflation, volckerTamedInflation;
        public int crisisManagerCount;
        public float crisisManagerMitigation;
        public float reserveCurrencyBand;
        public int reserveCurrencyWeeks;
        public float tradeWarTariff;
        public float peoplesChampionApproval;
        public int peoplesChampionWeeks;

        public string Title(string id)
        {
            string title;
            return titles.TryGetValue(id, out title) ? title : id;
        }
    }

    /// <summary>What has been earned, and the half-finished feats in progress. Saved.</summary>
    public class AchievementState
    {
        public readonly List<string> unlocked = new List<string>();
        public readonly List<int> unlockedWeek = new List<int>();

        public bool softLandingArmed;
        public float softLandingPeakUnemployment;
        public bool volckerArmed;
        public int weeksCurrencyStable;
        public int weeksPopular;
        public readonly List<bool> tradeWarWith = new List<bool>();

        public bool Has(string id) { return unlocked.Contains(id); }
    }
}
