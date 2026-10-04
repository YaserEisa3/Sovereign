using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>One foreign nation, as it stands with you this week.</summary>
    public class NationState
    {
        public float relationship;
        /// <summary>GDD 13: withdrawing from an agreement leaves a mark that never heals.</summary>
        public float permanentScar;
        public float theirTariffOnYou;
        /// <summary>How much trade actually runs with this nation now, 1 being its
        /// normal volume. Tariffs, deals and sanctions move it; the map counts ships by it.</summary>
        public float tradeIndex = 1f;
        public bool buyingBonds = true;
        public bool sanctioningYou;
        /// <summary>Share of YOUR debt this nation holds.</summary>
        public float bondHolding;
        public int weeksToSignatureMove;

        // Last quarter's diplomatic settings, so the AI sees the EDGE of a change.
        public bool hadAgreement, wasSanctioned;
    }

    public class GeopoliticsState
    {
        public readonly List<NationState> nations = new List<NationState>();
        /// <summary>Where trade volume settles given every tariff, deal and sanction in force.</summary>
        public float tradeTarget = 100f;
        /// <summary>0-1: how much of your foreign-held debt belongs to nations still buying.</summary>
        public float foreignAppetite = 1f;
        /// <summary>Share of the corporate and capital gains base sheltering abroad.</summary>
        public float capitalFlight;
        /// <summary>Emergency reserves from open swap lines, $B.</summary>
        public float swapBackstop;
        /// <summary>Your per-nation tariffs, weighted by trade share - what they add to import prices.</summary>
        public float importTariffWeighted;
    }
}
