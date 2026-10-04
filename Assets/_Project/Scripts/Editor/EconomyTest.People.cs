using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 7: people, workers, wages.</summary>
    public static partial class EconomyTest
    {
        static void TestPopulation(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            PopulationState p = s.population;
            float startTotal = p.Total, startDependency = p.DependencyRatio, startVeterans = p.veterans;
            runner.Step(10 * Year);

            float annualGrowth = (MathUtil.Pow(p.Total / startTotal, 0.1f) - 1f) * 100f;
            Check(annualGrowth > 0.1f && annualGrowth < 1.2f,
                  "population: grew " + annualGrowth.ToString("0.00") + "% a year - outside a plausible band");
            Check(p.birthsPerYear > 0f && p.deathsPerYear > 0f, "population: no births or no deaths");
            Check(p.DependencyRatio > startDependency, "population: the society did not age over a decade");
            Check(p.veterans < startVeterans, "population: veterans grew with no war to feed them");

            // Employment adds up. Wages move at the quarter boundary after the week's pool
            // is computed, so step one ordinary week to measure both at the same moment.
            runner.Step(1);
            Check(MathUtil.Abs(p.employed - p.laborForce * (1f - s.unemployment * 0.01f)) < 0.01f,
                  "employment: employed is not labour force times the employment rate");
            float headcount = 0f, pool = 0f;
            foreach (EconomicSector sector in s.sectors) { headcount += sector.headcount; pool += sector.WageBillBillions; }
            Check(MathUtil.Abs(headcount - p.employed) < 0.01f, "employment: sector headcounts do not sum to the employed");
            Check(MathUtil.Abs(pool - p.taxableIncomePool) < 0.5f, "employment: the taxable income pool is not the sum of wage bills");
        }

        /// <summary>
        /// Regression tests for the two wage bugs Phase 2 exposed: real wages fell every
        /// year forever, and stickiness set each sector's GROWTH rather than its lag.
        /// </summary>
        static void TestWagesKeepUp(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            float startReal = s.Series("realWage").Latest;
            float[] startWages = new float[s.sectors.Count];
            for (int i = 0; i < s.sectors.Count; i++) startWages[i] = s.sectors[i].averageWage;

            runner.Step(10 * Year);

            Check(s.Series("realWage").Latest > startReal,
                  "wages: real wages fell over a calm decade - wages are not keeping up with prices");

            float fastest = 0f, slowest = float.MaxValue;
            for (int i = 0; i < s.sectors.Count; i++)
            {
                float growth = s.sectors[i].averageWage / startWages[i];
                if (growth > fastest) fastest = growth;
                if (growth < slowest) slowest = growth;
            }
            Check(fastest / slowest < 1.15f,
                  "wages: sectors diverged " + ((fastest / slowest - 1f) * 100f).ToString("0")
                  + "% in a calm decade - stickiness is changing growth, not just the lag");

            float taxBaseGrowth = s.population.taxableIncomePool;
            Check(taxBaseGrowth / s.nominalGdpBillions > 0.8f * (9442f / 27000f),
                  "wages: the income tax base shrank against GDP");
        }

        static void TestChildCredit(SimulationRunner runner)
        {
            EconomyState none = Fresh(runner).State;
            runner.Policy.taxRates[TaxKeys.ChildCredit] = 0f;
            runner.Step(10 * Year);
            float youthWithout = none.population.youth;

            EconomyState generous = Fresh(runner).State;
            runner.Policy.taxRates[TaxKeys.ChildCredit] = 5000f;
            runner.Step(10 * Year);

            Check(generous.population.youth > youthWithout,
                  "child credit: $5,000 a child for ten years produced no more children");
            Check(generous.population.youth - youthWithout < youthWithout * 0.05f,
                  "child credit: it moved births by more than 5% - GDD 22.2 says this lever is deliberately weak");
            Check(generous.treasury.revenueByLine[TaxKeys.ChildCredit] < -100f,
                  "child credit: a $5,000 credit cost less than $100B a year");
        }

        static void TestImmigration(SimulationRunner runner)
        {
            EconomyState closed = Fresh(runner).State;
            runner.Policy.immigrationInflowMillions = 0f;
            runner.Step(8 * Year);
            float closedWorkers = closed.population.laborForce;
            float closedGdp = closed.realGdpIndex;
            float closedServicesWage = closed.GetSector(SectorId.ServicesRetail).averageWage;
            float closedRight = closed.approval.right;

            EconomyState open = Fresh(runner).State;
            runner.Policy.immigrationInflowMillions = 3f;
            runner.Step(8 * Year);

            Check(open.population.laborForce > closedWorkers + 5f, "immigration: 3 million a year did not grow the labour force");
            Check(open.realGdpIndex > closedGdp, "immigration: a larger workforce produced no more output");
            // GDD 22.1: it must cost something, or it is a free GDP button.
            Check(open.GetSector(SectorId.ServicesRetail).averageWage < closedServicesWage,
                  "immigration: no downward pressure on Services wages - it is a free lunch");
            Check(open.approval.right < closedRight, "immigration: the Conservative faction did not object");
        }
    }
}
