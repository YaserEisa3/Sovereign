using System.Collections.Generic;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 14: approval, unrest, revolt.</summary>
    public static partial class EconomyTest
    {
        static void TestApproval(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            ApprovalConfig c = runner.Simulator.Approval.Config;
            Check(MathUtil.Abs(s.approval.poor - c.startPoor) < 0.01f && MathUtil.Abs(s.approval.middle - c.startMiddle) < 0.01f
                  && MathUtil.Abs(s.approval.right - c.startRight) < 0.01f,
                  "approval: the opening approvals are not the designer's starting values");

            // The preview must look without touching: no policy, approval or inequality change.
            float corporateBefore = runner.Policy.Tax(TaxKeys.CorporateIncome), giniBefore = s.giniCoefficient;
            float[] shift = runner.Simulator.Approval.PreviewTax(s, runner.Policy, TaxKeys.CorporateIncome, 40f);
            Check(runner.Policy.Tax(TaxKeys.CorporateIncome) == corporateBefore && s.giniCoefficient == giniBefore,
                  "approval: previewing a change altered the live state");
            Check(shift[2] < -1f && shift[3] > 0f,
                  "approval: previewing a 40% corporate tax did not show the wealthy objecting and the Left approving");

            // Each class answers to its own conditions. Compare against an untouched control.
            EconomyState control = Fresh(runner).State;
            runner.Step(3 * Year);
            float controlMiddle = control.approval.middle, controlWealthy = control.approval.wealthy;
            float controlOverall = control.approval.overall;

            EconomyState taxedMiddle = Fresh(runner).State;
            runner.Policy.taxRates[TaxKeys.IncomeMiddle] = 40f;
            runner.Step(3 * Year);
            Check(taxedMiddle.approval.middle < controlMiddle - 3f,
                  "approval: a 16-point middle income tax rise barely moved the middle class");

            EconomyState cutCorporate = Fresh(runner).State;
            runner.Policy.taxRates[TaxKeys.CorporateIncome] = 10f;
            runner.Step(3 * Year);
            Check(cutCorporate.approval.wealthy > controlWealthy + 2f,
                  "approval: cutting corporate tax to 10% did not please the wealthy");
            Check(cutCorporate.approval.left < control.approval.left,
                  "approval: the Left did not object to a corporate tax cut");

            EconomyState recession = Fresh(runner).State;
            runner.Policy.centralBankRate = 11f;
            runner.Step(3 * Year);
            Check(recession.approval.overall < controlOverall - 3f,
                  "approval: a recession cost the government nothing (" + recession.approval.overall.ToString("0")
                  + " vs " + controlOverall.ToString("0") + ")");
        }

        /// <summary>A run has to be losable, and the quiet one must not end by itself.</summary>
        static void TestRevolt(SimulationRunner runner)
        {
            EconomyState calm = FreshWithPolitics(runner).State;
            runner.Step(10 * Year);
            Check(!calm.IsGameOver, "revolt: the country revolted with no policy change - " + calm.approval.revoltReason);

            // Everything the poor and the middle care about, broken at once.
            EconomyState ruined = FreshWithPolitics(runner).State;
            runner.Policy.centralBankRate = 12f;
            runner.Policy.taxRates[TaxKeys.IncomeMiddle] = 50f;
            runner.Policy.taxRates[TaxKeys.IncomeLower] = 30f;
            runner.Policy.taxRates[TaxKeys.ValueAdded] = 25f;
            List<string> keys = new List<string>(runner.Policy.spendingBillions.Keys);
            foreach (string key in keys)
                if (key.StartsWith("Medic") || key.Contains("Assistance") || key.Contains("Unemployment")
                    || key.StartsWith("SocialSecurity") || key.StartsWith("Education"))
                    runner.Policy.spendingBillions[key] = 0f;

            runner.Step(10 * Year);
            Check(ruined.IsGameOver, "revolt: ten years of ruin never ended the run (overall "
                  + ruined.approval.overall.ToString("0") + "%, unrest " + ruined.approval.HighestUnrest.ToString("0") + ")");
            Check(ruined.approval.revoltReason.Length > 0, "revolt: the run ended without saying why");

            int weekAtRevolt = ruined.week;
            runner.Step(Year);
            Check(ruined.week == weekAtRevolt, "revolt: the clock kept running after the government fell");
        }
    }
}
