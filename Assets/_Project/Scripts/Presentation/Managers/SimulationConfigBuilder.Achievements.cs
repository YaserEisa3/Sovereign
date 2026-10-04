using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    public static partial class SimulationConfigBuilder
    {
        static AchievementConfig BuildAchievements(AchievementParameters p)
        {
            AchievementConfig c = new AchievementConfig();
            if (p == null) return c;
            foreach (AchievementParameters.Entry e in p.entries) c.titles[e.apiName] = e.title;
            c.softLandingFromInflation = p.softLandingFromInflation;
            c.softLandingToInflation = p.softLandingToInflation;
            c.softLandingMaxUnemployment = p.softLandingMaxUnemployment;
            c.debtHawkReductionPoints = p.debtHawkReductionPoints;
            c.debtHawkWindowWeeks = p.debtHawkWindowWeeks;
            c.volckerRate = p.volckerRate;
            c.volckerInflation = p.volckerInflation;
            c.volckerTamedInflation = p.volckerTamedInflation;
            c.crisisManagerCount = p.crisisManagerCount;
            c.crisisManagerMitigation = p.crisisManagerMitigation;
            c.reserveCurrencyBand = p.reserveCurrencyBand;
            c.reserveCurrencyWeeks = p.reserveCurrencyWeeks;
            c.tradeWarTariff = p.tradeWarTariff;
            c.peoplesChampionApproval = p.peoplesChampionApproval;
            c.peoplesChampionWeeks = p.peoplesChampionWeeks;
            return c;
        }
    }
}
