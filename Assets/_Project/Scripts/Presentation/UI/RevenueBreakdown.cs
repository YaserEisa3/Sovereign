using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 8 and 18. Where the money comes from, as bars: one per revenue line,
    /// longest first, scaled against the biggest, with the long tail gathered into
    /// one. Used by the dashboard and by the Fiscal drawer - the same chart, so the
    /// headline and the desk can never disagree about what is paying for the state.
    /// </summary>
    public class RevenueBreakdown
    {
        readonly VisualElement _list;
        readonly VisualTreeAsset _template;
        readonly Label _note;
        readonly float _smallShare;
        readonly List<VisualElement> _rows = new List<VisualElement>();
        readonly List<string> _rowKeys = new List<string>();

        /// <summary>Display names by revenue key, so a bar reads "VAT / Sales Tax"
        /// rather than the internal "ValueAdded".</summary>
        readonly Dictionary<string, string> _names = new Dictionary<string, string>();
        readonly Dictionary<string, string> _descriptions = new Dictionary<string, string>();
        System.Action<string, string, Vector3> _onHover;
        System.Action _onLeave;

        /// <summary>Which side of the budget this chart is drawing.</summary>
        public enum Side { Revenue, Spending }

        readonly Side _side;

        public RevenueBreakdown(VisualElement list, VisualTreeAsset template, Label note, float smallShare,
                                Sovereign.Data.TaxDefinition[] taxes)
            : this(list, template, note, smallShare, taxes, null, Side.Revenue) { }

        public RevenueBreakdown(VisualElement list, VisualTreeAsset template, Label note, float smallShare,
                                Sovereign.Data.TaxDefinition[] taxes,
                                Sovereign.Data.SpendingCategoryDefinition[] spending, Side side)
        {
            _list = list;
            _template = template;
            _note = note;
            _smallShare = smallShare;
            _side = side;

            if (spending != null)
                foreach (Sovereign.Data.SpendingCategoryDefinition line in spending)
                {
                    if (line == null) continue;
                    string spendKey = PolicyBootstrap.KeyFor(line, "SO_Spend_");
                    _names[spendKey] = line.displayName;
                    _descriptions[spendKey] = line.description;
                }
            _names["ExportSubsidies"] = "Export subsidies";
            _descriptions["ExportSubsidies"] = "Money paid to exporters to hold prices down abroad. Set it in the "
                                             + "Trade drawer; above a threshold it draws a WTO complaint.";

            if (taxes != null)
                foreach (Sovereign.Data.TaxDefinition tax in taxes)
                {
                    if (tax == null) continue;
                    string key = PolicyBootstrap.KeyFor(tax, "SO_Tax_");
                    _names[key] = tax.displayName;
                    _descriptions[key] = tax.description;
                }
            _names["NationTariffs"] = "Tariffs by nation";
            _descriptions["NationTariffs"] = "Tariffs aimed at particular countries, charged on that nation's share "
                                           + "of your imports. Set them in the Trade drawer.";
            _descriptions["Everything else"] = "The small lines gathered together - each raises less than 2% of "
                                             + "revenue on its own. Open the Fiscal drawer to see them.";
        }

        /// <summary>The display name for a line, so anything else showing the same lines
        /// names them identically - the driver table shares this map.</summary>
        public string NameFor(string key)
        {
            string found;
            return _names.TryGetValue(key, out found) ? found : Spaced(key);
        }

        /// <summary>Hovering a bar explains what that tax is - the chart names lines the
        /// player has never had to think about before.</summary>
        public void OnHover(System.Action<string, string, Vector3> show, System.Action hide)
        {
            _onHover = show;
            _onLeave = hide;
        }

        public bool Ready { get { return _list != null && _template != null; } }
        public int VisibleRows { get; private set; }

        public void Refresh(EconomyState state)
        {
            if (!Ready || state == null) return;

            Dictionary<string, float> source = _side == Side.Revenue
                ? state.treasury.revenueByLine
                : state.treasury.spendingByLine;

            float total = 0f, other = 0f;
            foreach (KeyValuePair<string, float> line in source)
                if (line.Value > 0f) total += line.Value;          // a tax credit is spending, not income

            List<KeyValuePair<string, float>> lines = new List<KeyValuePair<string, float>>();
            foreach (KeyValuePair<string, float> line in source)
            {
                if (line.Value <= 0f) continue;
                if (total > 0f && line.Value / total < _smallShare) { other += line.Value; continue; }
                lines.Add(line);
            }
            lines.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (other > 0f) lines.Add(new KeyValuePair<string, float>("Everything else", other));

            while (_rows.Count < lines.Count)
            {
                VisualElement row = _template.Instantiate();
                VisualElement content = row.childCount > 0 ? row[0] : row;
                content.AddToClassList("breakdown-row");
                if (_side == Side.Spending)
                {
                    VisualElement bar = content.Q<VisualElement>("breakdown-fill");
                    if (bar != null) bar.AddToClassList("spending");
                }

                int index = _rows.Count;
                content.RegisterCallback<PointerEnterEvent>(e => Explain(index, e.position));
                content.RegisterCallback<PointerLeaveEvent>(e => { if (_onLeave != null) _onLeave(); });

                _list.Add(row);
                _rows.Add(row);
                _rowKeys.Add("");
            }

            float biggest = lines.Count > 0 ? lines[0].Value : 1f;
            for (int i = 0; i < _rows.Count; i++)
            {
                VisualElement row = _rows[i];
                bool used = i < lines.Count;
                row.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (!used) continue;

                Label name = row.Q<Label>("breakdown-name");
                Label value = row.Q<Label>("breakdown-value");
                Label share = row.Q<Label>("breakdown-share");
                VisualElement fill = row.Q<VisualElement>("breakdown-fill");

                _rowKeys[i] = lines[i].Key;
                if (name != null) name.text = NameFor(lines[i].Key);
                if (value != null) value.text = Money(lines[i].Value);
                if (share != null) share.text = total <= 0f ? "" : (lines[i].Value / total * 100f).ToString("0") + "%";
                if (fill != null)
                    fill.style.width = Length.Percent(Mathf.Clamp01(lines[i].Value / Mathf.Max(0.01f, biggest)) * 100f);
            }
            VisibleRows = lines.Count;

            if (_note != null)
                _note.text = Money(total) + " a year across " + lines.Count + " lines, "
                             + (total / Mathf.Max(1f, state.nominalGdpBillions) * 100f).ToString("0.0") + "% of GDP"
                             + (_side == Side.Spending
                                ? ",  " + Money(Mathf.Abs(state.revenueBillions - state.spendingBillions))
                                  + (state.revenueBillions >= state.spendingBillions ? " surplus" : " borrowed")
                                : "");
        }

        /// <summary>What the hovered bar is, in words.</summary>
        void Explain(int row, Vector3 position)
        {
            if (_onHover == null || row < 0 || row >= _rowKeys.Count) return;
            string key = _rowKeys[row];
            if (string.IsNullOrEmpty(key)) return;

            string description;
            if (!_descriptions.TryGetValue(key, out description) || string.IsNullOrEmpty(description)) description = "";
            _onHover(NameFor(key), description, position);
        }

        static string Money(float billions) { return "$" + billions.ToString("#,0") + "B"; }

        /// <summary>CorporateIncome reads as Corporate Income.</summary>
        static string Spaced(string key)
        {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && char.IsUpper(key[i]) && !char.IsUpper(key[i - 1])) text.Append(' ');
                text.Append(key[i]);
            }
            return text.ToString();
        }
    }
}
