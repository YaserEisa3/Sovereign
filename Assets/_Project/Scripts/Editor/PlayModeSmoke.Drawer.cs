using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// The playable loop, driven through clickable points on screen: open a drawer,
    /// press a stepper, see it queue, watch it land at the quarter.
    /// </summary>
    public static partial class PlayModeSmoke
    {
        enum Stage
        {
            Dashboard, OpenFiscal, PressStepper, VerifyQueued, AwaitQuarter,
            CloseFiscalWithX, ReopenFiscal, CloseFiscalWithTab,
            OpenTrade, CloseTradeWithX, OpenMonetary, VerifyMonetary,
            OpenPopulation, VerifyPopulation, EndRun, VerifyGameOver, VerifyRestart,
            SpawnEvents, VerifyEvents, VerifyWorldEvents, VerifyAdvisor,
            VerifyTrade, PressDiplomacy, VerifyDiplomacy, VerifyRegulatory,
            VerifyShips, PressSave, VerifySave, PressLoad, VerifyLoad,
            PressBuyback, VerifyBuyback, HoverChart, VerifyHover, PressMeters, VerifyMeters,
            VerifyCityscape, PressZoom, VerifyZoom, Done
        }

        static Stage _stage = Stage.Dashboard;
        static float _queuedValue;
        static int _startQuarterWeek;
        static int _settleFrames;

        static void RunDrawerStages(SimulationRunner runner)
        {
            if (_settleFrames > 0) { _settleFrames--; return; }

            GameObject fiscal = FindPanel("Panel_Fiscal");

            switch (_stage)
            {
                case Stage.OpenFiscal:
                    if (!Press(Tab("open-fiscal"), "the Fiscal tab")) return;
                    Next(Stage.PressStepper);
                    return;

                case Stage.PressStepper:
                {
                    if (!Require(fiscal != null && fiscal.activeSelf, "clicking the Fiscal tab did not open it")) return;
                    VisualElement fiscalRoot = PanelRoot(fiscal);

                    // Where the money comes from, as bars - and a total under them.
                    VisualElement breakdown = fiscalRoot.Q<VisualElement>("revenue-breakdown");
                    Label breakdownNote = fiscalRoot.Q<Label>("breakdown-note");
                    int bars = 0;
                    if (breakdown != null)
                        foreach (VisualElement bar in breakdown.Children())
                            if (bar.style.display != DisplayStyle.None) bars++;
                    Debug.Log("PlayModeSmoke: revenue breakdown - " + bars + " bars, '"
                              + (breakdownNote == null ? "" : breakdownNote.text) + "'");
                    if (!Require(bars >= 4, "the revenue breakdown drew " + bars + " bars")) return;

                    // And the other half of the budget, in the same shape.
                    VisualElement spendBreakdown = fiscalRoot.Q<VisualElement>("spending-breakdown");
                    Label spendNote = fiscalRoot.Q<Label>("spending-note");
                    int spendBars = 0;
                    string spendNames = "";
                    if (spendBreakdown != null)
                        foreach (VisualElement bar in spendBreakdown.Children())
                            if (bar.style.display != DisplayStyle.None)
                            {
                                spendBars++;
                                Label barName = bar.Q<Label>("breakdown-name");
                                spendNames += (barName == null ? "?" : barName.text) + "; ";
                            }
                    Debug.Log("PlayModeSmoke: spending breakdown - " + spendBars + " bars, '"
                              + (spendNote == null ? "" : spendNote.text) + "' - " + spendNames);
                    if (!Require(spendBars >= 4, "the spending breakdown drew " + spendBars + " bars")) return;
                    if (!Require(spendNote != null && (spendNote.text.Contains("borrowed") || spendNote.text.Contains("surplus")),
                                 "the spending total does not say whether it is funded")) return;

                    // Hovering a spending bar must explain that line too.
                    VisualElement firstSpend = spendBreakdown[0];
                    VisualElement spendContent = firstSpend.childCount > 0 ? firstSpend[0] : firstSpend;
                    using (PointerEnterEvent spendEnter = PointerEnterEvent.GetPooled())
                    {
                        spendEnter.target = spendContent;
                        spendContent.SendEvent(spendEnter);
                    }
                    FiscalPanelController fiscalDesk = fiscal.GetComponent<FiscalPanelController>();
                    Debug.Log("PlayModeSmoke: hovering the top spending bar reads '" + fiscalDesk.TaxTipText + "'");
                    if (!Require(fiscalDesk.TaxTipText.Length > 40, "hovering a spending bar explained nothing")) return;
                    if (!Require(breakdownNote != null && breakdownNote.text.Contains("% of GDP"),
                                 "the revenue breakdown has no total")) return;

                    // Hovering a bar must explain that tax, and name it properly - the chart
                    // showed the internal key "ValueAdded" before anyone asked what it meant.
                    FiscalPanelController fiscalPanel = fiscal.GetComponent<FiscalPanelController>();
                    VisualElement firstBar = breakdown[0];
                    using (PointerEnterEvent enter = PointerEnterEvent.GetPooled())
                    {
                        enter.target = firstBar.childCount > 0 ? firstBar[0] : firstBar;
                        (firstBar.childCount > 0 ? firstBar[0] : firstBar).SendEvent(enter);
                    }
                    Debug.Log("PlayModeSmoke: hovering the top revenue bar reads '" + fiscalPanel.TaxTipText + "'");
                    if (!Require(fiscalPanel.TaxTipText.Length > 40, "hovering a revenue bar explained nothing")) return;

                    string labels = "";
                    foreach (Label barName in breakdown.Query<Label>(className: "breakdown-name").ToList()) labels += barName.text + "; ";
                    Debug.Log("PlayModeSmoke: revenue bars are named " + labels);
                    if (!Require(!labels.Contains("Value Added") && labels.Contains("VAT"),
                                 "the breakdown is labelled with internal keys: " + labels)) return;

                    VisualElement list = fiscalRoot.Q<VisualElement>("revenue-list");
                    if (!Require(list != null && list.childCount == 13,
                                 "the Fiscal drawer built " + (list == null ? 0 : list.childCount) + " revenue rows, expected 13")) return;
                    if (!Press(list[0].Q<Button>("policy-increase"), "the corporate tax + button")) return;
                    Next(Stage.VerifyQueued);
                    return;
                }

                case Stage.VerifyQueued:
                {
                    // Layout has settled by now, so this is where the geometry is measured:
                    // bars that line up, and labels with room for their own text. Checked
                    // one frame after opening the drawer, the elements had no size at all
                    // and both measurements quietly passed on nothing.
                    // The revenue chart, not the spending one: the drawer's spending section sits
                    // behind its own tab, and a hidden element has no geometry to measure.
                    VisualElement bars = PanelRoot(fiscal).Q<VisualElement>("revenue-breakdown");
                    float leftEdge = float.NaN, worstDrift = 0f, shortestLabel = float.MaxValue;
                    int measured = 0;
                    string clipped = "";
                    if (bars != null)
                    {
                        foreach (VisualElement track in bars.Query<VisualElement>(className: "breakdown-track").ToList())
                        {
                            if (track.worldBound.width <= 1f) continue;
                            measured++;
                            if (float.IsNaN(leftEdge)) leftEdge = track.worldBound.xMin;
                            worstDrift = Mathf.Max(worstDrift, Mathf.Abs(track.worldBound.xMin - leftEdge));
                        }
                        foreach (Label barName in bars.Query<Label>(className: "breakdown-name").ToList())
                        {
                            if (barName.worldBound.height <= 0.1f || barName.worldBound.height >= shortestLabel) continue;
                            shortestLabel = barName.worldBound.height;
                            clipped = barName.text;
                        }
                    }
                    Debug.Log("PlayModeSmoke: " + measured + " revenue bars, within " + worstDrift.ToString("0.0")
                              + "px of each other; shortest label box " + shortestLabel.ToString("0.0") + "px (" + clipped + ")");
                    if (!Require(measured >= 4, "only " + measured + " revenue bars had any size - nothing was measured")) return;
                    if (!Require(worstDrift < 1f, "the bars are not lined up - up to " + worstDrift.ToString("0") + "px apart")) return;
                    if (!Require(shortestLabel >= 13f, "the bar labels are only " + shortestLabel.ToString("0")
                                 + "px tall - the text is being cut off")) return;

                    if (!Require(runner.Policy.HasPendingTax(TaxKeys.CorporateIncome), "pressing + queued nothing")) return;
                    _queuedValue = runner.Policy.PendingTax(TaxKeys.CorporateIncome);
                    Label queued = PanelRoot(fiscal).Q<VisualElement>("revenue-list")[0].Q<Label>("policy-queued");
                    if (!Require(queued != null && queued.text.StartsWith("QUEUED"), "the change queued but the row does not say so")) return;
                    if (!Require(runner.Policy.Tax(TaxKeys.CorporateIncome) != _queuedValue, "the tax changed immediately instead of at the quarter")) return;

                    Debug.Log("PlayModeSmoke: corporate tax queued " + runner.Policy.Tax(TaxKeys.CorporateIncome)
                              + " -> " + _queuedValue + " at week " + runner.State.week + ", row reads '" + queued.text + "'");
                    _startQuarterWeek = runner.State.week;
                    runner.Speed = GameSpeed.Quadruple;
                    _stage = Stage.AwaitQuarter;
                    return;
                }

                case Stage.AwaitQuarter:
                {
                    if (runner.State.week <= (_startQuarterWeek / 13 + 1) * 13) return;
                    bool landed = runner.Policy.Tax(TaxKeys.CorporateIncome) == _queuedValue
                                  && !runner.Policy.HasPendingTax(TaxKeys.CorporateIncome);
                    Debug.Log("PlayModeSmoke: at week " + runner.State.week + " corporate tax is "
                              + runner.Policy.Tax(TaxKeys.CorporateIncome) + " (queued " + _queuedValue + ")");
                    if (!Require(landed, "the queued tax never landed at the quarter boundary")) return;
                    runner.Speed = GameSpeed.Normal;
                    _stage = Stage.CloseFiscalWithX;
                    return;
                }

                default:
                    RunClosingStages(fiscal);
                    return;
            }
        }
    }
}
