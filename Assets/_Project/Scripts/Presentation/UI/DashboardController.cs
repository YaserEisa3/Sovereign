using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 18 and 20 Phase 4. Queries the elements Dashboard.uxml already defines
    /// and fills them from the simulation. It builds no layout: every name below
    /// exists in the UXML, and the repeated KPI cards come from KPICard.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public partial class DashboardController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] SimulationRunner runner;
        [SerializeField] UITheme theme;

        [Tooltip("KPICard.uxml - the template each indicator card is stamped from.")]
        [SerializeField] VisualTreeAsset kpiCardTemplate;

        [Tooltip("MeterRow.uxml - one bar in the all-meters view.")]
        [SerializeField] VisualTreeAsset meterRowTemplate;
        [Tooltip("BreakdownRow.uxml - one bar of the revenue breakdown.")]
        [SerializeField] VisualTreeAsset breakdownRowTemplate;
        [Tooltip("SO_MeterParameters - the range, target and unit of every bar.")]
        [SerializeField] Sovereign.Data.MeterParameters meterParameters;

        [Tooltip("SaveLoadManager - the header's SAVE and LOAD buttons call it.")]
        [SerializeField] SaveLoadManager saveLoad;

        [Tooltip("MapCameraController - the map column's zoom buttons drive it.")]
        [SerializeField] MapCameraController mapCamera;

        [Header("Panels opened by the drawer bar")]
        [SerializeField] GameObject panelFiscal;
        [SerializeField] GameObject panelMonetaryBonds;
        [SerializeField] GameObject panelTradeCurrency;
        [SerializeField] GameObject panelRegulatory;
        [SerializeField] GameObject panelPopulation;
        [SerializeField] GameObject panelWorldEvents;
        [SerializeField] GameObject panelAdvisor;

        UIDocument _document;
        VisualElement _root;

        Label _date, _chartReadout, _mapStatus;
        Label _inversionFlag, _foreignHoldings, _creditRating, _marketConfidence;
        Label _overallApproval;
        VisualElement _kpiRow, _curveRow;
        LineChartComponent _chart;
        readonly KpiCard[] _cards = new KpiCard[11];
        string _chartSeries = "gdpGrowth";

        struct KpiCard
        {
            public Label label, value, delta;
        }

        void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            BuildKpiCards();
            BuildApprovalBars();
            BuildChart();
            BindButtons();
            BindAlertBell();
            BindSaveLoad();
            BuildMeters();
            BuildDrivers();
            BuildIndustry();
            BindMapControls();

            // Where the money comes from, on the front page where the bond chart was.
            _breakdown = new RevenueBreakdown(_root.Q<VisualElement>("revenue-breakdown"), breakdownRowTemplate,
                                              _root.Q<Label>("breakdown-note"), 0.02f,
                                              runner == null || runner.Database == null ? null : runner.Database.Taxes);
            // Hovering a bar says what that tax is, in the same tooltip the meters use.
            _breakdown.OnHover((title, body, position) => ShowMapTip(title, body, ScreenFromPanel(position)), HideMapTip);

            // And the other half of the budget: where it all goes.
            _spendingBreakdown = new RevenueBreakdown(_root.Q<VisualElement>("spending-breakdown"), breakdownRowTemplate,
                                                      _root.Q<Label>("spending-note"), 0.02f,
                                                      null,
                                                      runner == null || runner.Database == null ? null : runner.Database.SpendingCategories,
                                                      RevenueBreakdown.Side.Spending);
            _spendingBreakdown.OnHover((title, body, position) => ShowMapTip(title, body, ScreenFromPanel(position)), HideMapTip);

            if (runner != null)
            {
                runner.OnWeekTick += Refresh;
                if (runner.State != null) Refresh(runner.State);
            }
        }

        void OnDisable()
        {
            if (runner != null) runner.OnWeekTick -= Refresh;
        }

        void CacheElements()
        {
            _date = _root.Q<Label>("date-label");
            _chartReadout = _root.Q<Label>("chart-readout");
            _mapStatus = _root.Q<Label>("map-status");
            _inversionFlag = _root.Q<Label>("inversion-flag");
            _foreignHoldings = _root.Q<Label>("foreign-holdings");
            _creditRating = _root.Q<Label>("credit-rating");
            _marketConfidence = _root.Q<Label>("market-confidence");
            _overallApproval = _root.Q<Label>("overall-approval");
            _kpiRow = _root.Q<VisualElement>("kpi-row");
            _curveRow = _root.Q<VisualElement>("yield-curve-container");
        }

        /// <summary>Clears the authored placeholders and stamps six real cards from
        /// the template, per GDD 3.7.</summary>
        void BuildKpiCards()
        {
            if (_kpiRow == null || kpiCardTemplate == null) return;
            _kpiRow.Clear();

            for (int i = 0; i < _cards.Length; i++)
            {
                VisualElement card = kpiCardTemplate.Instantiate();
                // Instantiate wraps the template in a container; the card is inside it.
                VisualElement content = card.childCount > 0 ? card[0] : card;
                content.AddToClassList("kpi-card");

                _cards[i] = new KpiCard
                {
                    label = card.Q<Label>("kpi-label"),
                    value = card.Q<Label>("kpi-value"),
                    delta = card.Q<Label>("kpi-delta")
                };
                _kpiRow.Add(card);
            }
        }

        void BuildChart()
        {
            VisualElement container = _root.Q<VisualElement>("chart-container");
            if (container == null) return;

            container.Clear();
            _chart = new LineChartComponent();
            if (theme != null)
            {
                _chart.lineColor = theme.neutralData;
                _chart.gridColor = theme.gridLine;
                _chart.dangerColor = theme.danger;
            }
            _chart.threshold = 0f;
            _chart.Hovered += OnChartHover;
            container.Add(_chart);
        }
    }
}
