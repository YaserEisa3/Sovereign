#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// A developer's record of a run: what the country looked like each year, what the
    /// player did to it, and what the world did back. Not a player feature - it exists
    /// because every balance judgement so far has come from probes running three rules,
    /// and a real playthrough is the one measurement none of them can make.
    ///
    /// Compiled out of a release build. Written as it goes, so a crash or a quit still
    /// leaves the run behind.
    /// </summary>
    public class RunLogger : MonoBehaviour
    {
        [SerializeField] SimulationRunner runner;

        readonly StringBuilder _log = new StringBuilder();
        readonly Dictionary<string, float> _lastTaxes = new Dictionary<string, float>();
        readonly Dictionary<string, float> _lastSpending = new Dictionary<string, float>();
        float _lastRate = -1f;
        int _alertsSeen;
        int _milestonesLast;
        bool _wasProsperous;
        string _path;

        void OnEnable()
        {
            if (runner == null) return;
            runner.OnWeekTick += FirstWeek;
            runner.OnYearTick += LogYear;
            runner.OnGameOver += LogEnding;
            runner.OnVictory += LogProsperity;
        }

        void OnDisable()
        {
            if (runner == null) return;
            runner.OnWeekTick -= FirstWeek;
            runner.OnYearTick -= LogYear;
            runner.OnGameOver -= LogEnding;
            runner.OnVictory -= LogProsperity;
            Flush();
        }

        void OnApplicationQuit() { Flush(); }

        /// <summary>Opens the file in week one rather than at the first year, so a run
        /// that falls apart in its first months still leaves a record - and so the RNG
        /// state recorded is close enough to the seed to replay from.</summary>
        void FirstWeek(EconomyState s)
        {
            if (_log.Length != 0) return;
            Open(s);
            // The diff baseline belongs HERE, not at the first year tick. Taken a year in,
            // year one could never show a change - which hid whether the opening moves
            // were ever made, in the one year of a run where they matter most.
            Snapshot();
        }

        void Snapshot()
        {
            foreach (KeyValuePair<string, float> tax in runner.Policy.taxRates) _lastTaxes[tax.Key] = tax.Value;
            foreach (KeyValuePair<string, float> line in runner.Policy.spendingBillions) _lastSpending[line.Key] = line.Value;
            _lastRate = runner.Policy.centralBankRate;
        }

        void LogYear(EconomyState s)
        {
            if (_log.Length == 0) Open(s);

            _log.Append("\n## Year ").Append(s.week / 52).Append("  -  Q").Append(s.Quarter).Append(' ').Append(s.Year).Append('\n');
            _log.Append("growth ").Append(s.realGdpGrowth.ToString("+0.0;-0.0"))
                .Append("%   unemp ").Append(s.unemployment.ToString("0.0"))
                .Append("%   CPI ").Append(s.inflation.ToString("0.0"))
                .Append("%   debt ").Append((s.DebtToGdp * 100f).ToString("0"))
                .Append("%   balance ").Append(s.budgetBalancePercentGdp.ToString("+0.0;-0.0"))
                .Append("%   approval ").Append(s.approval.overall.ToString("0"))
                .Append("   unrest ").Append(s.approval.unrestPoor.ToString("0"))
                .Append("   infra ").Append(s.infrastructureHealth.ToString("0"))
                .Append("   pop ").Append(s.population.Total.ToString("0"))
                .Append("M   births ").Append(s.population.birthRate.ToString("0.0"))
                .Append("   arrivals ").Append(s.population.netMigrationPerYear.ToString("0.00"))
                .Append("M\n");

            LogStanding(s);
            LogPolicyChanges(s);
            LogNewAlerts(s);
            LogMilestones(s);
            Flush();
        }

        /// <summary>
        /// Where policy actually STANDS, not just what moved. A log of diffs could not
        /// answer "what was the rate when this went wrong" - and the first run read back
        /// turned on exactly that question.
        /// </summary>
        void LogStanding(EconomyState s)
        {
            float infra = 0f;
            foreach (string line in SpendKeys.Infrastructure) infra += runner.Policy.Spending(line);

            _log.Append("SET: rate ").Append(runner.Policy.centralBankRate.ToString("0.00"))
                .Append("%  (real ").Append((runner.Policy.centralBankRate - s.inflation).ToString("+0.0;-0.0"))
                .Append("%, neutral 1.0)   infra $").Append(infra.ToString("0"))
                .Append("B of $192B needed to hold   spending $").Append(runner.Policy.TotalSpendingBillions.ToString("#,0"))
                .Append("B   revenue $").Append(s.revenueBillions.ToString("#,0")).Append("B\n");
        }

        /// <summary>Only what MOVED. A full budget every year is unreadable, and the
        /// question this log answers is "what did I do and what happened next".</summary>
        void LogPolicyChanges(EconomyState s)
        {
            List<string> changes = new List<string>();

            foreach (KeyValuePair<string, float> tax in runner.Policy.taxRates)
            {
                float was;
                if (_lastTaxes.TryGetValue(tax.Key, out was) && Mathf.Abs(was - tax.Value) > 0.001f)
                    changes.Add(tax.Key + " " + was.ToString("0.0") + " -> " + tax.Value.ToString("0.0") + "%");
                _lastTaxes[tax.Key] = tax.Value;
            }

            foreach (KeyValuePair<string, float> line in runner.Policy.spendingBillions)
            {
                float was;
                if (_lastSpending.TryGetValue(line.Key, out was) && Mathf.Abs(was - line.Value) > 0.5f)
                    changes.Add(line.Key + " $" + was.ToString("0") + "B -> $" + line.Value.ToString("0") + "B");
                _lastSpending[line.Key] = line.Value;
            }

            if (_lastRate >= 0f && Mathf.Abs(_lastRate - runner.Policy.centralBankRate) > 0.001f)
                changes.Add("policy rate " + _lastRate.ToString("0.00") + " -> " + runner.Policy.centralBankRate.ToString("0.00") + "%");
            _lastRate = runner.Policy.centralBankRate;

            if (changes.Count == 0) return;
            _log.Append("YOU: ").Append(string.Join(";  ", changes.ToArray())).Append('\n');
        }

        void LogNewAlerts(EconomyState s)
        {
            for (; _alertsSeen < s.events.alerts.Count; _alertsSeen++)
            {
                Alert alert = s.events.alerts[_alertsSeen];
                if (alert.level == AlertLevel.Info && alert.channel == AlertChannel.Ticker) continue;
                _log.Append("  w").Append(alert.week).Append(' ').Append(alert.level).Append(": ").Append(alert.text).Append('\n');
            }
        }

        void LogMilestones(EconomyState s)
        {
            int held = MilestoneModel.Count(s);
            if (held != _milestonesLast)
            {
                _log.Append(held > _milestonesLast ? "  MILESTONE reached - " : "  MILESTONE LOST - ")
                    .Append(held).Append(" of ").Append(MilestoneModel.Ladder.Length)
                    .Append(" now held. Next: ").Append(MilestoneModel.Next(s)).Append('\n');
                _milestonesLast = held;
            }

            if (s.prosperity == _wasProsperous) return;
            _wasProsperous = s.prosperity;
            _log.Append(s.prosperity ? "  *** PROSPEROUS ***\n" : "  *** slipped OUT of prosperity ***\n");
        }

        void LogProsperity(EconomyState s)
        {
            _log.Append("\n*** PROSPERITY reached in year ").Append(s.week / 52)
                .Append(" - debt ").Append((s.DebtToGdp * 100f).ToString("0"))
                .Append("%, unemployment ").Append(s.unemployment.ToString("0.0"))
                .Append("%, approval ").Append(s.approval.overall.ToString("0")).Append(" ***\n");
            Flush();
        }

        void LogEnding(EconomyState s)
        {
            _log.Append("\n## GAME OVER - year ").Append(s.week / 52).Append(", Q").Append(s.Quarter).Append(' ').Append(s.Year).Append('\n');
            _log.Append(s.approval.revoltReason).Append('\n');
            _log.Append("debt ").Append((s.DebtToGdp * 100f).ToString("0"))
                .Append("%   unemployment ").Append(s.unemployment.ToString("0.0"))
                .Append("%   inflation ").Append(s.inflation.ToString("0.0"))
                .Append("%   approval ").Append(s.approval.overall.ToString("0"))
                .Append(" (poor ").Append(s.approval.poor.ToString("0"))
                .Append(", middle ").Append(s.approval.middle.ToString("0"))
                .Append(", wealthy ").Append(s.approval.wealthy.ToString("0")).Append(")\n");
            _log.Append("milestones held at the end: ").Append(MilestoneModel.Count(s))
                .Append(" of ").Append(MilestoneModel.Ladder.Length).Append('\n');
            Flush();
        }
        /// <summary>
        /// Into the project's own Logs folder, which is already gitignored - a developer
        /// should not have to go hunting in persistentDataPath for this. One file per
        /// run, named for when it started, so runs are never overwritten.
        /// </summary>
        void Open(EconomyState s)
        {
            string folder = System.IO.Path.Combine(Application.dataPath, "../Logs");
            System.IO.Directory.CreateDirectory(folder);
            _path = System.IO.Path.Combine(folder,
                "run-" + System.DateTime.Now.ToString("yyyy-MM-dd-HHmm") + ".md");

            _log.Append("# Sovereign run - ").Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Append('\n');
            _log.Append("scenario: ").Append(runner.Scenario == null ? "none" : runner.Scenario.displayName).Append('\n');
            _log.Append("opening: debt ").Append((s.DebtToGdp * 100f).ToString("0"))
                .Append("%, unemployment ").Append(s.unemployment.ToString("0.0"))
                .Append("%, infrastructure ").Append(s.infrastructureHealth.ToString("0"))
                .Append(", population ").Append(s.population.Total.ToString("0")).Append("M\n");

            // The dice, so a run that goes strangely can be set up again. This is the
            // LIVE state rather than the seed it was built from - DeterministicRandom
            // advances as it is drawn from and does not keep the original - so it is
            // honest about which week it belongs to.
            _log.Append("rng state at week ").Append(s.week).Append(": ").Append(s.events.random.state).Append('\n');
            Debug.Log("RunLogger: writing this run to " + System.IO.Path.GetFullPath(_path));
        }

        void Flush()
        {
            if (_path == null || _log.Length == 0) return;
            try { System.IO.File.WriteAllText(_path, _log.ToString()); }
            catch (System.Exception e) { Debug.LogWarning("RunLogger: could not write the log - " + e.Message); }
        }

    }
}
#endif
