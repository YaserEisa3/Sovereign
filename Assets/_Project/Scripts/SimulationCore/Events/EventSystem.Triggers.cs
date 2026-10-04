namespace Sovereign.Core
{
    /// <summary>Whether an event can fire, and starting it.</summary>
    public partial class EventSystem
    {
        bool CanFire(EconomyState state, int index)
        {
            EventConfig cfg = _c.events[index];
            if (state.week / 52 < cfg.earliestYear) return false;
            if (cfg.uniqueWhileActive)
                foreach (EventInstance e in state.events.live) if (e.configIndex == index) return false;
            return ConditionsHold(state, cfg);
        }

        public bool ConditionsHold(EconomyState state, EventConfig cfg)
        {
            if (cfg.conditions == null) return true;
            foreach (TriggerCondition condition in cfg.conditions)
            {
                float value = Measure(state, condition.metric);
                bool holds;
                switch (condition.comparison)
                {
                    case Comparison.Above: holds = value > condition.threshold; break;
                    case Comparison.Below: holds = value < condition.threshold; break;
                    case Comparison.AtLeast: holds = value >= condition.threshold; break;
                    default: holds = value <= condition.threshold; break;
                }
                if (!holds) return false;
            }
            return true;
        }

        static float Measure(EconomyState s, ConditionMetric metric)
        {
            switch (metric)
            {
                case ConditionMetric.DebtToGdp: return s.DebtToGdp;
                case ConditionMetric.Inflation: return s.inflation;
                case ConditionMetric.Unemployment: return s.unemployment;
                case ConditionMetric.RealInterestRate: return s.RealInterestRate;
                case ConditionMetric.RealRateThreeYearAverage: return s.Series("realRate").Average(156);
                case ConditionMetric.CreditRating: return (int)s.bonds.creditRating;
                case ConditionMetric.ForeignHolding: return s.bonds.foreignHoldingPercent;
                case ConditionMetric.BankCapitalRequirement: return s.bankCapitalRequirement;
                case ConditionMetric.ExpectationsUnanchored: return s.monetary.expectationsUnanchored ? 1f : 0f;
                case ConditionMetric.FxReserves: return s.currency.fxReservesBillions + s.geopolitics.swapBackstop;
                case ConditionMetric.CurrencyIndex: return s.currency.exchangeRateIndex;
                case ConditionMetric.ApprovalOverall: return s.approval.overall;
                case ConditionMetric.Year: return s.week / 52f;
                default: return 0f;
            }
        }

        /// <summary>Starts an event - rolled, escalated into, or spawned by hand for a
        /// test or the debug menu. Returns null if no such event exists.</summary>
        public EventInstance Spawn(EconomyState state, PolicyState policy, string key, bool skipWarning)
        {
            int index = IndexOf(key);
            return index < 0 ? null : Spawn(state, policy, index, skipWarning);
        }

        EventInstance Spawn(EconomyState state, PolicyState policy, int index, bool skipWarning)
        {
            EventConfig cfg = _c.events[index];
            DeterministicRandom rng = state.events.random;

            EventInstance e = new EventInstance
            {
                id = state.events.nextId++,
                configIndex = index,
                key = cfg.key,
                severity = rng.Range(_t.severityMin, _t.severityMax),
                durationWeeks = rng.Range(cfg.minDurationWeeks, cfg.maxDurationWeeks + 1),
                nationIndex = ChooseNation(state, cfg),
                spawnWeek = state.week
            };
            state.events.live.Add(e);

            if (skipWarning || cfg.warningWeeks <= 0) { Strike(state, policy, e); return e; }

            if (cfg.earlySignalWeeks > 0)
            {
                e.stage = EventWarningLevel.EarlySignal;
                if (!string.IsNullOrEmpty(cfg.earlySignalHeadline))
                    state.events.Post(state.week, AlertLevel.Info, AlertChannel.Ticker, cfg.earlySignalHeadline);
            }
            else Advance(state, e, EventWarningLevel.Imminent);
            return e;
        }
    }
}
