using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 19. What the country makes, what it needs, and what it is short of.
    ///
    /// The production model ran for a whole session with no way to see it: sixteen
    /// products being grown, mined and built every week, shortages pushing on inflation,
    /// and the only view of any of it was a table printed by a test. A number that moves
    /// the game and cannot be looked at is a number the player is entitled to distrust.
    ///
    /// Sorted by what is scarcest, because the shortage is the thing worth acting on and
    /// a player should not have to hunt down a list of sixteen to find it.
    /// </summary>
    public partial class DashboardController
    {
        class ProductRow
        {
            public Label name, made, balance, price, note;
        }

        VisualElement _productionContainer;
        Button _productionToggle;
        Label _productionHead;
        readonly List<ProductRow> _productRows = new List<ProductRow>();
        bool _productionView;

        void BuildProduction()
        {
            _productionContainer = _root.Q<VisualElement>("production-container");
            _productionToggle = _root.Q<Button>("production-toggle");
            if (_productionContainer == null) return;

            VisualElement content = _productionContainer is ScrollView
                ? ((ScrollView)_productionContainer).contentContainer
                : _productionContainer;
            content.Clear();
            _productRows.Clear();

            VisualElement head = new VisualElement();
            head.AddToClassList("product-head");
            _productionHead = Text("", "product-head-label");
            head.Add(_productionHead);
            content.Add(head);

            // One row per product, built once. Counted from the ENUM, not from the live
            // model: this runs before the simulator boots, so asking it how many
            // products exist answered zero and the table was built empty - which looked
            // exactly like a working table until the smoke test counted the rows.
            int count = System.Enum.GetValues(typeof(ProductId)).Length;
            for (int i = 0; i < count; i++)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("product-row");

                ProductRow built = new ProductRow
                {
                    name = Text("", "product-name"),
                    made = Text("", "product-made"),
                    balance = Text("", "product-balance"),
                    price = Text("", "product-price"),
                    note = Text("", "product-note")
                };
                row.Add(built.name);
                row.Add(built.made);
                row.Add(built.balance);
                row.Add(built.price);
                row.Add(built.note);

                _productRows.Add(built);
                content.Add(row);
            }

            if (_productionToggle != null) _productionToggle.clicked += () => SetProductionView(true);
            SetProductionView(_productionView);
        }

        /// <summary>Five views share the chart's box and exactly one may be open.</summary>
        void SetProductionView(bool on)
        {
            _productionView = on;
            if (on) { SetMeterView(false); SetIndustryView(false); SetDriverView(false); }

            if (_productionContainer != null)
                _productionContainer.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (on && _chartContainer != null) _chartContainer.style.display = DisplayStyle.None;
            if (_chartReadout != null && on) _chartReadout.style.display = DisplayStyle.None;

            if (_productionToggle != null)
            {
                if (on) _productionToggle.AddToClassList("selected");
                else _productionToggle.RemoveFromClassList("selected");
            }

            if (on && runner != null && runner.State != null) RefreshProduction(runner.State);
        }
    }
}
