using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>The scenarios. Each is a claim the GDD makes, run for years and
    /// checked for direction rather than exact magnitude.</summary>
    public static partial class EconomyTest
    {
        /// <summary>Left alone, the economy should sit near trend rather than explode.</summary>
        static void TestBaselineStability(SimulationRunner runner)
        {
            EconomyState state = Fresh(runner).State;
            runner.Step(10 * Year);

            CheckFinite(state, "baseline");
            Check(state.realGdpGrowth > -6f && state.realGdpGrowth < 8f,
                  "baseline: growth left a sane band after ten years (" + state.realGdpGrowth.ToString("0.0") + "%)");
            Check(state.unemployment > 1f && state.unemployment < 15f,
                  "baseline: unemployment left a sane band (" + state.unemployment.ToString("0.0") + "%)");
            Check(state.inflation > -4f && state.inflation < 15f,
                  "baseline: inflation left a sane band (" + state.inflation.ToString("0.0") + "%)");
            Check(state.bonds.yields.longTermYield > 0f && state.bonds.yields.longTermYield < 30f,
                  "baseline: long yield left a sane band (" + state.bonds.yields.longTermYield.ToString("0.0") + "%)");
        }

        /// <summary>
        /// GDD 12 and 21. Rates high enough for long enough must cost output and
        /// employment before they bring inflation down, and the cost must arrive
        /// after the hike rather than with it.
        /// </summary>
        static void TestVolckerMoment(SimulationRunner runner)
        {
            EconomyState loose = Fresh(runner).State;
            runner.Policy.centralBankRate = 2f;
            runner.Step(6 * Year);
            float looseInflation = loose.inflation;
            float looseUnemployment = loose.unemployment;

            EconomyState tight = Fresh(runner).State;
            runner.Policy.centralBankRate = 11f;
            runner.Step(2 * Year);
            float unemploymentAfterTwoYears = tight.unemployment;
            runner.Step(4 * Year);

            CheckFinite(tight, "volcker");
            Check(tight.inflation < looseInflation,
                  "volcker: tight money did not lower inflation (tight " + tight.inflation.ToString("0.0")
                  + "% vs loose " + looseInflation.ToString("0.0") + "%)");
            Check(tight.unemployment > looseUnemployment,
                  "volcker: the disinflation was free - unemployment did not rise (tight "
                  + tight.unemployment.ToString("0.0") + "% vs loose " + looseUnemployment.ToString("0.0") + "%)");
            Check(unemploymentAfterTwoYears > runner.Simulator.Config.startingUnemployment,
                  "volcker: two years of an 11% policy rate left unemployment untouched - the rate channel is dead");
        }

        /// <summary>
        /// GDD 12. Sustained high inflation unanchors expectations, and once
        /// unanchored the pass-through into wages jumps.
        /// </summary>
        static void TestInflationTrap(SimulationRunner runner)
        {
            EconomyState state = Fresh(runner).State;
            runner.Policy.centralBankRate = 0.25f;
            runner.Policy.qeAmountPerQuarter = 400f;

            for (int week = 0; week < 8 * Year; week++)
            {
                runner.Step();
                runner.Policy.qeAmountPerQuarter = 400f;
            }

            CheckFinite(state, "inflation trap");
            Check(state.inflation > runner.Simulator.Config.startingInflation,
                  "inflation trap: zero rates and heavy QE did not raise inflation");
            Check(state.monetary.expectationsUnanchored,
                  "inflation trap: expectations never unanchored after eight years of it");
            Check(state.monetary.credibility < runner.Simulator.Config.macro.startingCredibility,
                  "inflation trap: credibility survived monetising the deficit");
        }
    }
}
