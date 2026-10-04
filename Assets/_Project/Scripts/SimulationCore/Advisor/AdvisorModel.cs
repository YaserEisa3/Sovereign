using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 19. The Chief Economic Advisor, in plain English: thresholds approaching,
    /// lags arriving, contradictions in your own policy. Each warning fires on the
    /// way IN to a condition and re-arms once the condition clears, so the advisor
    /// speaks when something changes rather than nagging every week.
    /// </summary>
    public class AdvisorModel
    {
        readonly float _inflationTarget;

        public AdvisorModel(float inflationTarget) { _inflationTarget = inflationTarget; }

        public void Reset(EconomyState state) { state.events.advisorRaised.Clear(); }

        public void TickWeek(EconomyState s, PolicyState p)
        {
            Watch(s, "inverted", s.bonds.yields.IsInverted, AlertLevel.Warning,
                  "The yield curve has inverted - short rates above long. Markets read that as a recession coming, "
                  + "and consumer confidence will take a hit whether or not one arrives.");

            Watch(s, "watch", s.bonds.ratingWatchNegative, AlertLevel.Danger,
                  "The rating agencies have us on watch negative at " + s.bonds.creditRating
                  + ". Without a credible fiscal change, the downgrade comes at the next annual review.");

            Watch(s, "unanchored", s.monetary.expectationsUnanchored, AlertLevel.Danger,
                  "Inflation expectations have come unanchored. Wages will now chase prices. Getting out of this "
                  + "means rates high enough to cause a recession on purpose.");

            Watch(s, "printing", p.qeAmountPerQuarter > 0f && s.coreInflation > _inflationTarget + 2f, AlertLevel.Warning,
                  "We are running QE with core inflation at " + s.coreInflation.ToString("0.0")
                  + "%. That risks unanchoring expectations, and the market will stop believing us.");

            Watch(s, "debt130", s.DebtToGdp > 1.3f, AlertLevel.Warning,
                  "Debt has passed 130% of GDP. That is junk-rating territory, and every point from here costs more to borrow.");

            Watch(s, "debt180", s.DebtToGdp > 1.8f, AlertLevel.Danger,
                  "Debt is above 180% of GDP. A debt crisis can now start at any auction.");

            Watch(s, "infra", s.infrastructureHealth < 70f, AlertLevel.Warning,
                  "Infrastructure has fallen below 70%. It is now dragging on growth in every sector, and repairs cost more than maintenance would have.");

            Watch(s, "approval", s.approval.overall < 40f, AlertLevel.Warning,
                  "Overall approval is under 40%. Below 20% for six months, the government falls.");

            Watch(s, "unrest", s.approval.HighestUnrest > 50f, AlertLevel.Danger,
                  "Unrest is past the halfway mark. Above 90% for two months, that class brings the government down.");

            // GDD 19's lag flag: the hikes of three quarters ago arriving now.
            bool lagArriving = s.Series("realRate").Ago(39) - s.Series("realRate").Ago(52) > 1f
                               && s.unemployment - s.Series("unemployment").Ago(13) > 0.3f;
            Watch(s, "lag", lagArriving, AlertLevel.Info,
                  "The rate rises from three quarters ago are now showing up in unemployment. That was always going to happen - it is the lag.");
        }

        void Watch(EconomyState s, string id, bool condition, AlertLevel level, string message)
        {
            // What the advisor has already said lives in state, so a loaded game does
            // not repeat every standing warning on its first week.
            List<string> raised = s.events.advisorRaised;
            if (condition && !raised.Contains(id)) { raised.Add(id); s.events.Post(s.week, level, AlertChannel.Advisor, message); }
            else if (!condition) raised.Remove(id);
        }
    }
}
