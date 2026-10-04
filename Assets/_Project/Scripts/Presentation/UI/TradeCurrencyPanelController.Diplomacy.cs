using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>The diplomacy rows, and the weekly refresh.</summary>
    public partial class TradeCurrencyPanelController
    {
        void BuildDiplomacy()
        {
            VisualElement list = Clear("agreement-list");
            if (list == null || diplomacyRowTemplate == null) return;

            NationProfile[] nations = runner.Simulator.Geopolitics.Config.nations;
            for (int i = 0; i < nations.Length; i++)
            {
                if (nations[i].isPlayer) continue;
                VisualElement element = diplomacyRowTemplate.Instantiate();
                list.Add(element);

                DiplomacyRow row = new DiplomacyRow
                {
                    nation = nations[i].name,
                    index = i,
                    relationship = element.Q<Label>("nation-relationship"),
                    status = element.Q<Label>("nation-status"),
                    aid = element.Q<Label>("aid-value"),
                    agreement = element.Q<Button>("toggle-agreement"),
                    sanction = element.Q<Button>("toggle-sanction"),
                    swap = element.Q<Button>("toggle-swap")
                };
                Label name = element.Q<Label>("nation-name");
                if (name != null) name.text = row.nation;
                VisualElement swatch = element.Q<VisualElement>("nation-colour");
                if (swatch != null && i < database.Nations.Length) swatch.style.backgroundColor = database.Nations[i].nationColor;
                if (row.sanction != null) row.sanction.AddToClassList("hostile");

                BindToggle(row.agreement, row.nation, PolicyState.NationAgreement);
                BindToggle(row.sanction, row.nation, PolicyState.NationSanction);
                BindToggle(row.swap, row.nation, PolicyState.NationSwapLine);
                BindAid(element.Q<Button>("aid-decrease"), row.nation, -aidStep);
                BindAid(element.Q<Button>("aid-increase"), row.nation, aidStep);
                _rows.Add(row);
            }
        }

        void BindToggle(Button button, string nation, string field)
        {
            if (button == null) return;
            button.clicked += () =>
            {
                float pending = Policy.PendingNation(field, nation);
                Policy.QueueNation(field, nation, pending >= 0.5f ? 0f : 1f);
                Refresh(runner.State);
            };
        }

        void BindAid(Button button, string nation, float delta)
        {
            if (button == null) return;
            button.clicked += () =>
            {
                float next = Mathf.Clamp01(Policy.PendingNation(PolicyState.NationAidShare, nation) + delta);
                Policy.QueueNation(PolicyState.NationAidShare, nation, Mathf.Round(next * 100f) / 100f);
                Refresh(runner.State);
            };
        }

        protected override void Refresh(EconomyState state)
        {
            foreach (PolicyStepper stepper in _steppers) stepper.Refresh();

            GeopoliticsState g = state.geopolitics;
            GeopoliticsTuning t = runner.Simulator.Geopolitics.Config.tuning;
            string retaliation = "";

            foreach (DiplomacyRow row in _rows)
            {
                NationState n = g.nations[row.index];
                if (row.relationship != null)
                {
                    row.relationship.text = n.relationship.ToString("+0;-0");
                    if (theme != null)
                        row.relationship.style.color = n.relationship >= 30f ? theme.growth : n.relationship >= 0f ? theme.neutralData
                                                     : n.relationship >= -40f ? theme.warning : theme.danger;
                }

                string status = "";
                if (n.theirTariffOnYou > 0.5f) status += "tariffs you " + n.theirTariffOnYou.ToString("0") + "%  ";
                if (!n.buyingBonds) status += "NOT BUYING YOUR BONDS  ";
                if (n.sanctioningYou) status += "SANCTIONING YOU  ";
                if (Policy.NationFlag(PolicyState.NationSwapLine, row.nation) && n.relationship < t.swapLineMinimumRelationship)
                    status += "swap line refused  ";
                if (row.status != null) row.status.text = status.Length == 0 ? "normal relations" : status.Trim();
                if (n.theirTariffOnYou > 0.5f) retaliation += row.nation + " " + n.theirTariffOnYou.ToString("0") + "%   ";

                ShowToggle(row.agreement, row.nation, PolicyState.NationAgreement);
                ShowToggle(row.sanction, row.nation, PolicyState.NationSanction);
                ShowToggle(row.swap, row.nation, PolicyState.NationSwapLine);
                if (row.aid != null)
                {
                    float share = Policy.PendingNation(PolicyState.NationAidShare, row.nation);
                    row.aid.text = "$" + (Policy.Spending("ForeignAid") * share).ToString("0") + "B";
                }
            }

            SetLabel("retaliation-warning", retaliation.Length == 0 ? "No one is tariffing your exports."
                                                                   : "Tariffs on your exports: " + retaliation.Trim());
            RefreshCurrency(state, g);
        }

        void ShowToggle(Button button, string nation, string field)
        {
            if (button == null) return;
            bool live = Policy.NationFlag(field, nation);
            bool pending = Policy.PendingNation(field, nation) >= 0.5f;
            if (pending) button.AddToClassList("on"); else button.RemoveFromClassList("on");
            if (live != pending) button.AddToClassList("queued"); else button.RemoveFromClassList("queued");
        }

        void RefreshCurrency(EconomyState state, GeopoliticsState g)
        {
            CurrencyState fx = state.currency;
            SetLabel("exchange-rate-index", "Currency index " + fx.exchangeRateIndex.ToString("0.0")
                     + "   real effective " + fx.realEffectiveExchangeRate.ToString("0.0"));
            VisualElement rates = Root.Q<VisualElement>("exchange-rate-list");
            if (rates != null) rates.Clear();
            SetLabel("fx-reserves", "FX reserves " + Billions(fx.fxReservesBillions)
                     + (g.swapBackstop > 0f ? "  + " + Billions(g.swapBackstop) + " in swap lines" : ""));
            SetLabel("reserve-burn", Policy.currencyIntervention > 0f
                ? "Burning " + Billions(Policy.currencyIntervention) + " a quarter defending the currency"
                : Policy.currencyIntervention < 0f ? "Adding " + Billions(-Policy.currencyIntervention) + " a quarter by selling the currency"
                : "No intervention");
            SetLabel("fdi-readout", "FDI inflow " + Billions(fx.fdiInflowRate) + "/yr   trade volume " + state.tradeVolumeIndex.ToString("0"));
            SetLabel("competitiveness-readout", "Manufacturing competitiveness " + fx.manufacturingCompetitiveness.ToString("0")
                     + "   capital sheltering offshore " + (g.capitalFlight * 100f).ToString("0") + "%");
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class TradeCurrencyPanelController
    {
        /// <summary>
        /// GDD 13. What a tariff on this nation actually means: the imports it is
        /// charged on, the money it would raise, and what it costs you in relations.
        /// A percentage with no base behind it is not a decision anyone can make.
        /// </summary>
        string TariffImpact(Sovereign.Core.NationProfile nation, float pending, float current, float threshold)
        {
            Sovereign.Core.TreasuryModel treasury = runner.Simulator.Treasury;
            float imports = treasury.NationImportBaseBillions(runner.State, nation.tradeVolume);
            float raised = treasury.NationTariffRevenue(runner.State, nation.tradeVolume, pending);
            float now = treasury.NationTariffRevenue(runner.State, nation.tradeVolume, current);

            // Even at zero the row names the base: that is the number a rate would be
            // charged on, and it is the whole question the player is asking.
            string money = pending.ToString("0") + "% of " + Billions(imports) + " imports = "
                           + Billions(raised) + "/yr"
                           + (Mathf.Abs(raised - now) >= 0.5f ? " (" + SignedBillions(raised - now) + ")" : "");

            float relationship = -(pending - current) * runner.Simulator.Geopolitics.Config.tuning.tariffRelationshipPenalty;
            // And what one more point of tariff would be worth, which is the question
            // anyone standing on a stepper is actually asking.
            float perPoint = treasury.NationTariffRevenue(runner.State, nation.tradeVolume, 1f);

            return money + ",  +1% = " + SignedBillions(perPoint) + "/yr,  relationship "
                   + relationship.ToString("+0;-0")
                   + (pending > threshold ? ", they WILL retaliate" : "");
        }
    }
}

namespace Sovereign.Presentation
{
    public partial class TradeCurrencyPanelController
    {
        /// <summary>
        /// GDD 13. The money lines for the trade desk's own controls. Every stepper in
        /// the game states what it is charged on and what one more step is worth; these
        /// three were the last ones showing a bare number.
        /// </summary>
        string GlobalTariffLine(float rate, float step)
        {
            Sovereign.Core.TreasuryModel treasury = runner.Simulator.Treasury;
            float imports = treasury.LineBaseBillions(Sovereign.Core.TaxKeys.ImportTariff, runner.State);
            if (imports <= 0.5f) return "";

            float raised = treasury.EstimateLine(Sovereign.Core.TaxKeys.ImportTariff, rate, runner.State);
            float next = treasury.EstimateLine(Sovereign.Core.TaxKeys.ImportTariff, rate + step, runner.State) - raised;

            return rate.ToString("0") + "% of " + Billions(imports) + " imports = " + Billions(raised)
                   + "/yr,  next step " + SignedBillions(next) + "/yr";
        }

        string SubsidyLine(float billions, float step)
        {
            float share = billions / Mathf.Max(1f, runner.State.nominalGdpBillions) * 100f;
            return "costs " + Billions(billions) + "/yr, " + share.ToString("0.0")
                   + "% of GDP,  next step " + SignedBillions(step) + "/yr";
        }

        string InterventionLine(float billionsPerQuarter)
        {
            float reserves = runner.State.currency.fxReservesBillions;
            if (billionsPerQuarter == 0f)
                return "reserves " + Billions(reserves) + ", untouched";

            float yearly = billionsPerQuarter * 4f;
            float quarters = billionsPerQuarter > 0f ? reserves / Mathf.Max(0.01f, billionsPerQuarter) : 0f;
            return (billionsPerQuarter > 0f ? "spends " : "buys ") + Billions(Mathf.Abs(yearly))
                   + "/yr from reserves of " + Billions(reserves)
                   + (billionsPerQuarter > 0f ? " - " + quarters.ToString("0") + " quarters of ammunition" : "");
        }
    }
}
