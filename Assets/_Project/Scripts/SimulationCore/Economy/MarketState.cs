using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>GDD 9.1. Three points on the curve, and the one question the market
    /// actually asks about it.</summary>
    public class YieldCurve
    {
        public float shortTermYield;
        public float mediumTermYield;
        public float longTermYield;

        /// <summary>GDD 9.1: not a guarantee of recession, but the market believes it
        /// is, and that belief moves confidence.</summary>
        public bool IsInverted { get { return shortTermYield > longTermYield; } }

        public float Spread { get { return longTermYield - shortTermYield; } }
    }

    /// <summary>GDD 9. The debt stock, who holds it, and what it costs.</summary>
    public class BondMarketState
    {
        public YieldCurve yields = new YieldCurve();

        public float debtStockBillions;
        public float averageCoupon;            // percent, moves as new debt replaces old
        public float riskPremium;              // percentage points the market adds for your fiscal position
        public CreditRating creditRating = CreditRating.AA;
        public bool ratingWatchNegative;       // telegraphed one quarter ahead, GDD 9.5

        public float foreignHoldingPercent;    // 0-1
        public float foreignCurrencyDebtPercent;
        public readonly Dictionary<string, float> holdingsByNation = new Dictionary<string, float>();

        // Maturity profile, shares summing to 1 (GDD 9.2).
        public float maturingWithinOneYear = 0.22f;
        public float maturingOneToFive = 0.43f;
        public float maturingBeyondFive = 0.35f;

        /// <summary>Demand at the last auction, 1 is a clean sale. Below 1 means yields
        /// had to rise to clear - the market disciplining fiscal policy.</summary>
        public float lastAuctionCover = 1f;

        /// <summary>What foreign buyers added to (or took from) the last auction.</summary>
        public float lastForeignDemand;

        /// <summary>The last buyback: what it retired, what it cost, and at what price.</summary>
        public float lastBuybackFace, lastBuybackCost, lastBuybackPrice;
        public int lastBuybackWeek = -1;

        public float AnnualDebtServiceBillions { get { return debtStockBillions * averageCoupon * 0.01f; } }
    }

    /// <summary>GDD 10.</summary>
    public class CurrencyState
    {
        public float exchangeRateIndex = 100f;
        public float realEffectiveExchangeRate = 100f;
        public float fxReservesBillions;
        public float fdiInflowRate;                    // $B per year
        public float manufacturingCompetitiveness = 50f;
        public float currentAccountPercentGdp;
        public float interventionThisQuarter;          // + defends the currency, - weakens it
    }

    /// <summary>GDD 9.4 and 12. Credibility is hidden from the player and shown only
    /// as Market Confidence.</summary>
    public class MonetaryState
    {
        public float centralBankRate;
        public float moneySupplyM2Billions;
        public float moneySupplyGrowth;        // percent per year
        public float qeBalanceBillions;
        public float credibility = 75f;        // 0-100, hidden
        public ForwardGuidance guidance = ForwardGuidance.Neutral;
        public float inflationExpectations;    // percent per year
        public bool expectationsUnanchored;
        public int weeksAboveUnanchorThreshold;
        public int weeksSinceEmergencyCut = 520;

        /// <summary>What the player is shown instead of the credibility number.</summary>
        public float MarketConfidence { get { return credibility; } }
    }
}
