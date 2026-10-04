using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 11 and 17.6. Bank capital, environmental and labour regulation, and the
    /// sector subsidies. Capital requirements are the one regulatory lever that
    /// decides whether a banking crisis can happen at all.
    /// </summary>
    public class RegulatoryPanelController : DrawerPanelController
    {
        [Header("Control ranges")]
        [SerializeField] float capitalMax = 20f;
        [SerializeField] float capitalStep = 0.5f;
        [SerializeField] float regulationStep = 5f;

        [Tooltip("Capital requirement at or below which a banking crisis can fire - matches the BankingCrisis event's condition.")]
        [SerializeField] float crisisCapitalLine = 10f;

        readonly List<PolicyStepper> _steppers = new List<PolicyStepper>();

        protected override void Build()
        {
            _steppers.Clear();

            Add("financial-regulation-list", "Bank capital requirement", "bankingCapitalRequirement",
                () => Policy.bankingCapitalRequirement, capitalStep, 0f, capitalMax, v => v.ToString("0.0") + "%",
                (c, v) => (v <= crisisCapitalLine ? "banking crises possible" : "banking crises ruled out")
                          + (v > c ? ", tighter credit squeezes Finance" : ", looser credit, more risk"));

            Add("environment-regulation-list", "Environmental regulation", "environmentalRegulation",
                () => Policy.environmentalRegulation, regulationStep, 0f, 100f, v => v.ToString("0"),
                (c, v) => v > c ? "the Left and Euroland approve; Energy and Manufacturing pay" : "industry relieved; the Left and Euroland are not");

            Add("labour-regulation-list", "Labour regulation", "labourRegulation",
                () => Policy.labourRegulation, regulationStep, 0f, 100f, v => v.ToString("0"),
                (c, v) => v > c ? "the poor approve; labour costs rise, FDI cools" : "cheaper labour draws FDI; the poor object");

            VisualElement subsidies = Root.Q<VisualElement>("subsidy-list");
            if (subsidies != null && policyRowTemplate != null)
            {
                subsidies.Clear();
                foreach (SpendingCategoryDefinition line in database.SpendingCategories)
                {
                    if (line == null || !line.displayName.Contains("Subsidies")) continue;
                    string key = PolicyBootstrap.KeyFor(line, "SO_Spend_");
                    _steppers.Add(new PolicyStepper(policyRowTemplate, subsidies, line.displayName).Bind(
                        () => Policy.Spending(key), () => Policy.PendingSpending(key), v => Policy.QueueSpending(key, v),
                        line.stepBillions, line.minimumBillions, line.maximumBillions, Billions,
                        (c, v) => SignedBillions(v - c) + "/yr"));
                }
            }
        }

        void Add(string listName, string label, string key, System.Func<float> current, float step, float min, float max,
                 System.Func<float, string> format, System.Func<float, float, string> impact)
        {
            VisualElement list = Root.Q<VisualElement>(listName);
            if (list == null || policyRowTemplate == null) return;
            list.Clear();
            _steppers.Add(new PolicyStepper(policyRowTemplate, list, label).Bind(
                current, () => Policy.PendingScalar(key, current()), v => Policy.QueueScalar(key, v, current()),
                step, min, max, format, impact));
        }

        protected override void Refresh(EconomyState state)
        {
            foreach (PolicyStepper stepper in _steppers) stepper.Refresh();

            bool exposed = Policy.bankingCapitalRequirement <= crisisCapitalLine;
            SetLabel("banking-risk-readout", "Capital requirement " + Policy.bankingCapitalRequirement.ToString("0.0") + "% - "
                     + (exposed ? "a banking crisis CAN happen" : "banking crises are ruled out")
                     + "   Finance health " + (state.GetSector(SectorId.Finance) == null ? "--" : state.GetSector(SectorId.Finance).health.ToString("0")));

            float burden = 0f;
            foreach (EconomicSector sector in state.sectors) burden += sector.regulationBurden;
            SetLabel("regulation-burden-readout", "Average regulation burden " + (burden / Mathf.Max(1, state.sectors.Count)).ToString("0"));
        }
    }
}
