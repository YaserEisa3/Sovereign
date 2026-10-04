namespace Sovereign.Core
{
    /// <summary>Where an event happens, and sovereign default.</summary>
    public partial class EventSystem
    {
        /// <summary>Domestic crises happen at home; contagion comes from whoever you
        /// trade with most; a supply cut comes from the oil state; an attack comes
        /// from whoever likes you least.</summary>
        int ChooseNation(EconomyState state, EventConfig cfg)
        {
            int player = -1, worst = -1, holder = -1;
            for (int i = 0; i < _c.nations.Length; i++)
            {
                if (_c.nations[i].isPlayer) { player = i; continue; }
                if (worst < 0 || _c.nations[i].startingRelationship < _c.nations[worst].startingRelationship) worst = i;
                if (holder < 0 || _c.nations[i].bondHolding > _c.nations[holder].bondHolding) holder = i;
            }

            switch (cfg.key)
            {
                case "InvasionThreat": return worst;
                case "BondDumpingAttack": return holder;
                case "OilPriceShock": return ByArchetype(NationArchetype.OilState, player);
                case "EmergingSouthDefault": return ByArchetype(NationArchetype.EmergingDebtor, player);
                case "TradeWar": return ByArchetype(NationArchetype.ExportManufacturer, player);
            }

            switch (cfg.category)
            {
                case EventCategory.ForeignContagion: return WeightedByTrade(state);
                case EventCategory.War:
                case EventCategory.Pandemic:
                case EventCategory.DiplomaticIncident: return RandomForeign(state);
                default: return player;
            }
        }

        int ByArchetype(NationArchetype archetype, int fallback)
        {
            for (int i = 0; i < _c.nations.Length; i++) if (_c.nations[i].archetype == archetype) return i;
            return fallback;
        }

        int RandomForeign(EconomyState state)
        {
            int count = 0;
            foreach (NationConfig n in _c.nations) if (!n.isPlayer) count++;
            int pick = state.events.random.Range(0, count);
            for (int i = 0; i < _c.nations.Length; i++)
                if (!_c.nations[i].isPlayer && pick-- == 0) return i;
            return -1;
        }

        int WeightedByTrade(EconomyState state)
        {
            float total = 0f;
            foreach (NationConfig n in _c.nations) if (!n.isPlayer) total += n.tradeVolume;
            float roll = state.events.random.Value() * total;
            for (int i = 0; i < _c.nations.Length; i++)
            {
                if (_c.nations[i].isPlayer) continue;
                roll -= _c.nations[i].tradeVolume;
                if (roll <= 0f) return i;
            }
            return RandomForeign(state);
        }

        /// <summary>
        /// GDD 17.6 and 14. During a debt crisis, above the danger line, consecutive
        /// failed auctions mean the treasury cannot roll its debt: a missed payment,
        /// a D rating, and the end of the run.
        /// </summary>
        void CheckDefault(EconomyState state)
        {
            bool crisis = false;
            foreach (EventInstance e in state.events.live) if (e.IsActive && e.key == "DebtCrisis") crisis = true;

            bool failing = crisis && state.DebtToGdp > _t.defaultDebtThreshold
                           && state.bonds.lastAuctionCover < _t.defaultAuctionCover;
            state.events.failedAuctions = failing ? state.events.failedAuctions + 1 : 0;

            if (state.events.failedAuctions < _t.defaultFailedAuctions || state.IsGameOver) return;

            state.bonds.creditRating = CreditRating.D;
            state.approval.revolt = true;
            state.approval.revoltReason = "Sovereign default. " + state.events.failedAuctions + " auctions failed in a row with debt at "
                + (state.DebtToGdp * 100f).ToString("0") + "% of GDP, and the treasury missed an interest payment.";
        }
    }
}
