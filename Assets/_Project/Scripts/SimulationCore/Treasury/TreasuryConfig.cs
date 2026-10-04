namespace Sovereign.Core
{
    /// <summary>One revenue line, copied from a TaxDefinition at boot.</summary>
    public struct TaxLineConfig
    {
        public string key;
        public string name;
        public RevenueBase revenueBase;
        public PolicyUnit unit;
        /// <summary>Share of the revenue base this line applies to. The four income
        /// brackets split the taxable income pool between them.</summary>
        public float baseShare;
        public float collectionEfficiency;
        public float baseElasticity;
        public bool isCredit;
        public float defaultValue;
        public float minimumValue;
        public float maximumValue;
    }

    /// <summary>One spending line, copied from a SpendingCategoryDefinition at boot.</summary>
    public struct SpendLineConfig
    {
        public string key;
        public string name;
        public SpendingGroup group;
        public bool isAutomatic;
        public bool isAutomaticStabiliser;
        /// <summary>$B per point of unemployment above the natural rate - GDD 8.3.</summary>
        public float stabiliserSensitivity;
        public int lagQuarters;
    }

    /// <summary>
    /// The sizes of the things taxes are levied on. Most are shares of nominal GDP;
    /// carbon is gigatonnes and the child credit is a headcount, because a
    /// dollars-per-ton tax and a dollars-per-child credit are not percentages of
    /// anything.
    /// </summary>
    public struct TaxBaseConfig
    {
        public float taxableIncomeShare;     // wages and salaries
        public float corporateProfitShare;
        public float capitalGainsShare;
        public float payrollShare;
        public float consumerSpendingShare;
        public float importShare;
        public float estateShare;
        public float financialTurnoverMultiple;  // turnover is a multiple of GDP, not a share
        public float carbonEmissionsGigatonnes;
        public float childrenMillions;
    }

    public struct TreasuryConfig
    {
        /// <summary>How much faster a base runs away as the rate climbs above the going
        /// rate. 0 is linear; higher makes punitive rates collect far less than their
        /// arithmetic promises.</summary>
        public float avoidanceCurvature;

        public TaxLineConfig[] taxes;
        public SpendLineConfig[] spending;
        public TaxBaseConfig bases;

        /// <summary>Revenue at the opening position, as a share of GDP. The per-line
        /// estimates are scaled once at boot to hit it, so the relative weight of
        /// each tax stays honest while the macro calibration holds.</summary>
        public float openingRevenueShareOfGdp;

        /// <summary>Dollars per veteran per year. Zero leaves Veterans Benefits at its
        /// set level instead of computing it from the veteran count.</summary>
        public float veteranBenefitCostPerHead;
    }
}
