using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 9 and 12. The central bank and the debt office in one drawer. Rate, QE,
    /// QT and reserves are queued to the quarter like everything else; forward
    /// guidance is not, because the GDD says it moves expectations immediately -
    /// that immediacy is the whole point of it.
    /// </summary>
    public partial class MonetaryBondsPanelController : DrawerPanelController
    {
        [Header("Templates")]
        [SerializeField] VisualTreeAsset maturityRowTemplate;
        [SerializeField] VisualTreeAsset nationStatusRowTemplate;

        [Header("Control ranges - GDD 12")]
        [SerializeField] float rateStep = 0.25f;
        [SerializeField] float rateMax = 20f;
        [SerializeField] float qeStep = 25f;
        [SerializeField] float qeMax = 500f;
        [SerializeField] float qtMax = 300f;
        [SerializeField] float reserveStep = 0.5f;
        [SerializeField] float reserveMax = 20f;
        [Tooltip("GDD 9.3: 0 - 80% of new issuance offered abroad.")]
        [SerializeField] float foreignShareMax = 0.8f;

        [Header("Warnings")]
        [Tooltip("Core inflation above which the QE row warns you are printing into a fire.")]
        [SerializeField] float qeWarningInflation = 4f;

        readonly List<PolicyStepper> _steppers = new List<PolicyStepper>();
        LineChartComponent _curve;
        string _emergencyNote = "";

        protected override void Build()
        {
            _steppers.Clear();
            _emergencyNote = "";

            VisualElement monetary = Root.Q<VisualElement>("monetary-list");
            if (monetary != null && policyRowTemplate != null)
            {
                monetary.Clear();
                _steppers.Add(Scalar(monetary, "Policy rate", "centralBankRate",
                    () => Policy.centralBankRate, rateStep, 0f, rateMax, v => v.ToString("0.00") + "%",
                    (c, p) => "real rate " + (p - runner.State.inflation).ToString("+0.0;-0.0") + "%, bites over 3-4 qtr")
                    .Resting(v => RateBasis(v)));

                _steppers.Add(Scalar(monetary, "QE per quarter", "qeAmount",
                    () => Policy.qeAmountPerQuarter, qeStep, 0f, qeMax, Billions,
                    (c, p) => runner.State.coreInflation > qeWarningInflation && p > c
                        ? "printing into " + runner.State.coreInflation.ToString("0.0") + "% inflation - credibility will pay"
                        : "yields now, prices in 2-3 qtr")
                    .Resting(v => BalanceSheetLine(v, qeStep, true)));

                _steppers.Add(Scalar(monetary, "QT per quarter", "qtAmount",
                    () => Policy.qtAmountPerQuarter, qeStep, 0f, qtMax, Billions,
                    (c, p) => "lifts yields, drains money")
                    .Resting(v => BalanceSheetLine(v, qeStep, false)));

                _steppers.Add(Scalar(monetary, "Reserve requirement", "reserveRequirement",
                    () => Policy.reserveRequirement, reserveStep, 0f, reserveMax, v => v.ToString("0.0") + "%",
                    (c, p) => p > c ? "throttles bank lending" : "loosens bank lending"));
            }

            VisualElement issuance = Root.Q<VisualElement>("issuance-list");
            if (issuance != null && policyRowTemplate != null)
            {
                issuance.Clear();
                _steppers.Add(Scalar(issuance, "Offered to foreign buyers", "foreignIssuanceShare",
                    () => Policy.foreignIssuanceShare, 0.05f, 0f, foreignShareMax, v => (v * 100f).ToString("0") + "%",
                    (c, p) => p > c ? "cheaper now, and leverage for your creditors" : "dearer, but yours"));
            }

            BuildCurve();
            BindGuidance();
            BindMaturity();
            BindForeignCurrency();
            BindEmergencyCut();
            BuildBuyback();
            BuildMaturityRows();
            BuildHolderRows();
        }

        PolicyStepper Scalar(VisualElement parent, string label, string key, System.Func<float> current,
                             float step, float min, float max, System.Func<float, string> format,
                             System.Func<float, float, string> impact)
        {
            return new PolicyStepper(policyRowTemplate, parent, label).Bind(
                current,
                () => Policy.PendingScalar(key, current()),
                v => Policy.QueueScalar(key, v, current()),
                step, min, max, format, impact);
        }

        void BuildCurve()
        {
            VisualElement container = Root.Q<VisualElement>("yield-curve-container");
            if (container == null) return;
            container.Clear();
            _curve = new LineChartComponent();
            if (theme != null) { _curve.lineColor = theme.neutralData; _curve.gridColor = theme.gridLine; }
            container.Add(_curve);
        }
    }
}
