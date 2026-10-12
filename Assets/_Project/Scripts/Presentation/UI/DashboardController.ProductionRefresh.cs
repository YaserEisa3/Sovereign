using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>The weekly rewrite of the production table.</summary>
    public partial class DashboardController
    {
        readonly List<ProductConfig> _sortedProducts = new List<ProductConfig>();

        void RefreshProduction(EconomyState state)
        {
            if (!_productionView || _productRows.Count == 0) return;
            if (runner == null || runner.Simulator == null) return;

            ProductConfig[] products = runner.Simulator.Production.Products;

            // Scarcest first. A player opening this wants to know what they are short of,
            // not to read sixteen rows looking for the one in red.
            _sortedProducts.Clear();
            _sortedProducts.AddRange(products);
            _sortedProducts.Sort((a, b) => Price(state, b.id).CompareTo(Price(state, a.id)));

            int short_ = 0;
            float spare = 0f;
            for (int i = 0; i < _productRows.Count && i < _sortedProducts.Count; i++)
            {
                ProductConfig c = _sortedProducts[i];
                ProductRow row = _productRows[i];
                ProductState p;
                if (!state.production.TryGetValue(c.id, out p)) continue;

                if (p.Balance < 0f) short_++; else spare += p.Balance;

                row.name.text = c.name;
                row.made.text = "$" + p.produced.ToString("#,0") + "B of $" + p.needed.ToString("#,0") + "B";
                row.balance.text = p.Balance.ToString("+#,0;-#,0") + "B";
                row.price.text = p.domesticPrice.ToString("0");

                // The note is the point of the row: what this means and what it costs.
                row.note.text = p.Balance >= 0f
                    ? "spare to sell"
                    : c.shortageSeverity >= 0.7f
                        ? "SHORT - and people feel this one"
                        : "short - must be bought abroad";

                string state_ = p.domesticPrice > 108f ? "short"
                              : p.domesticPrice < 97f ? "spare" : "level";
                Restyle(row.balance, "product-balance", state_);
                Restyle(row.price, "product-price", state_);
            }

            if (_productionHead != null)
                _productionHead.text = short_ == 0
                    ? "The country supplies itself - $" + spare.ToString("#,0") + "B spare to sell"
                    : short_ + (short_ == 1 ? " product is short" : " products are short")
                      + ", $" + spare.ToString("#,0") + "B spare elsewhere";
        }

        static float Price(EconomyState state, ProductId id)
        {
            ProductState p;
            return state.production.TryGetValue(id, out p) ? p.domesticPrice : 0f;
        }

        static void Restyle(Label label, string baseClass, string state)
        {
            label.RemoveFromClassList("short");
            label.RemoveFromClassList("spare");
            label.RemoveFromClassList("level");
            label.AddToClassList(state);
        }
    }
}
