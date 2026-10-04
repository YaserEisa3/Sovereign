using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>One thing a group of people is judging you on, and how you are doing at it.</summary>
    public struct ApprovalDriver
    {
        /// <summary>What they care about, with the current reading in it: "Inflation 9.4%".</summary>
        public string label;
        /// <summary>How much this weighs in their overall view of you.</summary>
        public float weight;
        /// <summary>0-100. Below 50 is dragging them down.</summary>
        public float score;

        /// <summary>How much of their unhappiness this one thing accounts for.</summary>
        public float Drag { get { return weight * (100f - score); } }
    }

    public partial class ApprovalModel
    {
        /// <summary>
        /// GDD 14. Why each group feels the way it does. These are the SAME components
        /// the approval score is built from - the score weighs this list rather than
        /// computing its own copy, so the explanation can never disagree with the bar
        /// it is explaining.
        /// </summary>
        public List<ApprovalDriver>[] Components(EconomyState s, PolicyState p)
        {
            ApprovalState a = s.approval;

            float growth = s.Series("gdpGrowth").Average(13);
            float inflation = s.Series("inflation").Average(13);
            float unemployment = s.unemployment;

            float unemploymentScore = Score(100f - (unemployment - 3f) * 8f);
            float inflationScore = Score(100f - MathUtil.Abs(inflation - 2f) * 9f);
            float growthScore = Score(50f + (growth - 2f) * 12f);

            TimeSeries realWageSeries = s.Series("realWage");
            float realWageGrowth = (realWageSeries.Latest / MathUtil.Max(1f, realWageSeries.Ago(52)) - 1f) * 100f;
            float realHousing = s.priceLevel <= 0f ? 0f : s.housingCostIndex / s.priceLevel * 100f - 100f;
            float regulation = (p.environmentalRegulation + p.labourRegulation) * 0.5f + (p.bankingCapitalRequirement - 10f) * 2f;

            float welfare = Ratio(Welfare(p), a.baselineWelfare);
            float health = Ratio(Health(p), a.baselineHealth);
            float education = Ratio(Education(p), a.baselineEducation);
            float social = Ratio(Welfare(p) + Health(p), a.baselineSocial);
            float defence = Ratio(Defence(p), a.baselineDefence);

            float corporate = p.Tax(TaxKeys.CorporateIncome);
            float gains = p.Tax(TaxKeys.CapitalGains);
            float averageTop = (p.Tax(TaxKeys.IncomeTop) + corporate + gains) / 3f;

            List<ApprovalDriver>[] groups = new List<ApprovalDriver>[6];

            groups[0] = new List<ApprovalDriver>
            {
                Driver("Unemployment " + unemployment.ToString("0.0") + "%", _c.poorUnemployment, unemploymentScore),
                Driver("Inflation " + inflation.ToString("0.0") + "%", _c.poorInflation, inflationScore),
                Driver("Welfare spending " + Versus(welfare), _c.poorWelfare, Score(50f + (welfare - 1f) * 120f)),
                Driver("Healthcare spending " + Versus(health), _c.poorHealthcare, Score(50f + (health - 1f) * 120f)),
                Driver("Labour protections " + p.labourRegulation.ToString("0"), _c.poorMinimumWage, Score(50f + (p.labourRegulation - 40f) * 0.8f)),
            };

            groups[1] = new List<ApprovalDriver>
            {
                Driver("Real wages " + realWageGrowth.ToString("+0.0;-0.0") + "% this year", _c.middleRealWages, Score(50f + realWageGrowth * 10f)),
                Driver("Unemployment " + unemployment.ToString("0.0") + "%", _c.middleEmployment, unemploymentScore),
                Driver("Housing costs " + realHousing.ToString("+0;-0") + "% vs prices", _c.middleHousingCosts, Score(50f - realHousing * 3f)),
                Driver("Their income tax " + p.Tax(TaxKeys.IncomeMiddle).ToString("0") + "%", _c.middleIncomeTax, Score(100f - (p.Tax(TaxKeys.IncomeMiddle) - 10f) * 4f)),
                Driver("Education spending " + Versus(education), _c.middleEducation, Score(50f + (education - 1f) * 120f)),
                Driver("Consumer confidence " + s.consumerConfidence.ToString("0"), _c.middleConfidence, Score(s.consumerConfidence)),
            };

            groups[2] = new List<ApprovalDriver>
            {
                Driver("Corporate tax " + corporate.ToString("0") + "%", _c.wealthyCorporateTax, Score(100f - (corporate - 10f) * 3.5f)),
                Driver("Capital gains tax " + gains.ToString("0") + "%", _c.wealthyCapitalGains, Score(100f - (gains - 5f) * 3.5f)),
                Driver("Growth " + growth.ToString("0.0") + "%", _c.wealthyGrowth, growthScore),
                Driver("Debt " + (s.DebtToGdp * 100f).ToString("0") + "% of GDP", _c.wealthyDebtFiscal, Score(100f - (s.DebtToGdp - 0.6f) * 80f)),
                Driver("Regulation " + regulation.ToString("0"), _c.wealthyRegulation, Score(100f - (regulation - 20f))),
                Driver("Stability", _c.wealthyStability, Score(80f - MathUtil.Abs(growth - 2f) * 6f - MathUtil.Max(0f, inflation - 4f) * 3f)),
            };

            groups[3] = new List<ApprovalDriver>
            {
                Driver("Inequality, gini " + s.giniCoefficient.ToString("0.00"), _c.leftGini, Score(100f - (s.giniCoefficient - 0.30f) * 400f)),
                Driver("Green policy", _c.leftGreenPolicy, Score(30f + p.Tax(TaxKeys.Carbon) * 0.3f + (p.environmentalRegulation - 40f) * 0.8f)),
                Driver("Social spending " + Versus(social), _c.leftSocialSpending, Score(50f + (social - 1f) * 120f)),
                Driver("Corporate tax " + corporate.ToString("0") + "%", _c.leftCorporateTax, Score(30f + (corporate - 10f) * 2.5f)),
                Driver("Healthcare spending " + Versus(health), _c.leftHealthcare, Score(50f + (health - 1f) * 120f)),
            };

            groups[4] = new List<ApprovalDriver>
            {
                Driver("Growth " + growth.ToString("0.0") + "%", _c.centreGrowth, growthScore),
                Driver("Inflation " + inflation.ToString("0.0") + "%", _c.centreInflation, inflationScore),
                Driver("Unemployment " + unemployment.ToString("0.0") + "%", _c.centreUnemployment, unemploymentScore),
                Driver("Budget balance " + s.budgetBalancePercentGdp.ToString("+0.0;-0.0") + "% of GDP", _c.centreBudgetBalance, Score(70f + s.budgetBalancePercentGdp * 8f)),
            };

            groups[5] = new List<ApprovalDriver>
            {
                Driver("Debt " + (s.DebtToGdp * 100f).ToString("0") + "% of GDP", _c.rightDebtLevel, Score(100f - (s.DebtToGdp - 0.6f) * 80f)),
                Driver("Tax rates averaging " + averageTop.ToString("0") + "%", _c.rightTaxRates, Score(100f - (averageTop - 15f) * 3f)),
                Driver("Regulation " + regulation.ToString("0"), _c.rightDeregulation, Score(100f - (regulation - 20f) * 1.2f)),
                Driver("Defence spending " + Versus(defence), _c.rightDefence, Score(50f + (defence - 1f) * 120f)),
                Driver("Growth " + growth.ToString("0.0") + "%", _c.rightGrowth, growthScore),
            };

            return groups;
        }

        /// <summary>What is dragging this group down, worst first.</summary>
        public List<ApprovalDriver> Drivers(EconomyState s, PolicyState p, int group)
        {
            List<ApprovalDriver>[] all = Components(s, p);
            if (group < 0 || group >= all.Length) return new List<ApprovalDriver>();

            List<ApprovalDriver> drivers = all[group];
            drivers.Sort((a, b) => b.Drag.CompareTo(a.Drag));
            return drivers;
        }

        static ApprovalDriver Driver(string label, float weight, float score)
        {
            return new ApprovalDriver { label = label, weight = weight, score = score };
        }

        /// <summary>"12% below its opening level" - spending is judged against where it started.</summary>
        static string Versus(float ratio)
        {
            float percent = (ratio - 1f) * 100f;
            if (MathUtil.Abs(percent) < 1f) return "at its opening level";
            return MathUtil.Abs(percent).ToString("0") + "% " + (percent > 0f ? "above" : "below") + " opening";
        }

        static float Weigh(List<ApprovalDriver> drivers)
        {
            float total = 0f, weight = 0f;
            foreach (ApprovalDriver driver in drivers)
            {
                total += driver.weight * driver.score;
                weight += driver.weight;
            }
            return weight <= 0f ? 50f : total / weight;
        }
    }
}
