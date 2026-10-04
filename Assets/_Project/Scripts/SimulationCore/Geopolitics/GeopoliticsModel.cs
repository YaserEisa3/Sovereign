namespace Sovereign.Core
{
    /// <summary>
    /// GDD 15. Six nations that remember what you did. Every week each relationship
    /// drifts toward what your policies have earned it, and the consequences are
    /// summed: where trade volume settles, whether your creditors are still buying,
    /// how much capital is sheltering offshore. Every quarter each nation acts on
    /// its own personality - see GeopoliticsModel.Behaviour.
    /// </summary>
    public partial class GeopoliticsModel
    {
        readonly GeopoliticsConfig _c;
        readonly GeopoliticsTuning _t;

        public GeopoliticsModel(GeopoliticsConfig config) { _c = config; _t = config.tuning; }

        public GeopoliticsConfig Config { get { return _c; } }

        public void Initialise(EconomyState state, PolicyState policy)
        {
            GeopoliticsState g = state.geopolitics;
            g.nations.Clear();
            for (int i = 0; i < _c.nations.Length; i++)
            {
                NationProfile p = _c.nations[i];
                NationState n = new NationState
                {
                    relationship = p.startingRelationship,
                    bondHolding = p.bondHolding,
                    // Stagger the first signature move so no two nations act in lockstep.
                    weeksToSignatureMove = p.signatureMoveIntervalYears <= 0f ? -1
                        : (int)(p.signatureMoveIntervalYears * 52f * (0.5f + 0.1f * i))
                };
                g.nations.Add(n);

                // GDD 15: aid goes where it is needed - Emerging South by default.
                if (!p.isPlayer && policy.Nation(PolicyState.NationAidShare, p.name) == 0f)
                    policy.SetNation(PolicyState.NationAidShare, p.name, p.archetype == NationArchetype.EmergingDebtor ? 0.6f : 0.08f);
            }
            TickWeek(state, policy);
        }

        public void TickWeek(EconomyState state, PolicyState policy)
        {
            GeopoliticsState g = state.geopolitics;
            float trade = 100f, weightedTariff = 0f, heldTotal = 0f, heldBuying = 0f, swap = 0f;
            float aid = policy.Spending("ForeignAid");

            for (int i = 0; i < _c.nations.Length; i++)
            {
                NationProfile p = _c.nations[i];
                NationState n = g.nations[i];
                if (p.isPlayer) continue;

                float yourTariff = policy.Nation(PolicyState.NationTariff, p.name);
                bool agreement = policy.NationFlag(PolicyState.NationAgreement, p.name);
                bool sanctioned = policy.NationFlag(PolicyState.NationSanction, p.name);
                bool swapLine = policy.NationFlag(PolicyState.NationSwapLine, p.name);
                float aidBillions = aid * policy.Nation(PolicyState.NationAidShare, p.name);

                float target = p.startingRelationship - n.permanentScar
                               - yourTariff * _t.tariffRelationshipPenalty
                               + (agreement ? _t.agreementBonus : 0f)
                               - (sanctioned ? _t.sanctionPenalty : 0f)
                               + aidBillions * 0.1f * _t.aidRelationshipPer10B
                               + (swapLine && n.relationship >= _t.swapLineMinimumRelationship ? _t.swapLineBonus : 0f)
                               + Temperament(p, state, policy);
                n.relationship = MathUtil.Clamp(MathUtil.Approach(n.relationship, MathUtil.Clamp(target, -100f, 100f), _t.relationshipDrift), -100f, 100f);

                // Trade settles where tariffs, deals and sanctions leave it - per nation,
                // so the map can show which lanes are busy and which have gone quiet.
                float lane = 1f
                           - (n.theirTariffOnYou + yourTariff * 0.5f) * _t.tradeLossPerTariffPoint * 0.01f
                           + (agreement ? _t.agreementTradeBonus * 0.01f : 0f)
                           - (sanctioned || n.sanctioningYou ? _t.sanctionTradeLoss * 0.01f : 0f);
                n.tradeIndex = MathUtil.Clamp(lane, 0f, 2f);

                trade -= p.tradeVolume * (n.theirTariffOnYou + yourTariff * 0.5f) * _t.tradeLossPerTariffPoint;
                if (agreement) trade += p.tradeVolume * _t.agreementTradeBonus;
                if (sanctioned || n.sanctioningYou) trade -= p.tradeVolume * _t.sanctionTradeLoss;
                weightedTariff += p.tradeVolume * yourTariff;

                // GDD 9.3: creditors who stop buying stop propping up your auctions. Weighted
                // by what each NORMALLY buys - weighting by current holdings let a creditor
                // that dumped its bonds count for less, so the weapon blunted itself.
                heldTotal += p.bondHolding;
                if (n.buyingBonds) heldBuying += p.bondHolding;
                else n.bondHolding = MathUtil.Max(0f, n.bondHolding * (1f - 0.002f));

                if (swapLine && n.relationship >= _t.swapLineMinimumRelationship) swap += _t.swapLineCapacityBillions;
            }

            g.tradeTarget = MathUtil.Clamp(trade, 20f, 160f);
            g.importTariffWeighted = weightedTariff;
            g.foreignAppetite = heldTotal <= 0f ? 1f : heldBuying / heldTotal;
            g.swapBackstop = swap;

            // GDD 15, Island Finance: above 25% corporate tax, capital takes a holiday.
            float corporate = policy.Tax(TaxKeys.CorporateIncome);
            g.capitalFlight = MathUtil.Clamp((corporate - _t.capitalFlightThreshold) * _t.capitalFlightPerPoint, 0f, _t.capitalFlightCap);
        }

        /// <summary>Each archetype has opinions about your whole policy, not just about
        /// what you do to it - GDD 15's "key behaviors", as standing preferences.</summary>
        static float Temperament(NationProfile p, EconomyState s, PolicyState policy)
        {
            switch (p.archetype)
            {
                // Exports its regulatory standards and expects you to keep up.
                case NationArchetype.LargeDiversified: return (policy.environmentalRegulation - 40f) * 0.25f;
                // Resents a weak currency undercutting its exporters.
                case NationArchetype.ExportManufacturer: return (s.currency.exchangeRateIndex - 100f) * 0.3f;
                default: return 0f;
            }
        }
    }
}
