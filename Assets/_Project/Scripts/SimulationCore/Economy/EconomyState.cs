using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 5. Every indicator the game tracks, as a level plus a ten-year weekly
    /// series. Pure C#: this whole object serialises for saves without touching Unity.
    /// </summary>
    public class EconomyState
    {
        // --- Levels -----------------------------------------------------------
        public float nominalGdpBillions;
        public float realGdpIndex = 100f;
        public float potentialGdpIndex = 100f;

        public float realGdpGrowth;         // percent per year
        public float nominalGdpGrowth;
        public float inflation;             // headline CPI, percent per year
        public float coreInflation;
        public float unemployment;          // percent
        public float participationRate;     // percent of working-age population

        public float consumerConfidence = 50f;
        public float businessInvestmentIndex = 50f;
        public float giniCoefficient = 0.41f;
        public float infrastructureHealth = 82f;
        public float oilPriceIndex = 100f;
        public float housingCostIndex = 100f;
        public float averageWageIndex = 100f;

        /// <summary>Cumulative price level, 100 at the start. Fixed nominal spending
        /// erodes against this, which is why a budget left alone quietly shrinks in
        /// real terms - and why inflation is a debtor's friend right up until it is not.</summary>
        public float priceLevel = 100f;

        public float budgetBalancePercentGdp;
        public float revenueBillions;
        public float spendingBillions;

        public readonly TreasuryState treasury = new TreasuryState();
        public readonly PopulationState population = new PopulationState();
        public readonly ApprovalState approval = new ApprovalState();
        public readonly EventState events = new EventState();
        public readonly GeopoliticsState geopolitics = new GeopoliticsState();
        public readonly AchievementState achievements = new AchievementState();

        /// <summary>Trade volume with the world, 100 at the start. Wars and trade wars cut it.</summary>
        public float tradeVolumeIndex = 100f;

        /// <summary>Mirrored from policy each week so event conditions can read it.</summary>
        public float bankCapitalRequirement = 10f;

        /// <summary>The budget the economy has got used to, in the player's own dollars.
        /// A cut is measured against THIS and it drifts to the new level over a year, so
        /// austerity hurts while it happens and leaves a smaller state behind.</summary>
        public float settledSpendingBillions = -1f;

        /// <summary>Why the headline numbers moved this week. Derived from the same terms
        /// the numbers are summed from and rebuilt every tick, so it is not saved - a
        /// loaded game fills it again on its first week.</summary>
        [System.NonSerialized] public DriverBoard drivers = new DriverBoard();

        /// <summary>The floor under unemployment, which schooling moves over years. Saved,
        /// because it is a decade of investment rather than something derived from this
        /// week.</summary>
        public float naturalRate = -1f;

        /// <summary>GDD 4: OnRevolt leads to OnGameOver. Nothing else ends a run yet.</summary>
        public bool IsGameOver { get { return approval.revolt; } }

        /// <summary>The other ending: the scenario's victory conditions held for long
        /// enough. The clock keeps running afterwards - this is an achievement, not a
        /// full stop.</summary>
        public bool prosperity;
        public int victoryWeeks;
        public readonly BondMarketState bonds = new BondMarketState();
        public readonly CurrencyState currency = new CurrencyState();
        public readonly MonetaryState monetary = new MonetaryState();
        public readonly List<EconomicSector> sectors = new List<EconomicSector>();

        // --- Clock ------------------------------------------------------------
        public int week;                    // weeks elapsed since the run began
        public int StartYear = 2027;

        public int Year { get { return StartYear + week / 52; } }
        public int Quarter { get { return (week % 52) / 13 + 1; } }
        public bool IsQuarterBoundary { get { return week > 0 && week % 13 == 0; } }
        public bool IsYearBoundary { get { return week > 0 && week % 52 == 0; } }

        public float DebtToGdp
        {
            get { return nominalGdpBillions <= 0f ? 0f : bonds.debtStockBillions / nominalGdpBillions; }
        }

        public float OutputGap { get { return realGdpIndex - potentialGdpIndex; } }

        /// <summary>The same gap as a percent of potential - what an economist quotes.</summary>
        public float OutputGapPercent
        {
            get { return potentialGdpIndex <= 0f ? 0f : (realGdpIndex - potentialGdpIndex) / potentialGdpIndex * 100f; }
        }

        /// <summary>Real policy rate - the number that actually decides whether money is
        /// tight or loose, and the one players forget to look at.</summary>
        public float RealInterestRate { get { return monetary.centralBankRate - inflation; } }

        // --- Series -----------------------------------------------------------
        public readonly Dictionary<string, TimeSeries> series = new Dictionary<string, TimeSeries>();

        /// <summary>A series if it has been recorded, without creating one. The dashboard
        /// reads per-line revenue history this way: a reader that quietly adds an empty
        /// series puts UI state into the save file.</summary>
        public TimeSeries Recorded(string key)
        {
            TimeSeries found;
            return series.TryGetValue(key, out found) ? found : null;
        }

        public TimeSeries Series(string key)
        {
            TimeSeries found;
            if (series.TryGetValue(key, out found)) return found;
            found = new TimeSeries(key);
            series[key] = found;
            return found;
        }

        public void RecordWeek()
        {
            Series("gdpGrowth").Record(realGdpGrowth);
            Series("nominalGdpGrowth").Record(nominalGdpGrowth);
            Series("inflation").Record(inflation);
            Series("coreInflation").Record(coreInflation);
            Series("unemployment").Record(unemployment);
            Series("participation").Record(participationRate);
            Series("debtToGdp").Record(DebtToGdp * 100f);
            Series("budgetBalance").Record(budgetBalancePercentGdp);
            Series("baseRate").Record(monetary.centralBankRate);
            Series("moneySupplyGrowth").Record(monetary.moneySupplyGrowth);
            Series("shortYield").Record(bonds.yields.shortTermYield);
            Series("mediumYield").Record(bonds.yields.mediumTermYield);
            Series("longYield").Record(bonds.yields.longTermYield);
            Series("currency").Record(currency.exchangeRateIndex);
            Series("fxReserves").Record(currency.fxReservesBillions);
            Series("confidence").Record(consumerConfidence);
            Series("investment").Record(businessInvestmentIndex);
            Series("gini").Record(giniCoefficient);
            Series("infrastructure").Record(infrastructureHealth);
            Series("marketConfidence").Record(monetary.MarketConfidence);
            Series("currentAccount").Record(currency.currentAccountPercentGdp);
            Series("averageWage").Record(averageWageIndex);
            Series("population").Record(population.Total);
            Series("realWage").Record(priceLevel <= 0f ? 100f : averageWageIndex / priceLevel * 100f);
            Series("dependencyRatio").Record(population.DependencyRatio);
        }

        public EconomicSector GetSector(SectorId id)
        {
            for (int i = 0; i < sectors.Count; i++)
                if (sectors[i].id == id) return sectors[i];
            return null;
        }

        /// <summary>A single number for how healthy the productive economy is - the
        /// GDP-weighted average of sector health.</summary>
        public float WeightedSectorHealth
        {
            get
            {
                float total = 0f, weight = 0f;
                for (int i = 0; i < sectors.Count; i++)
                {
                    total += sectors[i].health * sectors[i].gdpShare;
                    weight += sectors[i].gdpShare;
                }
                return weight <= 0f ? 0f : total / weight;
            }
        }
    }
}
