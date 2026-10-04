using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// One named cause of a headline number, and how much of that number it accounts
    /// for. The sign is the direction it pushes: a negative contribution is dragging
    /// the figure down.
    /// </summary>
    public struct Driver
    {
        /// <summary>What it is, with its reading in it: "Interest rates 8.0%".</summary>
        public string label;
        /// <summary>Signed contribution, in the headline figure's own unit.</summary>
        public float points;
        /// <summary>Plain language: why it is pushing the way it is.</summary>
        public string note;

        public float Size { get { return points < 0f ? -points : points; } }
    }

    /// <summary>
    /// GDD 18. Why the headline numbers are what they are. Each list is built from the
    /// SAME terms the number is summed from - the model adds this list up rather than
    /// keeping a second copy - so an explanation cannot drift from the figure it
    /// explains, the way the approval drivers cannot drift from the approval bar.
    ///
    /// Rebuilt from scratch every week, so it is never saved.
    /// </summary>
    public class DriverBoard
    {
        public readonly List<Driver> growth = new List<Driver>();
        public readonly List<Driver> unemployment = new List<Driver>();

        /// <summary>Trend growth, which the growth drivers push around. Held apart from
        /// the ranking rather than listed in it: it is much the largest term and barely
        /// moves, so it would occupy a place in any top three forever while telling the
        /// player nothing they can act on.</summary>
        public float growthTrend;

        /// <summary>Where growth is HEADING, before stickiness. The reading on the
        /// dashboard is a slow average of this, which is why a policy change can look
        /// like it did nothing for months.</summary>
        public float growthTarget;

        /// <summary>Unemployment as this model left it last week. Anything that moved it
        /// since was something else - an event, a disaster, a war - and is named as such.
        /// Negative until the first week has run, so a fresh or just-loaded game does not
        /// report the whole rate as a shock.</summary>
        public float unemploymentAfterLastWeek = -1f;

        public void Clear()
        {
            growth.Clear();
            unemployment.Clear();
        }

        public static void Add(List<Driver> into, string label, float points, string note)
        {
            into.Add(new Driver { label = label, points = points, note = note });
        }

        /// <summary>The few that move the number most, either way.</summary>
        public static List<Driver> Top(List<Driver> all, int count)
        {
            List<Driver> sorted = new List<Driver>(all);
            sorted.Sort(BySize);
            if (sorted.Count > count) sorted.RemoveRange(count, sorted.Count - count);
            return sorted;
        }

        static int BySize(Driver a, Driver b) { return b.Size.CompareTo(a.Size); }
    }
}
