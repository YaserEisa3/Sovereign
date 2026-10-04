using UnityEngine;
using UnityEngine.UIElements;

namespace Sovereign.EditorTools
{
    /// <summary>Closing drawers every way a player would. These stages exist because
    /// every drawer once covered the tab bar and five X buttons were bound to nothing.</summary>
    public static partial class PlayModeSmoke
    {
        static void RunClosingStages(GameObject fiscal)
        {
            GameObject trade = FindPanel("Panel_TradeCurrency");

            switch (_stage)
            {
                case Stage.CloseFiscalWithX:
                    if (!Press(PanelRoot(fiscal).Q<Button>("close-panel"), "the Fiscal X button")) return;
                    Next(Stage.ReopenFiscal);
                    return;

                case Stage.ReopenFiscal:
                    if (!Require(!fiscal.activeSelf, "the Fiscal X button did not close the drawer")) return;
                    Debug.Log("PlayModeSmoke: X closed Fiscal.");
                    if (!Press(Tab("open-fiscal"), "the Fiscal tab (reopening)")) return;
                    Next(Stage.CloseFiscalWithTab);
                    return;

                case Stage.CloseFiscalWithTab:
                    if (!Require(fiscal.activeSelf, "the Fiscal tab did not reopen the drawer")) return;
                    // The tab bar has to stay clickable WITH the drawer open.
                    if (!Press(Tab("open-fiscal"), "the Fiscal tab while Fiscal is open")) return;
                    Next(Stage.OpenTrade);
                    return;

                case Stage.OpenTrade:
                    if (!Require(!fiscal.activeSelf, "clicking the open drawer's own tab did not close it")) return;
                    Debug.Log("PlayModeSmoke: clicking its tab again closed Fiscal.");
                    if (!Press(Tab("open-trade"), "the Trade tab")) return;
                    Next(Stage.CloseTradeWithX);
                    return;

                case Stage.CloseTradeWithX:
                    if (!Require(trade != null && trade.activeSelf, "the Trade tab did not open its drawer")) return;
                    if (!Press(PanelRoot(trade).Q<Button>("close-panel"), "the Trade X button")) return;
                    Next(Stage.OpenMonetary);
                    return;

                case Stage.OpenMonetary:
                    if (!Require(!trade.activeSelf, "the Trade X did not close it - an unbuilt drawer strands the player")) return;
                    Debug.Log("PlayModeSmoke: X closed Trade.");
                    if (!Press(Tab("open-monetary"), "the Monetary tab")) return;
                    Next(Stage.VerifyMonetary);
                    return;

                case Stage.VerifyMonetary:
                {
                    GameObject monetary = FindPanel("Panel_MonetaryBonds");
                    if (!Require(monetary != null && monetary.activeSelf, "the Monetary tab did not open it")) return;
                    VisualElement root = PanelRoot(monetary);
                    int controls = CountChildren(root, "monetary-list");
                    int maturity = CountChildren(root, "maturity-list");
                    int holders = CountChildren(root, "foreign-holder-list");
                    Debug.Log("PlayModeSmoke: monetary drawer - " + controls + " controls, "
                              + maturity + " maturity rows, " + holders + " holders");
                    if (!Require(controls == 4 && maturity == 3 && holders == 6,
                                 "monetary drawer built " + controls + "/" + maturity + "/" + holders + ", expected 4/3/6")) return;
                    _stage = Stage.OpenPopulation;
                    return;
                }

                default:
                    RunPhase2Stages();
                    return;
            }
        }
    }
}
