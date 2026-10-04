namespace Sovereign.Core
{
    /// <summary>
    /// GDD 20, forever mode. A country is never finished, so there is no finish line -
    /// but a run with nothing to aim at between "just survived a war" and "fully
    /// recovered" is thirty years of fog. This is the ladder in between: each rung is
    /// a real thing a government can point at, announced when the country reaches it.
    ///
    /// Rungs can be LOST. That is the point: holding what you have built is its own
    /// job, and a country that slips back is told so.
    /// </summary>
    public class MilestoneModel
    {
        public struct Rung
        {
            public string name;
            public string reached;
            public string lost;
        }

        // Ordered easiest to hardest, which is roughly the order a recovery meets them.
        public static readonly Rung[] Ladder =
        {
            new Rung { name = "Debt under 120% of GDP", reached = "The debt is under 120% of GDP. The emergency is over, the problem is not.", lost = "Debt has climbed back over 120% of GDP." },
            new Rung { name = "Unemployment under 12%", reached = "Unemployment is under 12%. People are being hired again.", lost = "Unemployment is back over 12%." },
            new Rung { name = "Infrastructure above 50", reached = "Infrastructure has passed 50 out of 100. The country works again.", lost = "Infrastructure has fallen back below 50." },
            new Rung { name = "Wages back to pre-war", reached = "Real wages are back where they were before the war. People can feel it.", lost = "Real wages have fallen back below their pre-war level." },
            new Rung { name = "Debt under 90% of GDP", reached = "The debt is under 90% of GDP - what an ordinary country carries.", lost = "Debt has climbed back over 90% of GDP." },
            new Rung { name = "Unemployment under 8%", reached = "Unemployment is under 8%. This is close to as good as a shocked economy gets.", lost = "Unemployment is back over 8%." },
            new Rung { name = "Infrastructure above 75", reached = "Infrastructure has passed 75. Nothing is being held back by the state of the country.", lost = "Infrastructure has fallen back below 75." },
            new Rung { name = "Debt under 60% of GDP", reached = "The debt is under 60% of GDP. The reconstruction loan is history.", lost = "Debt has climbed back over 60% of GDP." },
        };

        public static bool Holds(int rung, EconomyState s)
        {
            switch (rung)
            {
                case 0: return s.DebtToGdp <= 1.2f;
                case 1: return s.unemployment <= 12f;
                case 2: return s.infrastructureHealth >= 50f;
                case 3: return s.Series("realWage").Latest >= 100f;
                case 4: return s.DebtToGdp <= 0.9f;
                case 5: return s.unemployment <= 8f;
                case 6: return s.infrastructureHealth >= 75f;
                case 7: return s.DebtToGdp <= 0.6f;
            }
            return false;
        }

        /// <summary>Announces a rung the first time it is reached, and says so when one
        /// is lost. Held in a bitmask so a save keeps the country's record.</summary>
        public void TickWeek(EconomyState s)
        {
            if (s.IsGameOver) return;

            for (int i = 0; i < Ladder.Length; i++)
            {
                bool holds = Holds(i, s);
                bool had = (s.milestonesReached & (1 << i)) != 0;
                if (holds == had) continue;

                s.milestonesReached = holds ? s.milestonesReached | (1 << i)
                                            : s.milestonesReached & ~(1 << i);
                s.events.Post(s.week, holds ? AlertLevel.Info : AlertLevel.Warning, AlertChannel.Ticker,
                              holds ? Ladder[i].reached : Ladder[i].lost);
            }
        }

        /// <summary>The next thing to aim at, or empty once every rung is held.</summary>
        public static string Next(EconomyState s)
        {
            for (int i = 0; i < Ladder.Length; i++)
                if ((s.milestonesReached & (1 << i)) == 0) return Ladder[i].name;
            return "";
        }

        public static int Count(EconomyState s)
        {
            int held = 0;
            for (int i = 0; i < Ladder.Length; i++) if ((s.milestonesReached & (1 << i)) != 0) held++;
            return held;
        }
    }
}
