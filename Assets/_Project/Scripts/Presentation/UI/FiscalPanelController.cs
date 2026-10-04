using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 8 and 11. One stepper per tax and spending asset - add an asset and it
    /// appears here with no code change. Every change is queued to the quarter
    /// boundary, and every tax row previews what the queued rate would raise
    /// against what the current one does.
    /// </summary>
    public partial class FiscalPanelController : DrawerPanelController
    {
        readonly List<PolicyStepper> _steppers = new List<PolicyStepper>();

        protected override void Build()
        {
            _steppers.Clear();
            BuildRevenue();
            BuildSpending();
            BindTabs();
        }

        void BuildRevenue()
        {
            VisualElement list = Root.Q<VisualElement>("revenue-list");
            if (list == null || policyRowTemplate == null) return;
            list.Clear();

            List<TaxDefinition> taxes = new List<TaxDefinition>(database.Taxes);
            taxes.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            foreach (TaxDefinition tax in taxes)
            {
                string key = PolicyBootstrap.KeyFor(tax, "SO_Tax_");
                TaxDefinition captured = tax;

                _steppers.Add(new PolicyStepper(policyRowTemplate, list, tax.displayName).Bind(
                    () => Policy.Tax(key),
                    () => Policy.PendingTax(key),
                    v => Policy.QueueTax(key, v),
                    tax.stepSize, tax.minimumValue, tax.maximumValue,
                    v => FormatTax(captured, v),
                    (current, pending) => RevenueDelta(key, current, pending))
                    .Resting(v => Basis(key, v, tax.stepSize)));
            }
        }

        void BuildSpending()
        {
            List<SpendingCategoryDefinition> lines = new List<SpendingCategoryDefinition>(database.SpendingCategories);
            lines.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            foreach (string group in new[] { "social", "military", "infrastructure", "other", "automatic" })
            {
                VisualElement list = Root.Q<VisualElement>("spending-list-" + group);
                if (list != null) list.Clear();
            }

            foreach (SpendingCategoryDefinition line in lines)
            {
                VisualElement list = Root.Q<VisualElement>("spending-list-" + line.group.ToString().ToLowerInvariant());
                if (list == null || policyRowTemplate == null) continue;

                string key = PolicyBootstrap.KeyFor(line, "SO_Spend_");
                PolicyStepper stepper = new PolicyStepper(policyRowTemplate, list, line.displayName);

                if (line.isAutomatic)
                {
                    stepper.ReadOnly(() => LiveSpending(key), Billions, "set by the model, not by you");
                }
                else
                {
                    int lag = line.lagQuarters;
                    stepper.Bind(
                        () => Policy.Spending(key),
                        () => Policy.PendingSpending(key),
                        v => Policy.QueueSpending(key, v),
                        line.stepBillions, line.minimumBillions, line.maximumBillions,
                        Billions,
                        (current, pending) => SpendingDelta(key, current, pending, lag))
                        .Resting(v => SpendingBasis(v, line.stepBillions));
                }

                _steppers.Add(stepper);
            }
        }

        void BindTabs()
        {
            VisualElement revenue = Root.Q<VisualElement>("revenue-section");
            VisualElement spending = Root.Q<VisualElement>("spending-section");

            Button revenueTab = Root.Q<Button>("tab-revenue");
            Button spendingTab = Root.Q<Button>("tab-spending");

            if (revenueTab != null) revenueTab.clicked += () => Show(revenue, spending, revenueTab, spendingTab);
            if (spendingTab != null) spendingTab.clicked += () => Show(spending, revenue, spendingTab, revenueTab);

            Show(revenue, spending, revenueTab, spendingTab);
        }

        static void Show(VisualElement on, VisualElement off, Button onTab, Button offTab)
        {
            if (on != null) on.style.display = DisplayStyle.Flex;
            if (off != null) off.style.display = DisplayStyle.None;
            if (onTab != null) onTab.AddToClassList("selected");
            if (offTab != null) offTab.RemoveFromClassList("selected");
        }

        protected override void Refresh(EconomyState state)
        {
            foreach (PolicyStepper stepper in _steppers) stepper.Refresh();

            TreasuryState t = state.treasury;
            float balance = t.totalRevenue - t.totalSpending;

            SetLabel("total-revenue", "Revenue " + Billions(t.totalRevenue));
            SetLabel("total-spending", "Spending " + Billions(t.totalSpending));
            SetLabel("budget-balance", "Balance " + SignedBillions(balance));
            SetLabel("balance-share-gdp", state.budgetBalancePercentGdp.ToString("+0.0;-0.0") + "% of GDP");
            SetLabel("queued-count", Policy.PendingChangeCount + " queued - land at the end of Q" + state.Quarter);
            RefreshBreakdown(state);
        }

        /// <summary>GDD 18: revenue AND approval by class, before the change is queued.</summary>
        string RevenueDelta(string key, float current, float pending)
        {
            TreasuryModel treasury = runner.Simulator.Treasury;
            float delta = treasury.EstimateLine(key, pending, runner.State) - treasury.EstimateLine(key, current, runner.State);
            float[] shift = runner.Simulator.Approval.PreviewTax(runner.State, Policy, key, pending);

            // The rate, the base it is charged on, and what it raises - a rate on its own
            // is a number nobody can reason about.
            float raised = treasury.EstimateLine(key, pending, runner.State);
            float baseSize = treasury.LineBaseBillions(key, runner.State);
            string basis = baseSize > 0.5f
                ? " on " + Billions(baseSize) + " = " + Billions(raised) + "/yr"
                : "";

            return SignedBillions(delta) + "/yr" + basis + "  " + ApprovalModel.Describe(shift);
        }

        string SpendingDelta(string key, float current, float pending, int lag)
        {
            float[] shift = runner.Simulator.Approval.PreviewSpending(runner.State, Policy, key, pending);
            return SignedBillions(pending - current) + "/yr  " + ApprovalModel.Describe(shift)
                   + (lag > 0 ? "  bites in " + lag + " qtr" : "");
        }

        float LiveSpending(string key)
        {
            float value;
            return runner.State.treasury.spendingByLine.TryGetValue(key, out value) ? value : 0f;
        }

        static string FormatTax(TaxDefinition tax, float value)
        {
            switch (tax.unit)
            {
                case PolicyUnit.DollarsPerTon: return "$" + value.ToString("0") + "/t";
                case PolicyUnit.DollarsPerUnit: return "$" + value.ToString("#,0");
                default: return value.ToString(tax.stepSize < 1f ? "0.0#" : "0") + "%";
            }
        }
    }
}
