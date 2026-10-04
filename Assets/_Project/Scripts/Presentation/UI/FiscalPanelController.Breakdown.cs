using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 8. The Fiscal drawer's copy of the revenue breakdown, and the line every
    /// tax row carries at rest: what the rate is charged on, what it raises, and what
    /// one more step of it would be worth. A rate with no base behind it is not a
    /// decision anyone can make.
    /// </summary>
    public partial class FiscalPanelController
    {
        [Header("Revenue breakdown")]
        [Tooltip("BreakdownRow.uxml - one bar per revenue line.")]
        [SerializeField] VisualTreeAsset breakdownRowTemplate;
        [Tooltip("Lines smaller than this share of revenue are gathered into one bar.")]
        [Range(0f, 0.1f)] [SerializeField] float smallLineShare = 0.02f;

        RevenueBreakdown _breakdown, _spending;

        void RefreshBreakdown(EconomyState state)
        {
            if (_breakdown == null)
                _breakdown = new RevenueBreakdown(Root.Q<VisualElement>("revenue-breakdown"), breakdownRowTemplate,
                                                  Root.Q<Label>("breakdown-note"), smallLineShare, database.Taxes);
                _breakdown.OnHover(ShowTaxTip, HideTaxTip);
            _breakdown.Refresh(state);

            if (_spending == null)
                _spending = new RevenueBreakdown(Root.Q<VisualElement>("spending-breakdown"), breakdownRowTemplate,
                                                 Root.Q<Label>("spending-note"), smallLineShare, null,
                                                 database.SpendingCategories, RevenueBreakdown.Side.Spending);
            _spending.OnHover(ShowTaxTip, HideTaxTip);
            _spending.Refresh(state);
        }

        /// <summary>What a spending line costs now, and what one more step of it costs -
        /// the same question a tax row answers, from the other side of the budget.</summary>
        string SpendingBasis(float amount, float step)
        {
            float share = amount / Mathf.Max(1f, runner.State.nominalGdpBillions) * 100f;
            return Billions(amount) + "/yr, " + share.ToString("0.0") + "% of GDP"
                   + (step > 0f ? ",  next step " + SignedBillions(step) + "/yr" : "");
        }

        /// <summary>
        /// What this rate is charged on, what it brings in, and what the next step up
        /// would add - shown even when nothing is queued, because "5%" means nothing
        /// until you know 5% of what.
        /// </summary>
        string Basis(string key, float rate, float step)
        {
            TreasuryModel treasury = runner.Simulator.Treasury;
            float baseSize = treasury.LineBaseBillions(key, runner.State);
            if (baseSize <= 0.5f) return "";

            float raised = treasury.EstimateLine(key, rate, runner.State);
            float next = treasury.EstimateLine(key, rate + step, runner.State) - raised;

            return "on " + Billions(baseSize) + " -> " + Billions(raised) + "/yr"
                   + (step > 0f ? ",  next step " + SignedBillions(next) + "/yr" : "");
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class FiscalPanelController
    {
        /// <summary>Hovering a bar in the drawer explains that tax, in the drawer's own
        /// tooltip - a drawer is its own UIDocument and cannot draw into the dashboard's.</summary>
        void ShowTaxTip(string title, string body, Vector3 position)
        {
            VisualElement tip = Root.Q<VisualElement>("fiscal-tooltip");
            if (tip == null) return;

            Label titleLabel = Root.Q<Label>("fiscal-tooltip-title");
            Label bodyLabel = Root.Q<Label>("fiscal-tooltip-body");
            if (titleLabel != null) titleLabel.text = title;
            if (bodyLabel != null) bodyLabel.text = body;

            tip.style.display = DisplayStyle.Flex;
            float width = tip.resolvedStyle.width > 1f ? tip.resolvedStyle.width : 330f;
            float height = tip.resolvedStyle.height > 1f ? tip.resolvedStyle.height : 90f;
            tip.style.left = Mathf.Clamp(position.x + 14f, 8f, Mathf.Max(8f, Root.resolvedStyle.width - width - 8f));
            tip.style.top = Mathf.Clamp(position.y + 16f, 8f, Mathf.Max(8f, Root.resolvedStyle.height - height - 8f));
        }

        void HideTaxTip()
        {
            VisualElement tip = Root.Q<VisualElement>("fiscal-tooltip");
            if (tip != null) tip.style.display = DisplayStyle.None;
        }

        /// <summary>What a hovered bar would say - for the tests.</summary>
        public string TaxTipText
        {
            get
            {
                VisualElement tip = Root.Q<VisualElement>("fiscal-tooltip");
                if (tip == null || tip.style.display == DisplayStyle.None) return "";
                Label title = Root.Q<Label>("fiscal-tooltip-title");
                Label body = Root.Q<Label>("fiscal-tooltip-body");
                return (title == null ? "" : title.text) + ": " + (body == null ? "" : body.text);
            }
        }
    }
}
