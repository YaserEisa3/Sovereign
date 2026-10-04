using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Phase 5, part two, through the running game: ships sailing the trade
    /// routes, SAVE and LOAD from the header, hovering the chart, and Steam presence.</summary>
    public static partial class PlayModeSmoke
    {
        static int _savedWeek;
        static Vector3 _shipStart;
        static float _debtBeforeBuyback;

        static void RunPhase5bStages(SimulationRunner runner)
        {
            SaveLoadManager saveLoad = Object.FindAnyObjectByType<SaveLoadManager>();
            VisualElement dash = DashboardRoot();

            switch (_stage)
            {
                case Stage.VerifyShips:
                {
                    int routes = 0, ships = 0;
                    Transform first = null;
                    foreach (TradeRouteView route in Object.FindObjectsByType<TradeRouteView>(FindObjectsSortMode.None))
                    {
                        routes++;
                        foreach (Transform child in route.transform)
                            if (child.name.StartsWith("TradeShip_") && child.gameObject.activeSelf)
                            {
                                ships++;
                                if (first == null) first = child;
                            }
                    }
                    // Lanes carry different amounts of trade, and every lane runs both ways.
                    int outbound = 0, inbound = 0, busiest = 0, quietest = int.MaxValue;
                    foreach (TradeRouteView route in Object.FindObjectsByType<TradeRouteView>(FindObjectsSortMode.None))
                    {
                        outbound += route.OutboundShips;
                        inbound += route.ShipCount - route.OutboundShips;
                        busiest = Mathf.Max(busiest, route.ShipCount);
                        quietest = Mathf.Min(quietest, route.ShipCount);
                    }
                    Debug.Log("PlayModeSmoke: " + ships + " ships sailing " + routes + " trade routes - "
                              + outbound + " outbound, " + inbound + " homebound; busiest lane " + busiest
                              + ", quietest " + quietest);
                    if (!Require(routes >= 6 && ships >= routes, "only " + ships + " ships on " + routes + " routes")) return;
                    if (!Require(outbound > 0 && inbound > 0, "every ship is sailing the same way")) return;
                    if (!Require(busiest > quietest, "every lane carries the same " + busiest + " ships, whatever the trade")) return;
                    _shipStart = first.position;
                    _shipProbe = first;
                    runner.Speed = GameSpeed.Normal;
                    Next(Stage.PressSave);
                    return;
                }

                case Stage.PressSave:
                {
                    float moved = (_shipProbe.position - _shipStart).magnitude;
                    Debug.Log("PlayModeSmoke: a ship sailed " + moved.ToString("0.00") + " units while the clock ran");
                    if (!Require(moved > 0f, "the ships are not moving")) return;

                    if (!Require(saveLoad != null, "there is no SaveLoadManager in the scene")) return;
                    saveLoad.SaveFileName = "sovereign_smoke_save.json";
                    if (File.Exists(saveLoad.SavePath)) File.Delete(saveLoad.SavePath);
                    runner.Speed = GameSpeed.Paused;
                    _savedWeek = runner.State.week;
                    if (!Press(dash.Q<Button>("save-game"), "the header SAVE button")) return;
                    Next(Stage.VerifySave);
                    return;
                }

                case Stage.VerifySave:
                {
                    bool written = File.Exists(saveLoad.SavePath);
                    Debug.Log("PlayModeSmoke: SAVE wrote " + (written ? new FileInfo(saveLoad.SavePath).Length / 1024 + " KB" : "nothing"));
                    if (!Require(written, "SAVE did not write " + saveLoad.SavePath)) return;
                    runner.Step(20);
                    if (!Press(dash.Q<Button>("load-game"), "the header LOAD button")) return;
                    Next(Stage.PressLoad);
                    return;
                }

                case Stage.PressLoad:
                {
                    Label date = dash.Q<Label>("date-label");
                    Debug.Log("PlayModeSmoke: LOAD returned to week " + runner.State.week + " (saved at " + _savedWeek
                              + "), header reads '" + (date == null ? "" : date.text) + "'");
                    if (!Require(runner.State.week == _savedWeek, "LOAD put the game at week " + runner.State.week + ", not " + _savedWeek)) return;
                    if (!Require(runner.Speed == GameSpeed.Paused, "the game did not open paused after loading")) return;
                    Next(Stage.VerifyLoad);
                    return;
                }

                case Stage.VerifyLoad:
                {
                    // The world keeps running after a load - the reloaded state is live, not a picture of it.
                    int before = runner.State.week;
                    runner.Step(4);
                    if (!Require(runner.State.week == before + 4, "the loaded game does not advance")) return;
                    File.Delete(saveLoad.SavePath);
                    // The Regulatory drawer is still open over the data column - close it, as a player would.
                    if (!Press(Tab("open-regulatory"), "the Regulatory tab, to close it")) return;
                    if (!Press(Tab("open-monetary"), "the Monetary tab")) return;
                    Next(Stage.PressBuyback);
                    return;
                }

                case Stage.PressBuyback:
                {
                    VisualElement bonds = PanelRoot(FindPanel("Panel_MonetaryBonds"));
                    if (!Require(bonds != null, "the Monetary drawer is not in the scene")) return;
                    _debtBeforeBuyback = runner.State.bonds.debtStockBillions;
                    if (!Press(bonds.Q<Button>("buyback-execute"), "the BUY BACK button")) return;
                    Debug.Log("PlayModeSmoke: buyback committed $" + runner.Policy.buybackBillions.ToString("0")
                              + "B - " + bonds.Q<Label>("buyback-readout").text);
                    if (!Require(runner.Policy.buybackBillions > 0f, "the BUY BACK button committed nothing")) return;
                    runner.Step(13);
                    Next(Stage.VerifyBuyback);
                    return;
                }

                case Stage.VerifyBuyback:
                {
                    BondMarketState b = runner.State.bonds;
                    Debug.Log("PlayModeSmoke: buyback retired $" + b.lastBuybackFace.ToString("0") + "B face for $"
                              + b.lastBuybackCost.ToString("0") + "B at " + (b.lastBuybackPrice * 100f).ToString("0")
                              + "c; debt $" + _debtBeforeBuyback.ToString("0") + "B -> $" + b.debtStockBillions.ToString("0") + "B");
                    if (!Require(b.lastBuybackFace > 0f, "the quarter passed without the buyback executing")) return;
                    if (!Require(runner.Policy.buybackBillions == 0f, "the buyback order was not consumed")) return;
                    if (!Press(Tab("open-monetary"), "the Monetary tab, to close it")) return;
                    Next(Stage.HoverChart);
                    return;
                }

                case Stage.HoverChart:
                {
                    // The driver table is the default view now, so the chart is not on
                    // screen until a series tab asks for it. Pressing the tab is not
                    // enough on its own: an element that was display:None has no
                    // geometry until the panel lays out again, and a thing with no
                    // geometry cannot be picked. So press, leave the stage standing,
                    // and judge it on the next tick with a layout pass behind us.
                    if (dash.Q<VisualElement>("chart-container").style.display == DisplayStyle.None)
                    {
                        if (!Press(dash.Q<Button>("tab-gdp"), "the GDP tab, to bring the chart up")) return;
                        return;
                    }
                    LineChartComponent chart = dash.Q<LineChartComponent>();
                    if (!Require(chart != null && chart.PointCount > 2, "the dashboard chart has no data to hover")) return;
                    Rect area = chart.worldBound;
                    Vector2 point = new Vector2(area.xMin + area.width * 0.25f, area.center.y);
                    VisualElement under = chart.panel.Pick(point);
                    if (!Require(under == chart, "the chart is covered by " + (under == null ? "nothing pickable" : Describe(under)))) return;
                    Event move = new Event { type = EventType.MouseMove, mousePosition = point };
                    using (PointerMoveEvent e = PointerMoveEvent.GetPooled(move)) chart.SendEvent(e);
                    // Read it in the same frame: in batch mode there is no real mouse over the
                    // chart, so the panel sends a leave on its next update and the readout resets.
                    string hovered = dash.Q<Label>("chart-readout").text;
                    Debug.Log("PlayModeSmoke: hovering a quarter of the way along the chart reads '" + hovered + "'");
                    if (!Require(hovered.Contains("weeks ago"), "hovering the chart did not show that week's value")) return;

                    // GDD 18: a chart without numbers on it is a squiggle.
                    System.Collections.Generic.List<string> scale = new System.Collections.Generic.List<string>();
                    foreach (Label axis in chart.Query<Label>(className: "chart-value-label").ToList())
                        if (axis.style.display == DisplayStyle.Flex) scale.Add(axis.text);
                    string dates = "";
                    foreach (Label axis in chart.Query<Label>(className: "chart-date-label").ToList())
                        if (axis.style.display == DisplayStyle.Flex) dates += axis.text + " ";
                    Debug.Log("PlayModeSmoke: chart scale " + string.Join(" / ", scale) + "; dates " + dates);
                    if (!Require(scale.Count == 5 && scale[0] != scale[4] && scale[0].EndsWith("%"), "the chart has no value scale")) return;
                    if (!Require(dates.Contains("Q"), "the chart has no dates along the bottom")) return;
                    Next(Stage.VerifyHover);
                    return;
                }

                case Stage.VerifyHover:
                {
                    SteamManager steam = Object.FindAnyObjectByType<SteamManager>();
                    string presence = steam == null ? null : steam.LastPresence;
                    Debug.Log("PlayModeSmoke: Steam ("
                              + (steam == null ? "missing" : steam.Backend.Name) + ") presence '" + presence + "'");
                    if (!Require(!string.IsNullOrEmpty(presence) && presence.Contains("Approval"), "Steam rich presence was never set")) return;
                    if (!Press(dash.Q<Button>("view-toggle"), "the ALL METERS toggle")) return;
                    Next(Stage.PressMeters);
                    return;
                }

                case Stage.PressMeters:
                {
                    VisualElement meters = dash.Q<VisualElement>("meter-container");
                    VisualElement chart = dash.Q<VisualElement>("chart-container");
                    if (!Require(meters != null && chart != null, "the dashboard has no meter view")) return;
                    int bars = meters.childCount;
                    int filled = 0;
                    string first = "";
                    foreach (Label value in meters.Query<Label>(className: "meter-value").ToList())
                        if (!string.IsNullOrEmpty(value.text) && value.text != "--")
                        {
                            filled++;
                            if (first.Length == 0) first = value.text;
                        }
                    Debug.Log("PlayModeSmoke: meter view - " + bars + " bars, " + filled + " reading values, first " + first
                              + "; chart hidden " + (chart.style.display == DisplayStyle.None));
                    if (!Require(bars >= 10 && filled == bars, "the meter view built " + bars + " bars with " + filled + " values")) return;
                    if (!Require(chart.style.display == DisplayStyle.None, "the line chart is still drawn under the meters")) return;
                    Next(Stage.VerifyMeters);
                    return;
                }

                case Stage.VerifyMeters:
                {
                    // The bars have to be scaled to their own ranges, not all full or all empty.
                    VisualElement meters = dash.Q<VisualElement>("meter-container");
                    int distinct = 0;
                    float previous = -1f;
                    string widths = "";
                    foreach (VisualElement fill in meters.Query<VisualElement>(className: "meter-fill").ToList())
                    {
                        float percent = fill.style.height.value.value;
                        widths += percent.ToString("0") + "% ";
                        if (System.Math.Abs(percent - previous) > 0.5f) distinct++;
                        previous = percent;
                    }
                    Debug.Log("PlayModeSmoke: meter fills " + widths);

                    // Twelve columns must SHARE the width of the box, not overflow it.
                    VisualElement lastColumn = meters[meters.childCount - 1];
                    VisualElement firstColumn = meters[0];
                    Debug.Log("PlayModeSmoke: meter box " + meters.worldBound + ", column width "
                              + firstColumn.worldBound.width.ToString("0") + ", last column ends at "
                              + lastColumn.worldBound.xMax.ToString("0"));
                    if (!Require(lastColumn.worldBound.xMax <= meters.worldBound.xMax + 1f,
                                 "the last columns are off the right of the box - box ends at " + meters.worldBound.xMax.ToString("0")
                                 + ", last column at " + lastColumn.worldBound.xMax.ToString("0"))) return;
                    if (!Require(firstColumn.worldBound.width > 20f && firstColumn.worldBound.height > 80f,
                                 "the columns are " + firstColumn.worldBound.width.ToString("0") + "x"
                                 + firstColumn.worldBound.height.ToString("0") + ", too small to read")) return;
                    if (!Require(distinct >= 5, "the meter bars are not scaled to their own ranges")) return;
                    // Every bar must explain itself, and hovering one must show that text.
                    DashboardController dashboard = Object.FindAnyObjectByType<DashboardController>();
                    int explained = 0;
                    for (int i = 0; i < dashboard.MeterCount; i++)
                        if (dashboard.MeterTipFor(i).Length > 40) explained++;

                    using (PointerEnterEvent enter = PointerEnterEvent.GetPooled())
                    {
                        enter.target = meters[0];
                        meters[0].SendEvent(enter);
                    }
                    Label tipTitle = dash.Q<Label>("tooltip-title");
                    Label tipBody = dash.Q<Label>("tooltip-body");
                    VisualElement tip = dash.Q<VisualElement>("meter-tooltip");
                    Debug.Log("PlayModeSmoke: " + explained + "/" + dashboard.MeterCount + " meters explained; hover shows '"
                              + (tipTitle == null ? "" : tipTitle.text) + "' - '"
                              + (tipBody == null ? "" : tipBody.text.Substring(0, Mathf.Min(60, tipBody.text.Length))) + "...'");
                    if (!Require(explained == dashboard.MeterCount, explained + " of " + dashboard.MeterCount + " meters carry an explanation")) return;
                    if (!Require(tip != null && tip.style.display == DisplayStyle.Flex, "hovering a meter showed no tooltip")) return;
                    if (!Require(tipBody != null && tipBody.text.Length > 40, "the tooltip has no explanation in it")) return;


                    if (!Press(dash.Q<Button>("view-toggle"), "the SINGLE CHART toggle")) return;
                    if (!Require(dash.Q<VisualElement>("chart-container").style.display != DisplayStyle.None,
                                 "the chart did not come back when the view was toggled")) return;

                    // Hovering an approval bar must say WHY that group feels as it does.
                    VisualElement poorBar = dash.Q<VisualElement>("class-approval-list")[0];
                    VisualElement poorContent = poorBar.childCount > 0 ? poorBar[0] : poorBar;
                    using (PointerEnterEvent enter = PointerEnterEvent.GetPooled())
                    {
                        enter.target = poorContent;
                        poorContent.SendEvent(enter);
                    }
                    DashboardController board = Object.FindAnyObjectByType<DashboardController>();
                    Debug.Log("PlayModeSmoke: hovering the Poor bar reads '"
                              + board.MapTipText.Replace(System.Environment.NewLine, " | ").Replace("\n", " | ") + "'");
                    if (!Require(board.MapTipText.Contains("/100"), "the approval bar does not explain itself")) return;
                    board.HideMapTip();

                    // And the Sectors tab shows the industries, not one investment line.
                    if (!Press(dash.Q<Button>("tab-sectors"), "the Sectors tab")) return;
                    VisualElement industry = dash.Q<VisualElement>("industry-container");
                    int industries = 0;
                    string industryNames = "";
                    if (industry != null)
                        foreach (VisualElement row in industry.Children())
                            if (row.style.display != DisplayStyle.None)
                            {
                                industries++;
                                Label rowName = row.Q<Label>("breakdown-name");
                                industryNames += (rowName == null ? "?" : rowName.text) + "; ";
                            }
                    Debug.Log("PlayModeSmoke: Sectors tab shows " + industries + " industries - " + industryNames);
                    if (!Require(industries >= 6, "the Sectors tab shows " + industries + " industries")) return;
                    if (!Require(dash.Q<VisualElement>("chart-container").style.display == DisplayStyle.None,
                                 "the line chart is still drawn under the industry list")) return;

                    // The driver table: what is MOVING each headline number. It replaced
                    // the chart as the default view, so the chart must give way to it and
                    // every row must carry real text - an empty table reads as a working
                    // one, which is how the breakdown geometry checks once passed on
                    // nothing at all.
                    if (!Press(dash.Q<Button>("drivers-toggle"), "the WHAT'S MOVING toggle")) return;
                    VisualElement drivers = dash.Q<VisualElement>("driver-container");
                    if (!Require(drivers != null && drivers.style.display != DisplayStyle.None,
                                 "the driver table did not open")) return;
                    if (!Require(dash.Q<VisualElement>("chart-container").style.display == DisplayStyle.None,
                                 "the line chart is still drawn under the driver table")) return;

                    List<Label> headValues = drivers.Query<Label>(className: "driver-head-value").ToList();
                    List<Label> headNotes = drivers.Query<Label>(className: "driver-head-note").ToList();
                    int headlines = 0;
                    foreach (Label head in headValues) if (head.text.Length > 1) headlines++;
                    if (!Require(headlines == 3,
                                 "the driver table shows " + headlines + " of 3 headline figures")) return;

                    List<Label> names = drivers.Query<Label>(className: "driver-name").ToList();
                    List<Label> amounts = drivers.Query<Label>(className: "driver-value").ToList();
                    int filled = 0;
                    string listed = "";
                    for (int i = 0; i < names.Count && i < amounts.Count; i++)
                    {
                        if (names[i].text.Length < 4 || amounts[i].text.Length < 2) continue;
                        filled++;
                        listed += names[i].text + " " + amounts[i].text + "; ";
                    }
                    Debug.Log("PlayModeSmoke: driver table reads " + listed);
                    if (!Require(filled >= 5,
                                 "the driver table filled " + filled + " rows, so it is explaining almost nothing")) return;

                    // Growth is sticky, so the reading alone never shows a policy working.
                    // The headline must say where growth is HEADING as well as where it is.
                    string growthNote = headNotes.Count > 0 ? headNotes[0].text : "";
                    if (!Require(growthNote.Contains("heading to"),
                                 "the growth headline does not say where growth is heading: '" + growthNote + "'")) return;

                    // And a series tab must still take you back to the chart.
                    if (!Press(dash.Q<Button>("tab-gdp"), "the GDP tab")) return;
                    if (!Require(dash.Q<VisualElement>("chart-container").style.display != DisplayStyle.None,
                                 "the chart did not come back when a series tab was pressed")) return;

                    Next(Stage.VerifyCityscape);
                    return;
                }
            }
        }

        static Transform _shipProbe;
    }
}
