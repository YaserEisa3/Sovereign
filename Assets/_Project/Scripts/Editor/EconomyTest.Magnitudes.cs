using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Magnitude tests. The direction tests all passed while the baseline quietly
    /// paid off a third of the national debt, the currency slid 34% with nobody
    /// touching it, and a six-year depression peaked at 7% unemployment. Directions
    /// were right; the numbers were nonsense. These check the numbers.
    /// </summary>
    public static partial class EconomyTest
    {
        /// <summary>Nothing should drift on its own. A player who touches no control
        /// should find the economy roughly where they left it.</summary>
        static void TestNoFreeDrift(SimulationRunner runner)
        {
            EconomyState state = Fresh(runner).State;
            float startingDebt = state.DebtToGdp;
            CreditRating startingRating = state.bonds.creditRating;
            runner.Step(10 * Year);

            float debtDrift = MathUtil.Abs(state.DebtToGdp - startingDebt) * 100f;
            Check(debtDrift < 25f,
                  "drift: debt/GDP moved " + debtDrift.ToString("0") + "pp in ten years with no policy change - "
                  + "the budget is drifting on its own (now " + (state.DebtToGdp * 100f).ToString("0") + "%)");

            float currencyDrift = MathUtil.Abs(state.currency.exchangeRateIndex - 100f);
            Check(currencyDrift < 15f,
                  "drift: the currency moved " + currencyDrift.ToString("0") + " points with no policy change (now "
                  + state.currency.exchangeRateIndex.ToString("0") + ")");

            Check(state.bonds.creditRating <= startingRating,
                  "drift: the rating fell from " + startingRating + " to " + state.bonds.creditRating
                  + " while the player did nothing");

            Check(MathUtil.Abs(state.inflation - runner.Simulator.Config.startingInflation) < 4f,
                  "drift: inflation wandered to " + state.inflation.ToString("0.0") + "% on its own");
        }

        /// <summary>
        /// GDD 12: breaking an entrenched inflation requires a real recession. A
        /// disinflation that costs 2 points of unemployment is not a Volcker moment,
        /// it is a rounding error.
        /// </summary>
        static void TestRecessionIsSevereEnough(SimulationRunner runner)
        {
            EconomyState state = Fresh(runner).State;
            runner.Policy.centralBankRate = 11f;
            runner.Step(5 * Year);

            Check(state.unemployment > 8f,
                  "severity: five years at an 11% policy rate only reached "
                  + state.unemployment.ToString("0.0") + "% unemployment - the recession has no teeth");
            Check(state.unemployment < 25f,
                  "severity: unemployment reached an implausible " + state.unemployment.ToString("0.0") + "%");
        }

        /// <summary>
        /// Deflation must not feed on itself forever. Rigid wages and a floor under
        /// expectations are what stop the model diving to minus infinity.
        /// </summary>
        static void TestNoDeflationSpiral(SimulationRunner runner)
        {
            // Severe but plausible: nine percent, held far too long.
            EconomyState state = Fresh(runner).State;
            runner.Policy.centralBankRate = 9f;
            runner.Step(10 * Year);

            CheckFinite(state, "deflation");
            Check(state.inflation > -4f,
                  "deflation: prices fell " + state.inflation.ToString("0.0") + "% a year and kept accelerating");
            // A decade at 9% SHOULD produce a debt crisis - GDD 17.6 treats that as a
            // real fail state, not a bug. What is checked is that it stays bounded.
            // Ceiling raised from 3.2 to 5.5 when the rate channel was deliberately
            // strengthened and its lag halved, so the player could feel a rate decision:
            // the same decade now ends at 4.8x rather than 3.1x. What this test guards -
            // finiteness, and deflation that does not accelerate away - still holds, and
            // both of those are asserted above. The ratio rises because nominal GDP
            // shrinks while deficits accumulate, which is arithmetic, not a runaway.
            Check(state.DebtToGdp < 5.5f,
                  "deflation: debt/GDP ran to " + (state.DebtToGdp * 100f).ToString("0")
                  + "% under sustained tight money - past a crisis and into a runaway");

            // And the extreme case must stay finite rather than exploding numerically.
            // A decade at 14% SHOULD ruin the country; it should not produce a NaN.
            EconomyState extreme = Fresh(runner).State;
            runner.Policy.centralBankRate = 14f;
            runner.Step(10 * Year);
            CheckFinite(extreme, "deflation extreme");
            Check(extreme.DebtToGdp < 12f,
                  "deflation extreme: debt/GDP ran to " + (extreme.DebtToGdp * 100f).ToString("0") + "%");
        }

        /// <summary>A recovery has to be possible. Tighten, then relent, and the
        /// economy should come back - otherwise every mistake is terminal.</summary>
        static void TestRecoveryIsPossible(SimulationRunner runner)
        {
            EconomyState state = Fresh(runner).State;
            runner.Policy.centralBankRate = 11f;
            runner.Step(4 * Year);
            float worstUnemployment = state.unemployment;

            runner.Policy.centralBankRate = 3f;
            runner.Step(6 * Year);

            Check(state.unemployment < worstUnemployment - 1f,
                  "recovery: cutting rates for six years left unemployment at "
                  + state.unemployment.ToString("0.0") + "% against a peak of " + worstUnemployment.ToString("0.0") + "%");
            Check(state.realGdpGrowth > 0f,
                  "recovery: growth never returned after the rate cut (" + state.realGdpGrowth.ToString("0.0") + "%)");
        }
    }
}
