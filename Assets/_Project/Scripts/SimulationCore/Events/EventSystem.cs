using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 17. Events move from a rumour on the ticker, to a warning from the advisor,
    /// to a strike - and then they simply happen to your economy, week after week,
    /// until they end. There is never a menu of choices: the player answers through
    /// the ordinary policy controls, and whatever those were set to at the moment of
    /// the strike decides how much it hurts.
    /// </summary>
    public partial class EventSystem
    {
        const float Weekly = 1f / 52f;

        readonly EventsConfig _c;
        readonly EventTuning _t;

        /// <summary>Off in the economy tests, which need a world without surprises to
        /// measure mechanics. Events can still be spawned by hand when it is off.</summary>
        public bool RandomEventsEnabled = true;

        public EventSystem(EventsConfig config) { _c = config; _t = config.tuning; }

        public EventsConfig Config { get { return _c; } }

        public void Initialise(EconomyState state, PolicyState policy)
        {
            state.events.random = new DeterministicRandom(_c.seed);
            // Opening spending, so preparation is judged against where the run began.
            state.events.baselinePublicHealth = policy.Spending(SpendKeys.PublicHealth);
            state.events.baselineHealth = Health(policy);
            state.events.baselineDefence = Defence(policy);
            state.events.failedAuctions = 0;
        }

        public void TickWeek(EconomyState state, PolicyState policy)
        {
            EventState ev = state.events;
            ev.deathRateOffset = 0f;
            ev.birthRateOffset = 0f;
            ev.laborForceOffsetPercent = 0f;

            List<EventInstance> snapshot = new List<EventInstance>(ev.live);
            foreach (EventInstance e in snapshot)
            {
                e.weeksInStage++;
                EventConfig cfg = _c.events[e.configIndex];

                if (e.stage == EventWarningLevel.EarlySignal && e.weeksInStage >= cfg.earlySignalWeeks)
                {
                    if (cfg.warningWeeks - cfg.earlySignalWeeks > 0) Advance(state, e, EventWarningLevel.Imminent);
                    else Strike(state, policy, e);
                }
                else if (e.stage == EventWarningLevel.Imminent && e.weeksInStage >= cfg.warningWeeks - cfg.earlySignalWeeks)
                {
                    Strike(state, policy, e);
                }
                else if (e.IsActive)
                {
                    ApplyImpacts(state, policy, e);
                    if (IsWarAgainstYou(cfg)) TickWar(state, policy, e);
                    if (e.weeksInStage >= e.durationWeeks) Resolve(state, e);
                }
            }
        }

        /// <summary>Quarterly: roll for new events, and for escalation of live ones.</summary>
        public void TickQuarter(EconomyState state, PolicyState policy)
        {
            CheckDefault(state);
            if (!RandomEventsEnabled) return;

            for (int i = 0; i < _c.events.Length; i++)
            {
                if (!CanFire(state, i)) continue;
                if (state.events.random.Value() < _c.events[i].baseAnnualProbability * 0.25f) Spawn(state, policy, i, false);
            }

            foreach (EventInstance e in new List<EventInstance>(state.events.live))
            {
                EventConfig cfg = _c.events[e.configIndex];
                if (!e.IsActive || cfg.escalationRisk <= 0f || string.IsNullOrEmpty(cfg.escalatesToKey)) continue;
                int target = IndexOf(cfg.escalatesToKey);
                if (target >= 0 && CanFire(state, target) && state.events.random.Value() < cfg.escalationRisk)
                {
                    state.events.Post(state.week, AlertLevel.Danger, AlertChannel.Ticker,
                                      cfg.title + " is escalating into " + _c.events[target].title.ToLowerInvariant());
                    Spawn(state, policy, target, false);
                }
            }
        }

        public int IndexOf(string key)
        {
            for (int i = 0; i < _c.events.Length; i++) if (_c.events[i].key == key) return i;
            return -1;
        }

        void Advance(EconomyState state, EventInstance e, EventWarningLevel stage)
        {
            e.stage = stage;
            e.weeksInStage = 0;
            EventConfig cfg = _c.events[e.configIndex];
            if (stage == EventWarningLevel.Imminent && !string.IsNullOrEmpty(cfg.imminentAdvisorLine))
                state.events.Post(state.week, AlertLevel.Warning, AlertChannel.Advisor, cfg.imminentAdvisorLine);
        }

        void Strike(EconomyState state, PolicyState policy, EventInstance e)
        {
            EventConfig cfg = _c.events[e.configIndex];
            e.stage = EventWarningLevel.Active;
            e.weeksInStage = 0;
            e.strikeWeek = state.week;
            // Only events with something preparation CAN soften get credit for it -
            // defence spending does nothing for a trade war, and must not claim to.
            e.mitigation = Mitigable(cfg) ? Mitigation(cfg.category, policy, state) : 0f;

            string where = e.nationIndex >= 0 && !_c.nations[e.nationIndex].isPlayer ? " - " + _c.nations[e.nationIndex].name : "";
            state.events.Post(state.week, AlertLevel.Danger, AlertChannel.Ticker, "BREAKING: " + cfg.title + where);
            if (e.mitigation >= 0.5f)
                state.events.Post(state.week, AlertLevel.Info, AlertChannel.Advisor,
                    cfg.title + ": our preparation is absorbing about " + (e.mitigation * 100f).ToString("0") + "% of the damage.");
            else if (e.mitigation < 0.25f && Mitigable(cfg))
                state.events.Post(state.week, AlertLevel.Warning, AlertChannel.Advisor,
                    cfg.title + " has hit us unprepared. Almost nothing is softening it.");

            if (IsWarAgainstYou(cfg)) StartWar(state, e);
        }

        void Resolve(EconomyState state, EventInstance e)
        {
            EventConfig cfg = _c.events[e.configIndex];
            string outcome = "passed";

            if (cfg.category == EventCategory.Pandemic && e.mitigation < _t.pandemicScarThreshold)
            {
                float scar = _t.pandemicScarSize * e.severity;
                state.events.productivityScar += scar;
                outcome = "left a permanent scar of " + scar.ToString("0.00") + " points on potential growth";
                state.events.Post(state.week, AlertLevel.Warning, AlertChannel.Advisor,
                    "The pandemic is over, but an under-funded health system leaves a permanent mark: potential growth is "
                    + scar.ToString("0.00") + " points lower for good.");
            }
            if (IsWarAgainstYou(cfg)) outcome = EndWar(state);

            state.events.history.Add(new EventRecord
            {
                key = e.key, title = cfg.title, startWeek = e.strikeWeek, endWeek = state.week,
                severity = e.severity, mitigation = e.mitigation, outcome = outcome
            });
            state.events.Post(state.week, AlertLevel.Info, AlertChannel.Ticker, cfg.title + " has ended - " + outcome + ".");
            state.events.live.Remove(e);
        }

        bool IsWarAgainstYou(EventConfig cfg) { return cfg.key == "InvasionThreat"; }

        bool Mitigable(EventConfig cfg)
        {
            foreach (ImpactConfig impact in cfg.impacts) if (impact.mitigable > 0f) return true;
            return false;
        }
    }
}
