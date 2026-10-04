using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Phase 5 through the UI: the Trade drawer's tariffs and diplomacy, the
    /// Regulatory drawer, and nations on the map reporting their live standing.</summary>
    public static partial class PlayModeSmoke
    {
        static void RunPhase5Stages()
        {
            SimulationRunner runner = Object.FindAnyObjectByType<SimulationRunner>();
            GameObject trade = FindPanel("Panel_TradeCurrency");

            switch (_stage)
            {
                case Stage.VerifyTrade:
                {
                    if (!Require(trade != null && trade.activeSelf, "the Trade tab did not open it")) return;
                    VisualElement root = PanelRoot(trade);
                    int tariffs = CountChildren(root, "tariff-list");
                    int nationTariffs = CountChildren(root, "nation-tariff-list");
                    int diplomacy = CountChildren(root, "agreement-list");
                    int intervention = CountChildren(root, "intervention-list");
                    Debug.Log("PlayModeSmoke: trade drawer - " + tariffs + " tariff rows, " + nationTariffs + " nation tariffs, "
                              + diplomacy + " diplomacy rows, " + intervention + " intervention");
                    if (!Require(tariffs == 2 && nationTariffs == 6 && diplomacy == 6 && intervention == 1,
                                 "trade drawer built " + tariffs + "/" + nationTariffs + "/" + diplomacy + "/" + intervention + ", expected 2/6/6/1")) return;

                    // A tariff has to say what it is charged on and what it raises.
                    Label tariffImpact = null;
                    foreach (Label label in root.Q<VisualElement>("nation-tariff-list")[0].Query<Label>().ToList())
                        if (label.text.Contains("imports") || label.text.Contains("raises nothing")) tariffImpact = label;
                    Debug.Log("PlayModeSmoke: tariff row reads '" + (tariffImpact == null ? "nothing" : tariffImpact.text) + "'");
                    if (!Require(tariffImpact != null, "a tariff row never says what the rate is charged on")) return;

                    // Every row on this desk prices itself, including the global tariff and
                    // the subsidy - those two sat there as bare numbers for a long time.
                    string silent = "";
                    foreach (string listName in new[] { "tariff-list", "nation-tariff-list", "intervention-list" })
                    {
                        VisualElement rows = root.Q<VisualElement>(listName);
                        if (rows == null) continue;
                        foreach (VisualElement row in rows.Children())
                        {
                            Label impact = row.Q<Label>("policy-impact");
                            Label rowName = row.Q<Label>("policy-name");
                            if (impact == null || impact.text.Length < 8)
                                silent += (rowName == null ? listName : rowName.text) + "; ";
                        }
                    }
                    Debug.Log("PlayModeSmoke: trade rows with no money line: " + (silent.Length == 0 ? "none" : silent));
                    if (!Require(silent.Length == 0, "these rows show no money: " + silent)) return;
                    _stage = Stage.PressDiplomacy;
                    return;
                }

                case Stage.PressDiplomacy:
                {
                    VisualElement root = PanelRoot(trade);
                    if (!Press(root.Q<VisualElement>("agreement-list")[0].Q<Button>("toggle-agreement"), "the first nation's Agreement toggle")) return;
                    if (!Press(root.Q<VisualElement>("nation-tariff-list")[0].Q<Button>("policy-increase"), "the first nation-tariff + button")) return;
                    Next(Stage.VerifyDiplomacy);
                    return;
                }

                case Stage.VerifyDiplomacy:
                {
                    string first = runner.Simulator.Geopolitics.Config.nations[1].name;
                    VisualElement root = PanelRoot(trade);
                    Button toggle = root.Q<VisualElement>("agreement-list")[0].Q<Button>("toggle-agreement");
                    bool queuedAgreement = runner.Policy.HasPendingNation(PolicyState.NationAgreement, first);
                    bool queuedTariff = runner.Policy.HasPendingNation(PolicyState.NationTariff, first);
                    Debug.Log("PlayModeSmoke: diplomacy - agreement queued " + queuedAgreement + " (toggle marked "
                              + toggle.ClassListContains("queued") + "), tariff queued " + queuedTariff + " for " + first);
                    if (!Require(queuedAgreement && toggle.ClassListContains("queued"), "the Agreement toggle did not queue, or does not show it")) return;
                    if (!Require(queuedTariff, "the nation tariff + did not queue")) return;
                    if (!Press(Tab("open-regulatory"), "the Regulatory tab")) return;
                    Next(Stage.VerifyRegulatory);
                    return;
                }

                case Stage.VerifyRegulatory:
                {
                    GameObject regulatory = FindPanel("Panel_Regulatory");
                    if (!Require(regulatory != null && regulatory.activeSelf, "the Regulatory tab did not open it")) return;
                    VisualElement root = PanelRoot(regulatory);
                    int financial = CountChildren(root, "financial-regulation-list");
                    int environment = CountChildren(root, "environment-regulation-list");
                    int labour = CountChildren(root, "labour-regulation-list");
                    int subsidies = CountChildren(root, "subsidy-list");

                    GameObject euroland = GameObject.Find("Nation_Euroland");
                    TextMesh status = euroland == null ? null : euroland.transform.Find("StatusLabel").GetComponent<TextMesh>();
                    Debug.Log("PlayModeSmoke: regulatory drawer - " + financial + "/" + environment + "/" + labour + "/" + subsidies
                              + "; Euroland on the map reads '" + (status == null ? "" : status.text) + "'");

                    if (!Require(financial == 1 && environment == 1 && labour == 1 && subsidies >= 2,
                                 "regulatory drawer built " + financial + "/" + environment + "/" + labour + "/" + subsidies)) return;
                    if (!Require(status != null && status.text.StartsWith("REL "), "nations on the map are not showing their live relationship")) return;
                    Next(Stage.VerifyShips);
                    return;
                }

                default:
                    RunPhase5bStages(runner);
                    RunCityscapeStages(runner);
                    return;
            }
        }
    }
}
