using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 20 Phase 5: each achievement fires for the feat, and not for a near miss.</summary>
    public static partial class EconomyTest
    {
        static void TestAchievements(SimulationRunner runner)
        {
            AchievementModel model;
            EconomyState s;

            s = FreshAchievements(runner, out model);
            AchievementConfig c = model.Config;
            s.inflation = c.softLandingFromInflation + 1f; s.unemployment = c.softLandingMaxUnemployment - 1f; model.TickWeek(s);
            s.inflation = c.softLandingToInflation - 0.5f; model.TickWeek(s);
            Check(s.achievements.Has(AchievementIds.SoftLanding), "achievements: a clean disinflation did not earn Soft Landing");

            s = FreshAchievements(runner, out model);
            s.inflation = c.softLandingFromInflation + 1f; s.unemployment = c.softLandingMaxUnemployment + 2f; model.TickWeek(s);
            s.inflation = c.softLandingToInflation - 0.5f; model.TickWeek(s);
            Check(!s.achievements.Has(AchievementIds.SoftLanding), "achievements: disinflation through mass unemployment earned Soft Landing");

            s = FreshAchievements(runner, out model);
            s.approval.overall = c.peoplesChampionApproval + 5f;
            for (int i = 1; i < c.peoplesChampionWeeks; i++) model.TickWeek(s);
            bool early = s.achievements.Has(AchievementIds.PeoplesChampion);
            model.TickWeek(s);
            Check(!early && s.achievements.Has(AchievementIds.PeoplesChampion), "achievements: The People's Champion did not unlock after exactly its weeks");

            s = FreshAchievements(runner, out model);
            s.geopolitics.nations[0].theirTariffOnYou = c.tradeWarTariff + 5f; model.TickWeek(s);
            s.geopolitics.nations[0].theirTariffOnYou = 0f; model.TickWeek(s);
            Check(s.achievements.Has(AchievementIds.TradeWarVeteran), "achievements: a lifted trade war did not earn Trade War Veteran");

            s = FreshAchievements(runner, out model);
            s.monetary.centralBankRate = c.volckerRate; s.inflation = c.volckerInflation + 1f; model.TickWeek(s);
            s.inflation = c.volckerTamedInflation - 1f; model.TickWeek(s);
            Check(s.achievements.Has(AchievementIds.VolckerMoment), "achievements: taming inflation at 10% rates did not earn Volcker Moment");

            s = FreshAchievements(runner, out model);
            s.monetary.expectationsUnanchored = true; model.TickWeek(s);
            Check(s.achievements.Has(AchievementIds.Unanchored), "achievements: unanchored expectations did not earn Unanchored");
            int alerts = 0;
            foreach (Alert a in s.events.alerts) if (a.text.Contains("ACHIEVEMENT")) alerts++;
            model.TickWeek(s);
            int after = 0;
            foreach (Alert a in s.events.alerts) if (a.text.Contains("ACHIEVEMENT")) after++;
            Check(alerts == 1 && after == 1, "achievements: an unlock was announced " + alerts + " then " + after + " times, not once");

            // An ordinary decade earns nothing it should not, and what is earned survives a save.
            Fresh(runner);
            runner.Step(10 * Year);
            Check(!runner.State.achievements.Has(AchievementIds.Unanchored), "achievements: a steady decade unanchored expectations");
            runner.State.achievements.unlocked.Add(AchievementIds.DebtHawk);
            runner.State.achievements.unlockedWeek.Add(runner.State.week);
            runner.LoadFromJson(runner.SaveToJson());
            Check(runner.State.achievements.Has(AchievementIds.DebtHawk), "achievements: an unlocked achievement did not survive save and load");
        }

        static EconomyState FreshAchievements(SimulationRunner runner, out AchievementModel model)
        {
            Fresh(runner);
            model = runner.Simulator.Achievements;
            return runner.State;
        }
    }
}
