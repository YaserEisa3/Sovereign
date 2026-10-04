namespace Sovereign.Core
{
    /// <summary>
    /// GDD 15's AI behaviours, evaluated quarterly. Nations do not choose from a
    /// menu either: they retaliate, stop buying, dump, sanction, devalue and cut
    /// supply when your relationship and your policies cross their thresholds - each
    /// one set on that nation's own NationDefinition.
    /// </summary>
    public partial class GeopoliticsModel
    {
        public void TickQuarter(EconomyState state, PolicyState policy, EventSystem events)
        {
            GeopoliticsState g = state.geopolitics;
            float globalTariff = policy.Tax(TaxKeys.ImportTariff);

            for (int i = 0; i < _c.nations.Length; i++)
            {
                NationProfile p = _c.nations[i];
                NationState n = g.nations[i];
                if (p.isPlayer) continue;

                bool agreement = policy.NationFlag(PolicyState.NationAgreement, p.name);
                bool sanctioned = policy.NationFlag(PolicyState.NationSanction, p.name);

                Retaliate(state, policy, p, n, globalTariff, agreement);
                Bonds(state, policy, events, i, p, n);

                // Below their line, they sanction you back.
                bool sanctioning = n.relationship < p.sanctionsRelationship;
                if (sanctioning && !n.sanctioningYou) Post(state, AlertLevel.Danger, AlertChannel.Ticker, p.name + " imposes sanctions on you");
                if (!sanctioning && n.sanctioningYou) Post(state, AlertLevel.Info, AlertChannel.Ticker, p.name + " lifts its sanctions");
                n.sanctioningYou = sanctioning;

                if (sanctioned && !n.wasSanctioned) OnYouSanction(state, policy, events, i, p);
                ManipulationWatch(state, policy, events, i, p, n);
                SignatureMove(state, policy, events, i, p, n);

                n.hadAgreement = agreement;
                n.wasSanctioned = sanctioned;
            }
        }

        void Retaliate(EconomyState state, PolicyState policy, NationProfile p, NationState n, float globalTariff, bool agreement)
        {
            // GDD 13: walking out of an agreement does lasting damage and invites a tariff.
            if (n.hadAgreement && !agreement)
            {
                n.permanentScar += _t.withdrawalScar;
                n.theirTariffOnYou = MathUtil.Max(n.theirTariffOnYou, 10f);
                Post(state, AlertLevel.Danger, AlertChannel.Ticker, p.name + " condemns your withdrawal from the trade agreement");
            }

            float yourTariff = policy.Nation(PolicyState.NationTariff, p.name);
            float provoked = 0f;
            if (yourTariff > p.tariffRetaliationThreshold) provoked = yourTariff;
            // The aggressive ones answer a high global tariff too.
            if (globalTariff > p.tariffRetaliationThreshold && p.aggression >= 0.5f)
                provoked = MathUtil.Max(provoked, globalTariff * p.aggression);

            if (provoked > 0f)
            {
                float answer = provoked * _t.retaliationMatch;
                if (answer > n.theirTariffOnYou + 1f)
                    Post(state, AlertLevel.Danger, AlertChannel.Ticker, p.name + " retaliates with " + answer.ToString("0") + "% tariffs on your exports");
                n.theirTariffOnYou = MathUtil.Max(n.theirTariffOnYou, answer);
            }
            else n.theirTariffOnYou *= 0.5f;
            if (n.theirTariffOnYou < 0.5f) n.theirTariffOnYou = 0f;
        }

        void Bonds(EconomyState state, PolicyState policy, EventSystem events, int index, NationProfile p, NationState n)
        {
            // GDD 9.3: first they quietly stop buying...
            bool buying = n.relationship >= p.quietBondReductionRelationship;
            if (!buying && n.buyingBonds)
                Post(state, AlertLevel.Warning, AlertChannel.Advisor, p.name + " has stopped bidding at our auctions. It holds "
                     + (n.bondHolding * 100f).ToString("0.0") + "% of our debt, and yields will drift up without it.");
            if (buying && !n.buyingBonds) Post(state, AlertLevel.Info, AlertChannel.Ticker, p.name + " returns to your bond auctions");
            n.buyingBonds = buying;

            // ...then they sell.
            if (n.relationship < p.activeBondDumpingRelationship && n.bondHolding > 0.01f && !Live(state, "BondDumpingAttack"))
            {
                EventInstance dump = events.Spawn(state, policy, "BondDumpingAttack", false);
                if (dump != null) dump.nationIndex = index;
                n.bondHolding *= 0.6f;
            }
        }

        static bool Live(EconomyState state, string key)
        {
            foreach (EventInstance e in state.events.live) if (e.key == key) return true;
            return false;
        }

        static void Post(EconomyState state, AlertLevel level, AlertChannel channel, string text)
        {
            state.events.Post(state.week, level, channel, text);
        }
    }
}
