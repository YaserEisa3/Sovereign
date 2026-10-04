namespace Sovereign.Core
{
    /// <summary>Sanctions, the manipulation watch, and each archetype's signature move.</summary>
    public partial class GeopoliticsModel
    {
        void OnYouSanction(EconomyState state, PolicyState policy, EventSystem events, int index, NationProfile p)
        {
            // GDD 15: sanction the oil state and it cuts supply.
            if (p.archetype == NationArchetype.OilState && !Live(state, "OilPriceShock"))
            {
                EventInstance cut = events.Spawn(state, policy, "OilPriceShock", true);
                if (cut != null) cut.nationIndex = index;
            }

            // GDD 15: the Northern Alliance joins a sanctions coalition on your side.
            for (int j = 0; j < _c.nations.Length; j++)
            {
                if (_c.nations[j].archetype != NationArchetype.ResourceDemocracy || j == index) continue;
                NationState ally = state.geopolitics.nations[j];
                if (ally.relationship < 30f) continue;
                ally.relationship = MathUtil.Min(100f, ally.relationship + 5f);
                Post(state, AlertLevel.Info, AlertChannel.Ticker, _c.nations[j].name + " joins your sanctions on " + p.name);
            }
        }

        /// <summary>GDD 15: run a big surplus or sell your own currency and the export
        /// giant calls it manipulation.</summary>
        void ManipulationWatch(EconomyState state, PolicyState policy, EventSystem events, int index, NationProfile p, NationState n)
        {
            if (p.archetype != NationArchetype.ExportManufacturer || Live(state, "WtoComplaint")) return;
            bool manipulating = state.currency.currentAccountPercentGdp > _t.manipulationSurplus
                                || policy.currencyIntervention < 0f
                                || policy.exportSubsidyBillions > _t.exportSubsidyWtoThreshold;
            if (!manipulating) return;

            n.relationship = MathUtil.Max(-100f, n.relationship - 10f);
            Post(state, AlertLevel.Warning, AlertChannel.Ticker, p.name + " accuses you of currency manipulation");
            EventInstance complaint = events.Spawn(state, policy, "WtoComplaint", false);
            if (complaint != null) complaint.nationIndex = index;
        }

        /// <summary>Off in the quiet economy tests: a devaluation on a four-year timer is the
        /// world acting, not a response to policy. Retaliation and bond behaviour stay on.</summary>
        public bool SignatureMovesEnabled = true;

        void SignatureMove(EconomyState state, PolicyState policy, EventSystem events, int index, NationProfile p, NationState n)
        {
            if (n.weeksToSignatureMove < 0 || !SignatureMovesEnabled) return;
            n.weeksToSignatureMove -= 13;
            if (n.weeksToSignatureMove > 0) return;
            n.weeksToSignatureMove = (int)(p.signatureMoveIntervalYears * 52f);

            switch (p.archetype)
            {
                case NationArchetype.ExportManufacturer:
                {
                    // Periodic devaluation: your currency is dearer against theirs.
                    state.currency.exchangeRateIndex = MathUtil.Min(400f, state.currency.exchangeRateIndex + _t.rivalDevaluation);
                    EconomicSector manufacturing = state.GetSector(SectorId.Manufacturing);
                    if (manufacturing != null)
                        manufacturing.health = MathUtil.Max(0f, manufacturing.health - _t.devaluationManufacturingHit);
                    Post(state, AlertLevel.Warning, AlertChannel.Ticker, p.name + " devalues its currency - your manufacturers lose ground");
                    break;
                }
                case NationArchetype.OilState:
                    if (n.relationship < 0f && !Live(state, "OilPriceShock"))
                    {
                        EventInstance cut = events.Spawn(state, policy, "OilPriceShock", false);
                        if (cut != null) cut.nationIndex = index;
                    }
                    else
                    {
                        state.oilPriceIndex = MathUtil.Max(20f, state.oilPriceIndex - 8f);
                        Post(state, AlertLevel.Info, AlertChannel.Ticker, p.name + " raises output; oil prices ease");
                    }
                    break;
                case NationArchetype.EmergingDebtor:
                {
                    // A restructuring request. Your aid decides how it ends.
                    float aid = policy.Spending("ForeignAid") * policy.Nation(PolicyState.NationAidShare, p.name);
                    if (aid >= 25f)
                    {
                        n.relationship = MathUtil.Min(100f, n.relationship + 8f);
                        Post(state, AlertLevel.Info, AlertChannel.Ticker, p.name + " restructures its debts with your support");
                    }
                    else if (!Live(state, "EmergingSouthDefault"))
                    {
                        EventInstance fallout = events.Spawn(state, policy, "EmergingSouthDefault", false);
                        if (fallout != null) fallout.nationIndex = index;
                    }
                    break;
                }
            }
        }
    }
}
