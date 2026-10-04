using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    public static partial class ProjectBuilder
    {
        static AchievementParameters BuildAchievementParameters()
        {
            return Asset<AchievementParameters>(ParamPath + "SO_AchievementParameters.asset", a =>
            {
                a.entries = new[]
                {
                    Entry(AchievementIds.SoftLanding, "Soft Landing", "Bring inflation above 5% back to target without unemployment ever passing 6.5%."),
                    Entry(AchievementIds.DebtHawk, "Debt Hawk", "Cut debt-to-GDP by 20 points within a single four-year term."),
                    Entry(AchievementIds.VolckerMoment, "Volcker Moment", "Raise rates to 10% with inflation above 8%, tame it below 3%, and keep your government."),
                    Entry(AchievementIds.CrisisManager, "Crisis Manager", "Meet five crises with at least 40% of the damage prepared for."),
                    Entry(AchievementIds.ReserveCurrencyDefender, "Reserve Currency Defender", "Keep the currency within 10% of par for ten straight years."),
                    Entry(AchievementIds.TradeWarVeteran, "Trade War Veteran", "Get a nation to lift tariffs of 20% or more it placed on you."),
                    Entry(AchievementIds.PeoplesChampion, "The People's Champion", "Hold overall approval above 70% for two straight years."),
                    Entry(AchievementIds.Unanchored, "Unanchored", "Let inflation expectations come loose. Everyone remembers where they were."),
                };
            });
        }

        static AchievementParameters.Entry Entry(string api, string title, string description)
        {
            return new AchievementParameters.Entry { apiName = api, title = title, description = description };
        }
    }
}
