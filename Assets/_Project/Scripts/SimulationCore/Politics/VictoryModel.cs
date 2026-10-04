namespace Sovereign.Core
{
    /// <summary>
    /// What a scenario counts as having WON. Authored per scenario, because
    /// "prosperity" means something different coming out of a war than it does in a
    /// steady state. All of it zeroed means the scenario has no victory and the run
    /// is open-ended.
    /// </summary>
    public struct VictoryConfig
    {
        public string title;
        public string summary;

        /// <summary>Years every condition below must hold, together, without a break.
        /// 0 disables the victory entirely.</summary>
        public float years;

        public float debtToGdp;          // at or below
        public float unemployment;       // at or below
        public float infrastructure;     // at or above
        public float realWage;           // at or above, index against the opening
        public float approval;           // at or above
    }

    /// <summary>
    /// GDD 20. The other ending. A government can fall in a week; it cannot be said
    /// to have rebuilt a country in one, so prosperity is a state of affairs that has
    /// to HOLD - every condition at once, for years - before the run is won. Slip on
    /// any of them and the clock starts again, which is what makes the last mile of a
    /// recovery as hard as the first.
    /// </summary>
    public class VictoryModel
    {
        readonly VictoryConfig _c;

        public VictoryModel(VictoryConfig config) { _c = config; }

        public VictoryConfig Config { get { return _c; } }
        public bool Enabled { get { return _c.years > 0f; } }

        public void TickWeek(EconomyState state)
        {
            if (!Enabled || state.prosperity || state.IsGameOver) return;

            if (!Holds(state))
            {
                state.victoryWeeks = 0;
                return;
            }

            state.victoryWeeks++;
            if (state.victoryWeeks < (int)(_c.years * 52f)) return;

            state.prosperity = true;
            state.events.Post(state.week, AlertLevel.Info, AlertChannel.Ticker,
                _c.title + " - " + _c.summary);
        }

        /// <summary>Every condition, at once. The list is deliberately short and all of
        /// it is the player's doing.</summary>
        public bool Holds(EconomyState state)
        {
            return state.DebtToGdp <= _c.debtToGdp
                && state.unemployment <= _c.unemployment
                && state.infrastructureHealth >= _c.infrastructure
                && state.Series("realWage").Latest >= _c.realWage
                && state.approval.overall >= _c.approval;
        }

        /// <summary>How far off each condition is, for the player who wants to know what
        /// is still missing.</summary>
        public string Outstanding(EconomyState state)
        {
            if (!Enabled) return "";
            System.Text.StringBuilder missing = new System.Text.StringBuilder();

            if (state.DebtToGdp > _c.debtToGdp)
                Add(missing, "debt " + (state.DebtToGdp * 100f).ToString("0") + "% needs " + (_c.debtToGdp * 100f).ToString("0") + "%");
            if (state.unemployment > _c.unemployment)
                Add(missing, "unemployment " + state.unemployment.ToString("0.0") + "% needs " + _c.unemployment.ToString("0.0") + "%");
            if (state.infrastructureHealth < _c.infrastructure)
                Add(missing, "infrastructure " + state.infrastructureHealth.ToString("0") + " needs " + _c.infrastructure.ToString("0"));
            if (state.Series("realWage").Latest < _c.realWage)
                Add(missing, "real wages " + state.Series("realWage").Latest.ToString("0") + " need " + _c.realWage.ToString("0"));
            if (state.approval.overall < _c.approval)
                Add(missing, "approval " + state.approval.overall.ToString("0") + "% needs " + _c.approval.ToString("0") + "%");

            if (missing.Length == 0)
            {
                int weeks = (int)(_c.years * 52f) - state.victoryWeeks;
                return "all conditions met - " + (weeks / 52f).ToString("0.0") + " years to hold them";
            }
            return missing.ToString();
        }

        static void Add(System.Text.StringBuilder text, string item)
        {
            if (text.Length > 0) text.Append(", ");
            text.Append(item);
        }
    }
}
