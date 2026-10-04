using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Data
{
    /// <summary>
    /// GDD 8.1 / 11. One asset per revenue line. The range, the step and who bears
    /// the burden all live here so a tax can be rebalanced without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Tax_", menuName = "Sovereign/Tax Definition")]
    public class TaxDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Tax";
        [TextArea(2, 4)] public string description = "";
        [Tooltip("Order this row appears in on the Fiscal panel.")]
        public int sortOrder = 0;

        [Header("Control - GDD 18, steppers not sliders")]
        public PolicyUnit unit = PolicyUnit.Percent;
        public float minimumValue = 0f;
        public float maximumValue = 55f;
        public float defaultValue = 21f;
        [Tooltip("How much one press of the stepper moves this control.")]
        public float stepSize = 1f;

        [Header("Revenue")]
        [Tooltip("The pool this rate is levied against.")]
        public RevenueBase revenueBase = RevenueBase.CorporateProfits;

        [Tooltip("Share of that pool this line applies to. The four income brackets split the taxable income pool between them; everything else is 1.")]
        [Range(0f, 1f)] public float baseShare = 1f;

        [Tooltip("Share of the theoretical base actually captured, 0-1. Collection efficiency, avoidance and carve-outs.")]
        [Range(0f, 1f)] public float collectionEfficiency = 0.85f;

        [Tooltip("How fast the base shrinks as the rate rises - the Laffer term. 0 means the base is unresponsive, 1 means highly elastic (capital gains, financial transactions).")]
        [Range(0f, 1f)] public float baseElasticity = 0.2f;

        [Tooltip("This line REDUCES revenue rather than raising it. Tick for the Child Tax Credit.")]
        public bool isCredit = false;

        [Header("Incidence - who actually pays (should sum to 1)")]
        [Range(0f, 1f)] public float poorIncidence = 0.1f;
        [Range(0f, 1f)] public float middleIncidence = 0.4f;
        [Range(0f, 1f)] public float wealthyIncidence = 0.5f;

        [Header("Economic effects")]
        [Tooltip("How much raising this tax by one step drags on GDP growth.")]
        [Range(0f, 1f)] public float growthDrag = 0.1f;

        [Tooltip("How much this tax feeds directly into consumer prices. High for VAT, carbon and tariffs.")]
        [Range(0f, 1f)] public float inflationPassThrough = 0f;

        [Tooltip("Risk that capital or talent leaves as this rate rises, 0-1. Drives the Island Finance capital-flight behaviour.")]
        [Range(0f, 1f)] public float capitalFlightRisk = 0f;

        [Tooltip("Sector hit hardest by this tax. Used for the per-sector burden term.")]
        public bool sectorSpecific = false;
        public SectorId primarySector = SectorId.Finance;
    }
}
