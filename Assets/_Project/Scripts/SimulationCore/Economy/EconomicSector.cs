namespace Sovereign.Core
{
    /// <summary>
    /// GDD 6. One of seven. Health drifts with policy and the cycle; the wage floats
    /// around the base wage from its SectorDefinition rather than sitting fixed,
    /// because the inflation spiral in GDD 12 needs wages that chase prices.
    /// </summary>
    public class EconomicSector
    {
        public SectorId id;
        public string name;

        public float gdpShare;
        public float employmentShare;
        public float health;              // 0-100
        public float averageWage;         // dollars per year, floats around the base
        public float baseWage;            // starting wage from the asset
        /// <summary>Where wage bargaining is heading. It grows at the demanded rate; the
        /// actual wage follows it at the sector's stickiness. Stickiness sets the lag,
        /// never the long-run growth - applying it to a growth rate made sticky sectors
        /// grow slower forever and loose ones faster.</summary>
        public float targetWage;
        public float headcount;           // millions - filled in by Phase 2's employment model
        public float effectiveTaxRate;
        public float regulationBurden;    // 0-100
        public float subsidyLevel;        // $B/yr
        public float foreignCompetition;  // 0-100
        public float exportShare;
        public float currencyExposure;

        public SectorConfig config;

        public EconomicSector(SectorConfig sectorConfig)
        {
            config = sectorConfig;
            id = sectorConfig.id;
            name = sectorConfig.name;
            gdpShare = sectorConfig.gdpShare;
            employmentShare = sectorConfig.employmentShare;
            baseWage = sectorConfig.baseAverageWage;
            averageWage = sectorConfig.baseAverageWage;
            targetWage = sectorConfig.baseAverageWage;
            exportShare = sectorConfig.exportShare;
            currencyExposure = sectorConfig.currencyExposure;
            health = 70f;
            regulationBurden = 30f;
            foreignCompetition = sectorConfig.foreignCompetitionExposure * 40f;
        }

        /// <summary>Headcount times wage: this sector's slice of the national taxable
        /// income pool, in billions per year (GDD 7.4).</summary>
        public float WageBillBillions { get { return headcount * averageWage / 1000f; } }
    }
}
