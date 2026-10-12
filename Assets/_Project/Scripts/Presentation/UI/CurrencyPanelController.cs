using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 10. The exchange rate, the reserves behind it, and what defending it costs.
    ///
    /// This used to be the bottom half of the trade drawer, under everything to do with
    /// tariffs and agreements. They are different jobs: tariffs are a bargain struck with
    /// another country, the exchange rate is a position held against the whole market, and
    /// reading them as one list meant scrolling past six nations to find the reserves.
    /// </summary>
    public class CurrencyPanelController : DrawerPanelController
    {
        [Tooltip("Currency intervention, $B a quarter. Positive defends the currency with reserves, negative sells it to weaken it.")]
        [SerializeField] float interventionLimit = 300f;
        [SerializeField] float interventionStep = 25f;

        readonly List<PolicyStepper> _steppers = new List<PolicyStepper>();

        protected override void Build()
        {
            _steppers.Clear();

            VisualElement list = Root.Q<VisualElement>("intervention-list");
            if (list == null || policyRowTemplate == null) return;
            list.Clear();

            _steppers.Add(new PolicyStepper(policyRowTemplate, list, "Currency intervention per quarter").Bind(
                () => Policy.currencyIntervention,
                () => Policy.PendingScalar("currencyIntervention", Policy.currencyIntervention),
                v => Policy.QueueScalar("currencyIntervention", v, Policy.currencyIntervention),
                interventionStep, -interventionLimit, interventionLimit,
                v => v > 0f ? "defend $" + v.ToString("0") + "B" : v < 0f ? "sell $" + (-v).ToString("0") + "B" : "none",
                (c, v) => InterventionLine(v) + ",  " + (v > 0f ? "a doubted defence buys less"
                        : v < 0f ? "invites a manipulation charge" : "let the market decide"))
                .Resting(InterventionLine));
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

        protected override void Refresh(EconomyState state)
        {
            CurrencyState fx = state.currency;
            GeopoliticsState g = state.geopolitics;

            SetLabel("exchange-rate-index", "Currency index " + fx.exchangeRateIndex.ToString("0.0")
                     + "   real effective " + fx.realEffectiveExchangeRate.ToString("0.0"));
            SetLabel("fx-reserves", "FX reserves " + Billions(fx.fxReservesBillions)
                     + (g.swapBackstop > 0f ? "  + " + Billions(g.swapBackstop) + " in swap lines" : ""));
            SetLabel("reserve-burn", Policy.currencyIntervention > 0f
                ? "Burning " + Billions(Policy.currencyIntervention) + " a quarter defending the currency"
                : Policy.currencyIntervention < 0f
                    ? "Adding " + Billions(-Policy.currencyIntervention) + " a quarter by selling the currency"
                    : "No intervention");
            SetLabel("fdi-readout", "FDI inflow " + Billions(fx.fdiInflowRate)
                     + "/yr   trade volume " + state.tradeVolumeIndex.ToString("0"));
            SetLabel("competitiveness-readout", "Manufacturing competitiveness "
                     + fx.manufacturingCompetitiveness.ToString("0")
                     + "   capital sheltering offshore " + (g.capitalFlight * 100f).ToString("0") + "%");

            foreach (PolicyStepper stepper in _steppers) stepper.Refresh();
        }
    }
}
