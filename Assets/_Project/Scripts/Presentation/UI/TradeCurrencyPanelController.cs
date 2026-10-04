using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 10 and 13. Tariffs global and per nation, export subsidies, trade
    /// agreements, sanctions, swap lines, aid, and currency intervention. Every
    /// switch queues to the quarter like everything else, and each row says what
    /// the other side will do about it.
    /// </summary>
    public partial class TradeCurrencyPanelController : DrawerPanelController
    {
        [Header("Templates")]
        [SerializeField] VisualTreeAsset diplomacyRowTemplate;

        [Header("Control ranges - GDD 10 and 13")]
        [SerializeField] float nationTariffMax = 50f;
        [SerializeField] float nationTariffStep = 1f;
        [SerializeField] float exportSubsidyMax = 200f;
        [SerializeField] float exportSubsidyStep = 5f;
        [Tooltip("Currency intervention, $B a quarter. Positive defends the currency with reserves, negative sells it to weaken it.")]
        [SerializeField] float interventionLimit = 300f;
        [SerializeField] float interventionStep = 25f;
        [Tooltip("Share of the foreign aid budget one press moves toward or away from a nation.")]
        [SerializeField] float aidStep = 0.05f;

        readonly List<PolicyStepper> _steppers = new List<PolicyStepper>();
        readonly List<DiplomacyRow> _rows = new List<DiplomacyRow>();

        class DiplomacyRow
        {
            public string nation;
            public int index;
            public Label relationship, status, aid;
            public Button agreement, sanction, swap;
        }

        protected override void Build()
        {
            _steppers.Clear();
            _rows.Clear();
            BuildTariffs();
            BuildNationTariffs();
            BuildDiplomacy();
            BuildIntervention();
        }

        void BuildTariffs()
        {
            VisualElement list = Clear("tariff-list");
            if (list == null || policyRowTemplate == null) return;

            TaxDefinition tariff = FindTax("SO_Tax_ImportTariff");
            if (tariff != null)
                _steppers.Add(new PolicyStepper(policyRowTemplate, list, "Global import tariff").Bind(
                    () => Policy.Tax(TaxKeys.ImportTariff), () => Policy.PendingTax(TaxKeys.ImportTariff),
                    v => Policy.QueueTax(TaxKeys.ImportTariff, v),
                    tariff.stepSize, tariff.minimumValue, tariff.maximumValue, v => v.ToString("0") + "%",
                    (c, v) => GlobalTariffLine(v, tariff.stepSize)
                              + ",  the aggressive nations answer anything above their threshold")
                    .Resting(v => GlobalTariffLine(v, tariff.stepSize)));

            _steppers.Add(new PolicyStepper(policyRowTemplate, list, "Export subsidy").Bind(
                () => Policy.exportSubsidyBillions,
                () => Policy.PendingScalar("exportSubsidy", Policy.exportSubsidyBillions),
                v => Policy.QueueScalar("exportSubsidy", v, Policy.exportSubsidyBillions),
                exportSubsidyStep, 0f, exportSubsidyMax, v => "$" + v.ToString("0") + "B",
                (c, v) => SubsidyLine(v, exportSubsidyStep)
                          + (v > runner.Simulator.Geopolitics.Config.tuning.exportSubsidyWtoThreshold
                             ? ",  big enough to draw a WTO complaint" : ""))
                .Resting(v => SubsidyLine(v, exportSubsidyStep)));
        }

        void BuildNationTariffs()
        {
            VisualElement list = Clear("nation-tariff-list");
            if (list == null || policyRowTemplate == null) return;

            NationProfile[] nations = runner.Simulator.Geopolitics.Config.nations;
            for (int i = 0; i < nations.Length; i++)
            {
                if (nations[i].isPlayer) continue;
                int i2 = i;                       // captured by the row's closures
                string name = nations[i].name;
                float threshold = nations[i].tariffRetaliationThreshold;

                _steppers.Add(new PolicyStepper(policyRowTemplate, list, "Tariff on " + name).Bind(
                    () => Policy.Nation(PolicyState.NationTariff, name),
                    () => Policy.PendingNation(PolicyState.NationTariff, name),
                    v => Policy.QueueNation(PolicyState.NationTariff, name, v),
                    nationTariffStep, 0f, nationTariffMax, v => v.ToString("0") + "%",
                    (c, v) => TariffImpact(nations[i2], v, c, threshold))
                    .Resting(v => TariffImpact(nations[i2], v, v, threshold)));
            }
        }

        void BuildIntervention()
        {
            VisualElement list = Clear("intervention-list");
            if (list == null || policyRowTemplate == null) return;

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

        VisualElement Clear(string name)
        {
            VisualElement element = Root.Q<VisualElement>(name);
            if (element != null) element.Clear();
            return element;
        }

        TaxDefinition FindTax(string assetName)
        {
            foreach (TaxDefinition tax in database.Taxes) if (tax != null && tax.name == assetName) return tax;
            return null;
        }
    }
}
