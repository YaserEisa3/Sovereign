using UnityEngine;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 19: selling what the country has spare, and being held to it.</summary>
    public static partial class EconomyTest
    {
        static void TestTradeContracts(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            runner.Step(Year);

            NationProfile[] nations = runner.Simulator.Geopolitics.Config.nations;

            // Find a country that genuinely needs something this one has spare. That
            // pairing is the whole point - a deal nobody needs is a favour, not a trade.
            int buyer = -1;
            ProductId sell = ProductId.Tourism;
            for (int i = 0; i < nations.Length && buyer < 0; i++)
            {
                if (nations[i].isPlayer) continue;
                foreach (ProductConfig c in runner.Simulator.Production.Products)
                    if (TradeModel.Wants(nations[i], c.id) && TradeModel.Spare(s, c.id) > 1f)
                    {
                        buyer = i; sell = c.id; break;
                    }
            }
            if (!Check(buyer >= 0, "trade: no nation needs anything this country has spare")) return;

            float reservesBefore = s.currency.fxReservesBillions;
            float spare = TradeModel.Spare(s, sell);
            float homeBefore = s.production[sell].produced - s.production[sell].contracted;

            TradeContract deal = TradeModel.Sign(s, nations[buyer], buyer, sell, spare * 0.5f, 5);
            if (!Check(deal != null, "trade: a willing buyer refused a deal for goods going spare")) return;
            Debug.Log("Trade: sold " + deal.volumeBillions.ToString("#,0") + "B of " + sell + " a year to "
                      + deal.nationName + " at " + deal.agreedPrice.ToString("0") + " for "
                      + deal.YearsRemaining.ToString("0") + " years - "
                      + deal.EarningsPerYear.ToString("#,0") + "B a year");

            runner.Step(Year);

            // It must take the goods OUT of the country, or an export is free money.
            Check(s.production[sell].contracted > 0f,
                  "trade: a signed contract reserved nothing - the goods never left");
            Check(s.production[sell].produced - s.production[sell].contracted < homeBefore,
                  "trade: selling abroad left just as much at home, which is a free lunch");

            // And pay for them, in foreign currency.
            Check(s.currency.fxReservesBillions > reservesBefore,
                  "trade: a year of exports earned nothing - reserves went from "
                  + reservesBefore.ToString("0") + " to " + s.currency.fxReservesBillions.ToString("0"));

            // A country that does not need the thing will not buy it.
            ProductId unwanted = ProductId.Tourism;
            foreach (ProductConfig c in runner.Simulator.Production.Products)
                if (!TradeModel.Wants(nations[buyer], c.id)) { unwanted = c.id; break; }
            Check(TradeModel.Sign(s, nations[buyer], buyer, unwanted, 10f, 5) == null,
                  "trade: " + nations[buyer].name + " bought " + unwanted + ", which it does not need");

            // And nothing may be promised that does not exist. Signing away food the
            // country has not grown is refused, not merely warned about.
            Check(TradeModel.Sign(s, nations[buyer], buyer, sell, TradeModel.Spare(s, sell) + 500f, 5) == null,
                  "trade: the country promised " + sell + " it does not have");

            // Breaking one has to cost, or a contract is not a commitment.
            float standing = s.geopolitics.nations[buyer].relationship;
            TradeModel.Break(s, deal);
            Check(s.geopolitics.nations[buyer].relationship < standing - 10f,
                  "trade: tearing up a contract cost " + (standing - s.geopolitics.nations[buyer].relationship).ToString("0")
                  + " points of goodwill - that is not a commitment");
            Check(s.contracts.Count == 0, "trade: a broken contract is still on the books");

            runner.Step(4);
            Check(s.production[sell].contracted <= 0.01f,
                  "trade: a broken contract is still taking goods out of the country");
        }
    }
}
