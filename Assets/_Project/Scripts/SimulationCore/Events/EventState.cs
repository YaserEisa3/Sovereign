using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>One event, from its first rumour to its resolution.</summary>
    public class EventInstance
    {
        public int id;
        public int configIndex;
        public string key;
        public EventWarningLevel stage;
        public int weeksInStage;
        public int durationWeeks;
        public float severity;      // 0.7 - 1.3, rolled at spawn
        public float mitigation;    // 0 - 0.9, fixed at the moment it strikes
        public int nationIndex;     // where in the world it happens
        public int spawnWeek;
        public int strikeWeek = -1;

        public bool IsActive { get { return stage == EventWarningLevel.Active; } }
        public int WeeksRemaining { get { return IsActive ? durationWeeks - weeksInStage : 0; } }
    }

    public struct EventRecord
    {
        public string key;
        public string title;
        public int startWeek;
        public int endWeek;
        public float severity;
        public float mitigation;
        public string outcome;
    }

    public enum AlertLevel { Info, Warning, Danger }
    public enum AlertChannel { Ticker, Advisor }

    /// <summary>GDD 17: events speak through the ticker and the advisor, never a menu.</summary>
    public struct Alert
    {
        public int week;
        public AlertLevel level;
        public AlertChannel channel;
        public string text;
    }

    /// <summary>GDD 17.3. A war fought against you, tracked in plain numbers.</summary>
    public class WarState
    {
        public bool active;
        public int eventId;
        public int weeks;
        public float kia;
        public float equipmentStock = 100f;
        public float cumulativeCostBillions;
        public float gapSum;
        public float enemyStrength;
        public string lastReport = "";
    }

    public class EventState
    {
        public readonly List<EventInstance> live = new List<EventInstance>();
        public readonly List<EventRecord> history = new List<EventRecord>();
        public readonly List<Alert> alerts = new List<Alert>();
        public readonly WarState war = new WarState();
        public DeterministicRandom random = new DeterministicRandom(1u);
        /// <summary>The newsroom has its own dice, so writing a headline never changes
        /// which events the world rolls.</summary>
        public DeterministicRandom newsRandom = new DeterministicRandom(7u);

        // Everything below used to live inside the models. A save must capture ALL
        // of a run, so it lives in state now: opening spending the events judge your
        // preparation against, the run of failed auctions, what the advisor has
        // already said, and which headlines are resting.
        public float baselinePublicHealth, baselineHealth, baselineDefence;
        public int failedAuctions;
        public readonly List<string> advisorRaised = new List<string>();
        public readonly List<string> recentTopics = new List<string>();
        public readonly List<string> recentTemplates = new List<string>();
        public int nextId = 1;

        // Written each week by the events, read by the population model.
        public float deathRateOffset, birthRateOffset, laborForceOffsetPercent;

        /// <summary>GDD 17.4: an under-funded pandemic leaves a permanent productivity
        /// scar, taken straight off potential growth.</summary>
        public float productivityScar;

        public int ActiveCount
        {
            get { int n = 0; foreach (EventInstance e in live) if (e.IsActive) n++; return n; }
        }

        public int WarningCount { get { return live.Count - ActiveCount; } }

        public void Post(int week, AlertLevel level, AlertChannel channel, string text)
        {
            alerts.Add(new Alert { week = week, level = level, channel = channel, text = text });
            if (alerts.Count > 120) alerts.RemoveAt(0);
        }
    }
}
