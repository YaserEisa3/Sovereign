namespace Sovereign.Core
{
    /// <summary>
    /// GDD 20 Phase 5. Watches the finished week and unlocks feats. It only reads
    /// state, so achievements can never change the economy they reward.
    /// </summary>
    public class AchievementModel
    {
        readonly AchievementConfig _c;

        public AchievementModel(AchievementConfig config) { _c = config ?? new AchievementConfig(); }

        public AchievementConfig Config { get { return _c; } }

        public void TickWeek(EconomyState state)
        {
            AchievementState a = state.achievements;

            // Soft Landing: bring high inflation back down without a jobs crisis.
            if (state.inflation >= _c.softLandingFromInflation && !a.softLandingArmed)
            {
                a.softLandingArmed = true;
                a.softLandingPeakUnemployment = state.unemployment;
            }
            if (a.softLandingArmed)
            {
                a.softLandingPeakUnemployment = MathUtil.Max(a.softLandingPeakUnemployment, state.unemployment);
                if (state.inflation <= _c.softLandingToInflation)
                {
                    if (a.softLandingPeakUnemployment <= _c.softLandingMaxUnemployment) Unlock(state, AchievementIds.SoftLanding);
                    a.softLandingArmed = false;
                }
            }

            // Debt Hawk: take a big bite out of debt/GDP within one term.
            TimeSeries debt = state.Series("debtToGdp");
            int window = System.Math.Min(_c.debtHawkWindowWeeks, debt.Count);
            float peak = 0f;
            for (int i = 0; i < window; i++) peak = MathUtil.Max(peak, debt.Ago(i));
            if (window > 0 && peak - state.DebtToGdp * 100f >= _c.debtHawkReductionPoints) Unlock(state, AchievementIds.DebtHawk);

            // Volcker Moment: crush runaway inflation with punishing rates and survive it.
            if (state.monetary.centralBankRate >= _c.volckerRate && state.inflation >= _c.volckerInflation) a.volckerArmed = true;
            if (a.volckerArmed && state.inflation <= _c.volckerTamedInflation && !state.IsGameOver) Unlock(state, AchievementIds.VolckerMoment);

            // Crisis Manager: meet enough crises prepared.
            int handled = 0;
            foreach (EventRecord r in state.events.history) if (r.mitigation >= _c.crisisManagerMitigation) handled++;
            if (handled >= _c.crisisManagerCount) Unlock(state, AchievementIds.CrisisManager);

            // Reserve Currency Defender: hold the currency near par for years.
            bool stable = System.Math.Abs(state.currency.exchangeRateIndex - 100f) <= _c.reserveCurrencyBand;
            a.weeksCurrencyStable = stable ? a.weeksCurrencyStable + 1 : 0;
            if (a.weeksCurrencyStable >= _c.reserveCurrencyWeeks) Unlock(state, AchievementIds.ReserveCurrencyDefender);

            // Trade War Veteran: someone slapped real tariffs on you, and you got them lifted.
            var nations = state.geopolitics.nations;
            while (a.tradeWarWith.Count < nations.Count) a.tradeWarWith.Add(false);
            for (int i = 0; i < nations.Count; i++)
            {
                if (nations[i].theirTariffOnYou >= _c.tradeWarTariff) a.tradeWarWith[i] = true;
                else if (a.tradeWarWith[i] && nations[i].theirTariffOnYou <= 0f) Unlock(state, AchievementIds.TradeWarVeteran);
            }

            // The People's Champion: stay genuinely popular for years.
            a.weeksPopular = state.approval.overall >= _c.peoplesChampionApproval ? a.weeksPopular + 1 : 0;
            if (a.weeksPopular >= _c.peoplesChampionWeeks) Unlock(state, AchievementIds.PeoplesChampion);

            // Unanchored: a badge of shame.
            if (state.monetary.expectationsUnanchored) Unlock(state, AchievementIds.Unanchored);
        }

        void Unlock(EconomyState state, string id)
        {
            AchievementState a = state.achievements;
            if (a.Has(id)) return;
            a.unlocked.Add(id);
            a.unlockedWeek.Add(state.week);
            state.events.Post(state.week, AlertLevel.Info, AlertChannel.Ticker, "ACHIEVEMENT: " + _c.Title(id));
        }
    }
}
