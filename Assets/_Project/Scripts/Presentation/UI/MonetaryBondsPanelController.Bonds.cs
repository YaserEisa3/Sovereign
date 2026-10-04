using System.Collections.Generic;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>Guidance, maturity strategy, the emergency cut, and the rows the debt
    /// profile and holder list are stamped into.</summary>
    public partial class MonetaryBondsPanelController
    {
        readonly List<VisualElement> _maturityRows = new List<VisualElement>();
        readonly List<VisualElement> _holderRows = new List<VisualElement>();
        readonly List<NationDefinition> _holders = new List<NationDefinition>();
        float _startingForeignTotal;

        void BindGuidance()
        {
            BindChoice("guidance-dovish", () => Policy.guidance = ForwardGuidance.Dovish);
            BindChoice("guidance-neutral", () => Policy.guidance = ForwardGuidance.Neutral);
            BindChoice("guidance-hawkish", () => Policy.guidance = ForwardGuidance.Hawkish);
        }

        void BindMaturity()
        {
            BindChoice("maturity-short", () => Policy.maturityPreference = BondMaturityPreference.ShortHeavy);
            BindChoice("maturity-balanced", () => Policy.maturityPreference = BondMaturityPreference.Balanced);
            BindChoice("maturity-long", () => Policy.maturityPreference = BondMaturityPreference.LongHeavy);
        }

        void BindChoice(string elementName, System.Action apply)
        {
            Button button = Root.Q<Button>(elementName);
            if (button == null) return;
            button.clicked += () => { apply(); Refresh(runner.State); };
        }

        void BindForeignCurrency()
        {
            Toggle toggle = Root.Q<Toggle>("foreign-currency-toggle");
            if (toggle == null) return;
            toggle.SetValueWithoutNotify(Policy.issueInForeignCurrency);
            toggle.RegisterValueChangedCallback(change =>
            {
                Policy.issueInForeignCurrency = change.newValue;
                Refresh(runner.State);
            });
        }

        void BindEmergencyCut()
        {
            Button button = Root.Q<Button>("emergency-cut");
            if (button == null) return;
            button.clicked += () =>
            {
                _emergencyNote = runner.RequestEmergencyRateCut()
                    ? "Emergency cut delivered. The market noticed you panicked."
                    : "Already used this year.";
                Refresh(runner.State);
            };
        }

        void BuildMaturityRows()
        {
            _maturityRows.Clear();
            VisualElement list = Root.Q<VisualElement>("maturity-list");
            if (list == null || maturityRowTemplate == null) return;
            list.Clear();

            foreach (string bucket in new[] { "Within 1 year", "1 - 5 years", "Beyond 5 years" })
            {
                VisualElement row = maturityRowTemplate.Instantiate();
                Label name = row.Q<Label>("maturity-bucket");
                if (name != null) name.text = bucket;
                list.Add(row);
                _maturityRows.Add(row);
            }
        }

        void BuildHolderRows()
        {
            _holderRows.Clear();
            _holders.Clear();
            _startingForeignTotal = 0f;

            VisualElement list = Root.Q<VisualElement>("foreign-holder-list");
            if (list == null || nationStatusRowTemplate == null) return;
            list.Clear();

            foreach (NationDefinition nation in database.Nations)
            {
                if (nation == null || nation.isPlayerNation || nation.bondHoldingPercent <= 0f) continue;
                _startingForeignTotal += nation.bondHoldingPercent;

                VisualElement row = nationStatusRowTemplate.Instantiate();
                Label name = row.Q<Label>("nation-name");
                if (name != null) name.text = nation.displayName;
                VisualElement swatch = row.Q<VisualElement>("nation-colour");
                if (swatch != null) swatch.style.backgroundColor = nation.nationColor;

                list.Add(row);
                _holderRows.Add(row);
                _holders.Add(nation);
            }
        }

        static void SetRowLabel(VisualElement row, string elementName, string text)
        {
            Label label = row.Q<Label>(elementName);
            if (label != null) label.text = text;
        }
    }
}
