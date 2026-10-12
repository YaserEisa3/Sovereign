using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 19. What the country makes, in the drawer where it will be traded.
    ///
    /// This first lived as a tab beside the charts, which was the wrong place: what a
    /// country has spare is the opening line of every trade negotiation, and reading it
    /// meant leaving the screen where tariffs and agreements are set. Scarcest first,
    /// because the shortage is what a government acts on.
    /// </summary>
    public partial class TradeCurrencyPanelController
    {
        class ProductLine
        {
            public Label name, made, balance, limit, note;
        }

        readonly List<ProductLine> _productLines = new List<ProductLine>();
        readonly List<ProductConfig> _sortedProducts = new List<ProductConfig>();

        void BuildProductionList()
        {
            _productLines.Clear();
            VisualElement list = Clear("production-list");
            if (list == null) return;

            // Counted from the enum, not from the live model: a drawer builds its rows
            // before the simulator has necessarily booted, and asking the model how many
            // products exist answered zero - a table that looked perfect and held nothing.
            int count = System.Enum.GetValues(typeof(ProductId)).Length;
            for (int i = 0; i < count; i++)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("product-row");

                ProductLine line = new ProductLine
                {
                    name = Labelled("product-name"),
                    made = Labelled("product-made"),
                    balance = Labelled("product-balance"),
                    limit = Labelled("product-limit"),
                    note = Labelled("product-note")
                };
                row.Add(line.name);
                row.Add(line.made);
                row.Add(line.balance);
                row.Add(line.limit);
                row.Add(line.note);

                _productLines.Add(line);
                list.Add(row);
            }
        }

        static Label Labelled(string className)
        {
            Label label = new Label("");
            label.AddToClassList(className);
            return label;
        }

        void RefreshProductionList(EconomyState state)
        {
            if (_productLines.Count == 0 || runner == null || runner.Simulator == null) return;

            _sortedProducts.Clear();
            _sortedProducts.AddRange(runner.Simulator.Production.Products);
            // By COVER, not by price. Prices all start at 100 and crawl toward their
            // target at 4% a week, so in the first months the price order is noise - the
            // list opened on a run calling grain scarcer than oil, with grain at 75% of
            // what the country needs and oil at 24%. Cover is true from week one.
            _sortedProducts.Sort((a, b) => Cover(state, a.id).CompareTo(Cover(state, b.id)));

            int short_ = 0;
            float spare = 0f;

            for (int i = 0; i < _productLines.Count && i < _sortedProducts.Count; i++)
            {
                ProductConfig c = _sortedProducts[i];
                ProductLine line = _productLines[i];
                ProductState p;
                if (!state.production.TryGetValue(c.id, out p)) continue;

                if (p.Balance < 0f) short_++; else spare += p.Balance;

                line.name.text = c.name;
                line.made.text = "$" + p.produced.ToString("#,0") + "B of $" + p.needed.ToString("#,0") + "B";
                line.balance.text = p.Balance.ToString("+#,0;-#,0") + "B";
                // What the ground allows, which no budget changes.
                line.limit.text = c.capability >= 0.999f ? "" : (c.capability * 100f).ToString("0") + "% max";

                // And whether this is worth fixing or has to be lived with. The three
                // facts a player needs in a row are: am I short, CAN I make more, and
                // what will it cost me to try.
                float effort = runner.Simulator.SupplyEffort(c.sector);
                line.note.text = p.Balance >= 0f ? "spare to sell"
                    : c.capability <= 0.35f ? "cannot make enough - must import"
                    : effort <= 0.01f ? "no lever - grows only with the economy"
                    : effort >= 5f ? "cheap to expand - subsidise it"
                    : effort >= 3f ? "costly to expand"
                    : "very costly to expand";

                string mood = p.domesticPrice > 108f ? "short" : p.domesticPrice < 97f ? "spare" : "level";
                Restyle(line.balance, mood);
            }

            SetLabel("production-summary", short_ == 0
                ? "The country supplies itself - " + Billions(spare) + " spare to sell"
                : short_ + (short_ == 1 ? " product short" : " products short") + ", "
                  + Billions(spare) + " spare elsewhere");
        }

        static float Cover(EconomyState state, ProductId id)
        {
            ProductState p;
            if (!state.production.TryGetValue(id, out p)) return 9f;
            return p.needed <= 0.01f ? 9f : p.produced / p.needed;
        }

        static void Restyle(Label label, string mood)
        {
            label.RemoveFromClassList("short");
            label.RemoveFromClassList("spare");
            label.RemoveFromClassList("level");
            label.AddToClassList(mood);
        }
    }
}
