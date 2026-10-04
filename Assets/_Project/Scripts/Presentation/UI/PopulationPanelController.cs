using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 7.8. One scrollable screen: the age pyramid, births and deaths, the
    /// taxable income pool sector by sector, veterans, unrest by class, and the
    /// slow levers - immigration and the child tax credit.
    /// </summary>
    public partial class PopulationPanelController : DrawerPanelController
    {
        [Header("Templates")]
        [SerializeField] VisualTreeAsset ageBandRowTemplate;
        [SerializeField] VisualTreeAsset sectorRowTemplate;
        [SerializeField] VisualTreeAsset approvalBarTemplate;

        readonly List<PolicyStepper> _steppers = new List<PolicyStepper>();
        readonly List<VisualElement> _bands = new List<VisualElement>();
        readonly List<VisualElement> _sectorRows = new List<VisualElement>();
        readonly List<VisualElement> _unrestRows = new List<VisualElement>();
        LineChartComponent _chart;

        protected override void Build()
        {
            _steppers.Clear();
            _bands.Clear(); _sectorRows.Clear(); _unrestRows.Clear();

            Stamp(ageBandRowTemplate, "age-band-list", _bands, new[] { "Youth (0-17)", "Working age (18-64)", "Retired (65+)" }, "band-name");
            BuildSectors();
            Stamp(approvalBarTemplate, "income-class-list", _unrestRows, new[] { "Poor - unrest", "Middle - unrest", "Wealthy - unrest" }, "approval-name");
            BuildLevers();

            VisualElement chart = Root.Q<VisualElement>("population-chart-container");
            if (chart != null)
            {
                chart.Clear();
                _chart = new LineChartComponent();
                if (theme != null) { _chart.lineColor = theme.neutralData; _chart.gridColor = theme.gridLine; }
                chart.Add(_chart);
            }
        }

        void Stamp(VisualTreeAsset template, string listName, List<VisualElement> into, string[] names, string nameElement)
        {
            VisualElement list = Root.Q<VisualElement>(listName);
            if (list == null || template == null) return;
            list.Clear();
            foreach (string name in names)
            {
                VisualElement row = template.Instantiate();
                Label label = row.Q<Label>(nameElement);
                if (label != null) label.text = name;
                list.Add(row);
                into.Add(row);
            }
        }

        void BuildSectors()
        {
            VisualElement list = Root.Q<VisualElement>("sector-employment-list");
            if (list == null || sectorRowTemplate == null) return;
            list.Clear();
            foreach (SectorDefinition sector in database.Sectors)
            {
                VisualElement row = sectorRowTemplate.Instantiate();
                Label name = row.Q<Label>("sector-name");
                if (name != null) name.text = sector.displayName;
                VisualElement swatch = row.Q<VisualElement>("sector-colour");
                if (swatch != null) swatch.style.backgroundColor = sector.chartColor;
                list.Add(row);
                _sectorRows.Add(row);
            }
        }

        void BuildLevers()
        {
            VisualElement list = Root.Q<VisualElement>("population-policy-list");
            if (list == null || policyRowTemplate == null) return;
            list.Clear();

            PopulationParameters p = database.Population;
            float step = p != null ? p.immigrationStep : 0.1f;
            float max = p != null ? p.maximumImmigrationInflow : 3f;

            _steppers.Add(new PolicyStepper(policyRowTemplate, list, "Net immigration").Bind(
                () => Policy.immigrationInflowMillions,
                () => Policy.PendingScalar("immigrationInflow", Policy.immigrationInflowMillions),
                v => Policy.QueueScalar("immigrationInflow", v, Policy.immigrationInflowMillions),
                step, 0f, max, v => v.ToString("0.0") + "M/yr",
                (c, v) => Shift(pol => pol.immigrationInflowMillions = v, pol => pol.immigrationInflowMillions = c)
                          + "  lowers service and farm wages"));

            _steppers.Add(new PolicyStepper(policyRowTemplate, list, "Skilled share of immigration").Bind(
                () => Policy.skilledImmigrationShare,
                () => Policy.PendingScalar("skilledImmigrationShare", Policy.skilledImmigrationShare),
                v => Policy.QueueScalar("skilledImmigrationShare", v, Policy.skilledImmigrationShare),
                0.05f, 0f, 1f, v => (v * 100f).ToString("0") + "%",
                (c, v) => v > c ? "feeds Technology" : "feeds Services and Agriculture"));

            TaxDefinition credit = FindTax("SO_Tax_ChildCredit");
            if (credit != null)
                _steppers.Add(new PolicyStepper(policyRowTemplate, list, "Child tax credit").Bind(
                    () => Policy.Tax(TaxKeys.ChildCredit),
                    () => Policy.PendingTax(TaxKeys.ChildCredit),
                    v => Policy.QueueTax(TaxKeys.ChildCredit, v),
                    credit.stepSize, credit.minimumValue, credit.maximumValue, v => "$" + v.ToString("#,0"),
                    (c, v) => SignedBillions(runner.Simulator.Treasury.EstimateLine(TaxKeys.ChildCredit, v, runner.State)
                                             - runner.Simulator.Treasury.EstimateLine(TaxKeys.ChildCredit, c, runner.State))
                              + "/yr  " + ApprovalModel.Describe(runner.Simulator.Approval.PreviewTax(runner.State, Policy, TaxKeys.ChildCredit, v))
                              + "  workers in 18 years"));
        }

        string Shift(System.Action<PolicyState> apply, System.Action<PolicyState> restore)
        {
            float[] shift = runner.Simulator.Approval.PreviewShift(runner.State, Policy, apply, restore);
            string[] names = { "poor", "mid", "rich", "left", "ctr", "right" };
            string text = "";
            for (int i = 0; i < 6; i++)
                if (Mathf.Abs(shift[i]) >= 0.5f)
                    text += (text.Length > 0 ? " " : "") + names[i] + " " + (shift[i] > 0f ? "+" : "") + shift[i].ToString("0");
            return text.Length == 0 ? "no one notices" : text;
        }

        TaxDefinition FindTax(string assetName)
        {
            foreach (TaxDefinition tax in database.Taxes)
                if (tax != null && tax.name == assetName) return tax;
            return null;
        }
    }
}
