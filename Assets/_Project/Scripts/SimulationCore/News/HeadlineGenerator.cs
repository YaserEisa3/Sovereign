using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 20 Phase 5. Procedural headlines: every few weeks, pick a topic that is
    /// actually true this week, pick one of its templates, fill it with live numbers.
    /// The templates are content and live in a text asset a writer can edit; this
    /// only decides which topics hold and does the filling.
    /// </summary>
    public class HeadlineGenerator
    {
        readonly Dictionary<string, List<string>> _templates = new Dictionary<string, List<string>>();

        const int TemplateRest = 10;
        readonly int _intervalWeeks;
        readonly int _noRepeatTopics;

        public int TemplateCount { get; private set; }

        /// <param name="lines">"TOPIC|headline with {tokens}" - one per line, # for comments.</param>
        public HeadlineGenerator(IEnumerable<string> lines, int intervalWeeks, int noRepeatTopics)
        {
            _intervalWeeks = intervalWeeks < 1 ? 1 : intervalWeeks;
            _noRepeatTopics = noRepeatTopics;
            if (lines == null) return;

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int bar = line.IndexOf('|');
                if (bar <= 0) continue;

                string topic = line.Substring(0, bar).Trim();
                List<string> list;
                if (!_templates.TryGetValue(topic, out list)) { list = new List<string>(); _templates[topic] = list; }
                list.Add(line.Substring(bar + 1).Trim());
                TemplateCount++;
            }
        }

        public IEnumerable<string> Topics { get { return _templates.Keys; } }

        public void TickWeek(EconomyState state, PolicyState policy, HeadlineFacts facts)
        {
            if (_templates.Count == 0 || state.week % _intervalWeeks != 0) return;

            List<string> candidates = new List<string>();
            foreach (string topic in HeadlineTopics.TrueThisWeek(state, policy, facts))
                if (_templates.ContainsKey(topic) && !state.events.recentTopics.Contains(topic)) candidates.Add(topic);
            if (candidates.Count == 0) candidates.Add(HeadlineTopics.Fallback);
            if (!_templates.ContainsKey(candidates[0]) && candidates.Count == 1) return;

            DeterministicRandom rng = state.events.newsRandom;
            string chosen = candidates[rng.Range(0, candidates.Count)];
            // Rest recently used headlines too, or a quiet spell repeats itself verbatim.
            List<string> options = new List<string>();
            foreach (string template in _templates[chosen]) if (!state.events.recentTemplates.Contains(template)) options.Add(template);
            if (options.Count == 0) options = _templates[chosen];

            string picked = options[rng.Range(0, options.Count)];
            string text = Fill(picked, state, policy, HeadlineTopics.Focus(chosen, facts));
            state.events.recentTemplates.Add(picked);
            while (state.events.recentTemplates.Count > TemplateRest) state.events.recentTemplates.RemoveAt(0);

            state.events.Post(state.week, AlertLevel.Info, AlertChannel.Ticker, text);
            state.events.recentTopics.Add(chosen);
            while (state.events.recentTopics.Count > _noRepeatTopics) state.events.recentTopics.RemoveAt(0);
        }

        public static string Fill(string template, EconomyState s, PolicyState p, HeadlineFacts f)
        {
            StringBuilder b = new StringBuilder(template);
            Replace(b, "{growth}", s.realGdpGrowth, "0.0");
            Replace(b, "{inflation}", s.inflation, "0.0");
            Replace(b, "{core}", s.coreInflation, "0.0");
            Replace(b, "{unemployment}", s.unemployment, "0.0");
            Replace(b, "{rate}", s.monetary.centralBankRate, "0.00");
            Replace(b, "{short}", s.bonds.yields.shortTermYield, "0.00");
            Replace(b, "{long}", s.bonds.yields.longTermYield, "0.00");
            Replace(b, "{debt}", s.DebtToGdp * 100f, "0");
            Replace(b, "{deficit}", -s.budgetBalancePercentGdp, "0.0");
            Replace(b, "{approval}", s.approval.overall, "0");
            Replace(b, "{currency}", s.currency.exchangeRateIndex, "0.0");
            Replace(b, "{reserves}", s.currency.fxReservesBillions, "0");
            Replace(b, "{oil}", s.oilPriceIndex, "0");
            Replace(b, "{confidence}", s.consumerConfidence, "0");
            Replace(b, "{infra}", s.infrastructureHealth, "0");
            Replace(b, "{pop}", s.population.Total, "0.0");
            Replace(b, "{workers}", s.population.employed, "0.0");
            Replace(b, "{pool}", s.population.taxableIncomePool / 1000f, "0.0");
            Replace(b, "{dependency}", s.population.DependencyRatio, "0.00");
            Replace(b, "{veterans}", s.population.veterans, "0.0");
            Replace(b, "{gini}", s.giniCoefficient, "0.00");
            Replace(b, "{fdi}", s.currency.fdiInflowRate, "0");
            Replace(b, "{trade}", s.tradeVolumeIndex, "0");
            Replace(b, "{kia}", s.events.war.kia, "#,0");
            Replace(b, "{immigration}", p.immigrationInflowMillions, "0.0");
            Replace(b, "{unrest}", s.approval.HighestUnrest, "0");
            b.Replace("{rating}", s.bonds.creditRating.ToString());
            b.Replace("{quarter}", "Q" + s.Quarter);
            b.Replace("{year}", s.Year.ToString(CultureInfo.InvariantCulture));
            b.Replace("{nation}", f.nation ?? "a trading partner");
            b.Replace("{sector}", f.sector ?? "industry");
            Replace(b, "{nationTariff}", f.nationTariff, "0");
            Replace(b, "{rel}", f.relationship, "+0;-0");
            return b.ToString();
        }

        static void Replace(StringBuilder b, string token, float value, string format)
        {
            b.Replace(token, value.ToString(format, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>The names a headline might mention this week, chosen by the caller
    /// who knows the nations and sectors.</summary>
    public struct HeadlineFacts
    {
        public string nation;
        public float relationship;
        public float nationTariff;
        public string sector;
        public string warmestNation;
        public string coldestNation;
        public string tariffNation;
        public string walkingCreditor;
        public string strongSector;
        public string weakSector;
    }
}
