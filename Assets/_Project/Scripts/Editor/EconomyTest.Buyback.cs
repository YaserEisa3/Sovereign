using UnityEngine;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 9. Buying your own debt back. The arithmetic has to be honest in both
    /// directions: a discount shrinks the debt by more than it costs, a premium
    /// shrinks it by less than it costs, and a huge order gets a worse price than a
    /// small one.
    /// </summary>
    public static partial class EconomyTest
    {
        static void TestBuyback(SimulationRunner runner)
        {
            // Cheap coupons against high market yields: the debt trades at a discount.
            Fresh(runner);
            runner.Policy.QueueScalar("centralBankRate", 12f, runner.Policy.centralBankRate);
            runner.Step(2 * Year);
            EconomyState s = runner.State;

            float price = runner.Simulator.BuybackPrice(s, 200f);
            Check(price < 1f, "buyback: with yields at " + s.bonds.yields.mediumTermYield.ToString("0.0")
                  + "% against a " + s.bonds.averageCoupon.ToString("0.0") + "% coupon, the price quoted was " + price.ToString("0.00"));
            Check(runner.Simulator.BuybackPrice(s, 2000f) > price, "buyback: a ten-times-larger order got the same price");

            float couponBefore = s.bonds.averageCoupon;
            float foreignBefore = s.bonds.foreignHoldingPercent;
            float face = runner.Simulator.BuybackFace(s, 200f);
            runner.Policy.buybackBillions = 200f;
            runner.Step(13);      // the next quarter executes it

            // The deficit adds to the debt every week whatever the desk does, so the
            // buyback is measured against the SAME quarter played without one.
            float debtWithBuyback = s.bonds.debtStockBillions;
            Fresh(runner);
            runner.Policy.QueueScalar("centralBankRate", 12f, runner.Policy.centralBankRate);
            runner.Step(2 * Year + 13);
            float debtWithout = runner.State.bonds.debtStockBillions;

            Check(runner.Policy.buybackBillions == 0f, "buyback: the order was not consumed by the operation");
            Check(s.bonds.lastBuybackFace > 0f, "buyback: the quarter came and went without an operation");
            Check(debtWithBuyback < debtWithout, "buyback: at a discount the debt ended the quarter at $"
                  + debtWithBuyback.ToString("0") + "B, no better than the $" + debtWithout.ToString("0") + "B without one");
            Check(s.bonds.lastBuybackFace > s.bonds.lastBuybackCost,
                  "buyback: $" + s.bonds.lastBuybackCost.ToString("0") + "B retired only $" + s.bonds.lastBuybackFace.ToString("0") + "B of face");
            Check(MathUtil.Abs(s.bonds.lastBuybackFace - face) < face * 0.35f,
                  "buyback: the quote said $" + face.ToString("0") + "B face and the desk retired $" + s.bonds.lastBuybackFace.ToString("0") + "B");
            Check(s.bonds.averageCoupon > couponBefore,
                  "buyback: retiring cheap debt with dear new issuance did not raise the average coupon");
            Check(s.bonds.foreignHoldingPercent < foreignBefore,
                  "buyback: foreign creditors sold nothing - holdings went " + foreignBefore.ToString("0.00") + " to " + s.bonds.foreignHoldingPercent.ToString("0.00"));
            CheckFinite(s, "buyback");

            // The operation cannot swallow the whole debt stock in one quarter.
            float capped = runner.Simulator.BuybackFace(s, s.bonds.debtStockBillions * 5f);
            Check(capped <= s.bonds.debtStockBillions * 0.5f,
                  "buyback: one order could retire $" + capped.ToString("0") + "B against a $" + s.bonds.debtStockBillions.ToString("0") + "B stock");

            // Cheap principal, dear interest: retiring 6% debt by issuing at 15% shrinks
            // the stock and raises the bill. The player has to be able to see both.
            float serviceAfter = s.bonds.AnnualDebtServiceBillions;
            Check(serviceAfter > 0f, "buyback: debt service vanished");
            Debug.Log("Buyback: retired $" + s.bonds.lastBuybackFace.ToString("0") + "B face for $"
                      + s.bonds.lastBuybackCost.ToString("0") + "B at " + (s.bonds.lastBuybackPrice * 100f).ToString("0")
                      + "c; coupon " + couponBefore.ToString("0.00") + "% -> " + s.bonds.averageCoupon.ToString("0.00")
                      + "%, service now $" + serviceAfter.ToString("0") + "B/yr");

            // And it is not free money: with yields BELOW the coupon the debt trades
            // above par, and buying it back costs more than it retires.
            Fresh(runner);
            runner.State.bonds.averageCoupon = runner.State.bonds.yields.mediumTermYield + 4f;
            Check(runner.Simulator.BuybackPrice(runner.State, 200f) > 1f,
                  "buyback: debt paying 4 points above the market still quoted at or below par");
            float premiumStock = runner.State.bonds.debtStockBillions;
            runner.Policy.buybackBillions = 200f;
            runner.Step(13);
            Check(runner.State.bonds.lastBuybackFace < runner.State.bonds.lastBuybackCost,
                  "buyback: paying above par retired more face than it cost");
            Check(runner.State.bonds.debtStockBillions > premiumStock - runner.State.bonds.lastBuybackFace,
                  "buyback: a premium buyback was as good as a discount one");
        }
    }
}
