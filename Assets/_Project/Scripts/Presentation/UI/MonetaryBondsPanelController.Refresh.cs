using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>The weekly refresh for the monetary drawer.</summary>
    public partial class MonetaryBondsPanelController
    {
        protected override void Refresh(EconomyState state)
        {
            foreach (PolicyStepper stepper in _steppers) stepper.Refresh();

            YieldCurve curve = state.bonds.yields;
            if (_curve != null)
            {
                _curve.SetValues(new[]
                {
                    curve.shortTermYield,
                    Mathf.Lerp(curve.shortTermYield, curve.mediumTermYield, 0.25f),
                    curve.mediumTermYield,
                    Mathf.Lerp(curve.mediumTermYield, curve.longTermYield, 0.5f),
                    curve.longTermYield
                }, 5);
                if (theme != null) _curve.lineColor = curve.IsInverted ? theme.danger : theme.neutralData;
            }

            SetLabel("inversion-flag", curve.IsInverted
                ? "INVERTED - short " + curve.shortTermYield.ToString("0.00") + " above long " + curve.longTermYield.ToString("0.00")
                : "3M " + curve.shortTermYield.ToString("0.00") + "   5Y " + curve.mediumTermYield.ToString("0.00")
                  + "   30Y " + curve.longTermYield.ToString("0.00"));

            SetLabel("credibility-readout", "Market confidence " + state.monetary.MarketConfidence.ToString("0")
                     + "   guidance " + Policy.guidance
                     + (_emergencyNote.Length > 0 ? "   " + _emergencyNote : ""));

            RefreshBuyback(state);

            SetLabel("currency-mismatch-warning", Policy.issueInForeignCurrency
                ? "Foreign-currency debt " + (state.bonds.foreignCurrencyDebtPercent * 100f).ToString("0")
                  + "% - cheap until your currency falls, then it is not."
                : "");

            int weeksToAuction = 13 - state.week % 13;
            SetLabel("next-auction", "Next auction in " + weeksToAuction + " weeks   last cover "
                     + state.bonds.lastAuctionCover.ToString("0.00") + "x   issuing " + Policy.maturityPreference);

            RefreshMaturity(state);
            RefreshHolders(state);
        }

        void RefreshMaturity(EconomyState state)
        {
            float[] shares = { state.bonds.maturingWithinOneYear, state.bonds.maturingOneToFive, state.bonds.maturingBeyondFive };
            for (int i = 0; i < _maturityRows.Count && i < shares.Length; i++)
            {
                VisualElement row = _maturityRows[i];
                VisualElement fill = row.Q<VisualElement>("maturity-fill");
                if (fill != null) fill.style.width = Length.Percent(shares[i] * 100f);

                SetRowLabel(row, "maturity-amount", Billions(state.bonds.debtStockBillions * shares[i]));
                SetRowLabel(row, "maturity-share", (shares[i] * 100f).ToString("0") + "%");
                SetRowLabel(row, "maturity-yield", "");
                SetRowLabel(row, "maturity-rollover-flag", i == 0 && shares[i] > 0.3f ? "ROLLOVER RISK" : "");
            }
        }

        void RefreshHolders(EconomyState state)
        {
            float scale = _startingForeignTotal <= 0f ? 0f : state.bonds.foreignHoldingPercent / _startingForeignTotal;
            for (int i = 0; i < _holderRows.Count; i++)
            {
                float share = _holders[i].bondHoldingPercent * scale;
                SetRowLabel(_holderRows[i], "nation-bond-holding", (share * 100f).ToString("0.0") + "% of debt");
                SetRowLabel(_holderRows[i], "nation-trade", Billions(state.bonds.debtStockBillions * share));
                SetRowLabel(_holderRows[i], "nation-relationship", "");
                SetRowLabel(_holderRows[i], "nation-archetype", "");
            }
        }
    }
}
