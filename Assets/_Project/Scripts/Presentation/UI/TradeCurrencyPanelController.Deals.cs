using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 19. The deals themselves: who wants what this country has spare, at what
    /// price, and for how long.
    ///
    /// Rebuilt every week rather than bound once, because what is on offer changes with
    /// the harvest. A row appears when a country needs something there is a surplus of
    /// and disappears when the surplus does - which is the honest way to show that a
    /// trading position is a thing you have to maintain.
    /// </summary>
    public partial class TradeCurrencyPanelController
    {
        [Tooltip("Years a new contract runs for. Two to ten; five is a term both sides can plan around.")]
        [SerializeField] int contractTermYears = 5;
        [Tooltip("Share of the available surplus a single deal commits. Leaving room is the point - sell every last tonne and the first bad harvest is a crisis.")]
        [Range(0.1f, 1f)] [SerializeField] float dealShareOfSurplus = 0.6f;

        void RefreshDeals(EconomyState state)
        {
            VisualElement offers = Clear("deal-list");
            VisualElement signed = Clear("contract-list");
            if (offers == null || signed == null || runner.Simulator == null) return;

            NationProfile[] nations = runner.Simulator.Geopolitics.Config.nations;
            ProductConfig[] products = runner.Simulator.Production.Products;

            int available = 0;
            for (int n = 0; n < nations.Length; n++)
            {
                if (nations[n].isPlayer) continue;

                for (int p = 0; p < products.Length; p++)
                {
                    ProductId id = products[p].id;
                    if (!TradeModel.Wants(nations[n], id)) continue;

                    float spare = TradeModel.Spare(state, id);
                    if (spare < 1f) continue;        // nothing to sell them

                    available++;
                    AddOffer(offers, state, nations[n], n, products[p], spare);
                }
            }

            SetLabel("deal-summary", available == 0
                ? "Nothing to sell - every country that wants your goods wants what you are short of"
                : available + (available == 1 ? " country wants something" : " offers from countries that need what you have"));

            float earning = 0f;
            foreach (TradeContract contract in state.contracts) earning += contract.EarningsPerYear;
            SetLabel("contract-summary", state.contracts.Count == 0
                ? "No contracts signed"
                : state.contracts.Count + (state.contracts.Count == 1 ? " contract earning " : " contracts earning ")
                  + Billions(earning) + " a year into the reserves");

            foreach (TradeContract contract in state.contracts) AddContract(signed, contract);
        }

        void AddOffer(VisualElement list, EconomyState state, NationProfile nation, int index,
                      ProductConfig product, float spare)
        {
            float price = TradeModel.OfferedPrice(state, nation, index, product.id);
            float volume = spare * dealShareOfSurplus;

            VisualElement row = new VisualElement();
            row.AddToClassList("deal-row");

            row.Add(Labelled2(nation.name + " wants " + product.name, "deal-name"));
            row.Add(Labelled2(Billions(volume) + "/yr at " + price.ToString("0")
                              + "  =  " + Billions(volume * price * 0.01f) + "/yr", "deal-terms"));

            Button sign = new Button { text = "SIGN " + contractTermYears + "y" };
            sign.AddToClassList("deal-button");
            sign.clicked += () =>
            {
                TradeModel.Sign(runner.State, nation, index, product.id,
                                TradeModel.Spare(runner.State, product.id) * dealShareOfSurplus,
                                contractTermYears);
                RefreshDeals(runner.State);
            };
            row.Add(sign);
            list.Add(row);
        }

        void AddContract(VisualElement list, TradeContract contract)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("deal-row");

            row.Add(Labelled2(contract.nationName + " - " + contract.product, "deal-name"));
            row.Add(Labelled2(Billions(contract.volumeBillions) + "/yr at " + contract.agreedPrice.ToString("0")
                              + ",  " + contract.YearsRemaining.ToString("0.0") + "y left", "deal-terms"));

            Button breakIt = new Button { text = "BREAK" };
            breakIt.AddToClassList("deal-button");
            breakIt.AddToClassList("danger");
            breakIt.clicked += () =>
            {
                TradeModel.Break(runner.State, contract);
                RefreshDeals(runner.State);
            };
            row.Add(breakIt);
            list.Add(row);
        }

        static Label Labelled2(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
    }
}
