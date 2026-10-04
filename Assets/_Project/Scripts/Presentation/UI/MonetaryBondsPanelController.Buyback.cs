using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 9. The buyback desk: how much cash to put in, what it buys at today's
    /// price, and what it does to the debt. The price is quoted BEFORE you commit,
    /// because the whole decision is whether the discount is worth the new issuance.
    /// </summary>
    public partial class MonetaryBondsPanelController
    {
        [Header("Buyback - GDD 9")]
        [Tooltip("Step of the buyback amount, in billions.")]
        [SerializeField] float buybackStep = 25f;
        [Tooltip("Most cash one operation can commit, in billions.")]
        [SerializeField] float buybackMax = 1500f;

        Label _buybackReadout;

        void BuildBuyback()
        {
            VisualElement list = Root.Q<VisualElement>("buyback-list");
            if (list != null && policyRowTemplate != null)
            {
                list.Clear();
                // Not a queued scalar: it is an order for the next quarter, and it is
                // spent when it executes rather than held as a standing policy.
                _steppers.Add(new PolicyStepper(policyRowTemplate, list, "Cash committed").Bind(
                    () => Policy.buybackBillions,
                    () => Policy.buybackBillions,
                    v => Policy.buybackBillions = Mathf.Clamp(v, 0f, buybackMax),
                    buybackStep, 0f, buybackMax, Billions,
                    (c, p) => Quote(p)));
            }

            _buybackReadout = Root.Q<Label>("buyback-readout");

            Button execute = Root.Q<Button>("buyback-execute");
            if (execute == null) return;
            execute.clicked += () =>
            {
                // Pressing it with nothing set commits a default parcel, so the button
                // does something on its own rather than looking broken.
                if (Policy.buybackBillions <= 0f) Policy.buybackBillions = buybackStep * 4f;
                Refresh(runner.State);
            };
        }

        /// <summary>What this much cash buys at the price the desk is quoting now.</summary>
        string Quote(float cash)
        {
            if (runner == null || runner.State == null || runner.Simulator == null) return "";
            if (cash <= 0f) return "nothing committed";

            EconomyState state = runner.State;
            EconomySimulator sim = runner.Simulator;
            float price = sim.BuybackPrice(state, cash);
            float face = sim.BuybackFace(state, cash);
            float spent = face * price;
            float net = face - spent;

            // The cash is raised by issuing at today's yields to retire bonds paying the
            // old coupon, so a discount can shrink the principal and RAISE the interest
            // bill at the same time. That is the whole decision, so it is on the row.
            float interest = (spent * state.bonds.yields.mediumTermYield - face * state.bonds.averageCoupon) * 0.01f;

            return (price * 100f).ToString("0") + "c buys $" + face.ToString("0") + "B face, "
                   + (net >= 0f ? "debt down $" + net.ToString("0") + "B" : "debt UP $" + (-net).ToString("0") + "B")
                   + ", interest " + (interest >= 0f ? "+$" : "-$") + Mathf.Abs(interest).ToString("0") + "B/yr";
        }

        void RefreshBuyback(EconomyState state)
        {
            if (_buybackReadout == null) return;

            float duration = runner.Simulator.PortfolioDuration(state);
            string line = "Your debt: $" + state.bonds.debtStockBillions.ToString("0") + "B at "
                          + state.bonds.averageCoupon.ToString("0.00") + "% average coupon, "
                          + duration.ToString("0.0") + "y duration, market " + state.bonds.yields.mediumTermYield.ToString("0.00") + "%.";

            if (state.bonds.lastBuybackWeek >= 0)
                line += "   Last: $" + state.bonds.lastBuybackFace.ToString("0") + "B face for $"
                        + state.bonds.lastBuybackCost.ToString("0") + "B at "
                        + (state.bonds.lastBuybackPrice * 100f).ToString("0") + "c, week " + state.bonds.lastBuybackWeek + ".";
            else if (Policy.buybackBillions > 0f)
                line += "   $" + Policy.buybackBillions.ToString("0") + "B committed - executes at the end of Q" + state.Quarter + ".";

            _buybackReadout.text = line;
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class MonetaryBondsPanelController
    {
        /// <summary>
        /// What the policy rate costs in money rather than in theory: the debt it
        /// prices, and what one step does to the interest bill once the stock has
        /// rolled over at the new level.
        /// </summary>
        string RateBasis(float rate)
        {
            Sovereign.Core.EconomyState state = runner.State;
            if (state == null) return "";

            float stock = state.bonds.debtStockBillions;
            float perStep = stock * rateStep * 0.01f;
            return "prices " + Billions(stock) + " of debt, now costing "
                   + Billions(state.bonds.AnnualDebtServiceBillions) + "/yr;  each step "
                   + SignedBillions(perStep) + "/yr once it has rolled over";
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class MonetaryBondsPanelController
    {
        /// <summary>What QE or QT is doing to the balance sheet, in money a year.</summary>
        string BalanceSheetLine(float perQuarter, float step, bool buying)
        {
            float balance = runner.State.monetary.qeBalanceBillions;
            string holding = "central bank holds " + Billions(balance);
            if (perQuarter <= 0f) return holding + ", unchanged";

            return holding + ", " + (buying ? "+" : "-") + Billions(perQuarter * 4f) + "/yr"
                   + ",  next step " + (buying ? "+" : "-") + Billions(step * 4f) + "/yr";
        }
    }
}
