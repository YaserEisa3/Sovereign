using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>The weekly refresh for the population drawer.</summary>
    public partial class PopulationPanelController
    {
        protected override void Refresh(EconomyState state)
        {
            foreach (PolicyStepper stepper in _steppers) stepper.Refresh();

            PopulationState p = state.population;
            float[] bands = { p.youth, p.workingAge, p.retired };
            for (int i = 0; i < _bands.Count && i < bands.Length; i++)
            {
                float share = p.Total <= 0f ? 0f : bands[i] / p.Total;
                SetRow(_bands[i], "band-count", bands[i].ToString("0.0") + "M");
                SetRow(_bands[i], "band-share", (share * 100f).ToString("0") + "%");
                SetRow(_bands[i], "band-change", "");
                SetWidth(_bands[i], "band-fill", share);
            }

            SetLabel("dependency-ratio", "Dependency ratio " + p.DependencyRatio.ToString("0.00")
                     + " - dependants per working-age adult");
            SetLabel("births-readout", "Births " + p.birthsPerYear.ToString("0.00") + "M/yr");
            SetLabel("deaths-readout", "Deaths " + p.deathsPerYear.ToString("0.00") + "M/yr");
            SetLabel("migration-readout", "Net migration " + p.netMigrationPerYear.ToString("0.0") + "M/yr");
            SetLabel("net-change-readout", "Population " + p.Total.ToString("0.0") + "M ("
                     + (p.birthsPerYear - p.deathsPerYear + p.netMigrationPerYear).ToString("+0.00;-0.00") + "M/yr)");

            if (_chart != null) _chart.SetValues(state.Series("population").Values, 520);

            for (int i = 0; i < _sectorRows.Count && i < state.sectors.Count; i++)
            {
                EconomicSector sector = state.sectors[i];
                VisualElement row = _sectorRows[i];
                SetRow(row, "sector-headcount", sector.headcount.ToString("0.0") + "M");
                SetRow(row, "sector-wage", "$" + (sector.averageWage / 1000f).ToString("0") + "k");
                SetRow(row, "sector-wage-bill", Billions(sector.WageBillBillions));
                SetRow(row, "sector-trend", "health " + sector.health.ToString("0"));
                SetWidth(row, "sector-health-fill", sector.health / 100f);
            }

            SetLabel("labour-force-readout", "Labour force " + p.laborForce.ToString("0.0") + "M");
            SetLabel("participation-readout", "Participation " + state.participationRate.ToString("0") + "% of working age");
            SetLabel("taxable-pool-readout", "Taxable income pool " + Billions(p.taxableIncomePool));

            float veteranCost;
            state.treasury.spendingByLine.TryGetValue(SpendKeys.VeteransBenefits, out veteranCost);
            SetLabel("veteran-count", p.veterans.ToString("0.00") + " million veterans");
            SetLabel("veteran-cost", "Veterans benefits " + Billions(veteranCost) + "/yr - set by the veteran count, not by you");

            float[] unrest = { state.approval.unrestPoor, state.approval.unrestMiddle, state.approval.unrestWealthy };
            for (int i = 0; i < _unrestRows.Count && i < unrest.Length; i++)
            {
                SetRow(_unrestRows[i], "approval-value", unrest[i].ToString("0"));
                SetRow(_unrestRows[i], "approval-trend", unrest[i] >= 90f ? "REVOLT" : "");
                SetWidth(_unrestRows[i], "approval-fill", unrest[i] / 100f);
                VisualElement fill = _unrestRows[i].Q<VisualElement>("approval-fill");
                if (fill != null && theme != null)
                    fill.style.backgroundColor = unrest[i] >= 60f ? theme.danger : unrest[i] >= 25f ? theme.warning : theme.growth;
            }

            SetLabel("gini-readout", "Gini " + state.giniCoefficient.ToString("0.000") + " - the Left watches this");
            SetLabel("immigration-cost-note",
                "Immigration grows the workforce and potential growth, and pushes down Services and Agriculture wages. "
                + "Potential growth is " + runner.Simulator.Potential(state).ToString("0.00") + "% a year.");
        }

        static void SetRow(VisualElement row, string name, string text)
        {
            Label label = row.Q<Label>(name);
            if (label != null) label.text = text;
        }

        static void SetWidth(VisualElement row, string name, float fraction)
        {
            VisualElement element = row.Q<VisualElement>(name);
            if (element != null) element.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
        }
    }
}
