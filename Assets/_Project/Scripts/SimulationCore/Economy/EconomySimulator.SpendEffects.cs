namespace Sovereign.Core
{
    /// <summary>
    /// What spending BUYS, as opposed to what it costs.
    ///
    /// Every line already pays for itself in jobs once, through the fiscal impulse -
    /// but that is the same whether the money goes on roads or on pensions, and it
    /// fades within a year. What makes a line worth choosing is this: the effects its
    /// data has always carried, and which nothing read until now. Research, the space
    /// programme and the farm and energy subsidies were pure cost; the military bought
    /// approval and nothing else.
    ///
    /// Measured against what the country spent on that line the day the run opened, so
    /// a scenario that starts lean is not punished for it and only the player's own
    /// decisions count.
    /// </summary>
    public partial class EconomySimulator
    {
        /// <summary>Growth points bought by spending above the opening, across all lines.</summary>
        float SpendingGrowthEffect(EconomyState state, PolicyState policy)
        {
            float points = 0f;
            for (int i = 0; i < _config.treasury.spending.Length; i++)
            {
                SpendLineConfig line = _config.treasury.spending[i];
                if (line.effects == null || line.effects.Length == 0) continue;

                float above = AboveOpening(state, policy, line.key);
                if (above == 0f) continue;

                for (int e = 0; e < line.effects.Length; e++)
                {
                    SpendEffectConfig effect = line.effects[e];
                    if (effect.sectorSpecific || effect.target != ImpactTarget.RealGdpGrowth) continue;
                    points += effect.strengthPer100B * above * 0.01f;
                }
            }
            return points;
        }

        /// <summary>Health points bought for one industry by the lines that back it.</summary>
        float SpendingSectorEffect(EconomyState state, PolicyState policy, SectorId sector)
        {
            float points = 0f;
            for (int i = 0; i < _config.treasury.spending.Length; i++)
            {
                SpendLineConfig line = _config.treasury.spending[i];
                if (line.effects == null || line.effects.Length == 0) continue;

                float above = AboveOpening(state, policy, line.key);
                if (above == 0f) continue;

                for (int e = 0; e < line.effects.Length; e++)
                {
                    SpendEffectConfig effect = line.effects[e];
                    if (!effect.sectorSpecific || effect.sector != sector) continue;
                    if (effect.target != ImpactTarget.SectorHealth) continue;
                    points += effect.strengthPer100B * above * 0.01f;
                }
            }
            return points;
        }

        /// <summary>
        /// GDD 19. The best health a hundred billion can buy in this sector, across every
        /// line that backs it. Derived from the same effects the simulation reads, so the
        /// screen cannot tell the player a sector is cheap to build while the model
        /// charges them for it.
        /// </summary>
        public float SupplyEffort(SectorId sector)
        {
            float best = 0f;
            for (int i = 0; i < _config.treasury.spending.Length; i++)
            {
                SpendEffectConfig[] effects = _config.treasury.spending[i].effects;
                if (effects == null) continue;
                for (int e = 0; e < effects.Length; e++)
                    if (effects[e].sectorSpecific && effects[e].sector == sector
                        && effects[e].target == ImpactTarget.SectorHealth
                        && effects[e].strengthPer100B > best)
                        best = effects[e].strengthPer100B;
            }
            return best;
        }

        /// <summary>$B this line is running above what it ran at in week one.</summary>
        float AboveOpening(EconomyState state, PolicyState policy, string key)
        {
            float opening;
            if (!state.openingSpendingByLine.TryGetValue(key, out opening)) return 0f;
            return policy.Spending(key) - opening;
        }

        /// <summary>Captured once, at boot, after the scenario has scaled the budget.</summary>
        void CaptureOpeningSpending(EconomyState state, PolicyState policy)
        {
            state.openingSpendingByLine.Clear();
            for (int i = 0; i < _config.treasury.spending.Length; i++)
            {
                string key = _config.treasury.spending[i].key;
                state.openingSpendingByLine[key] = policy.Spending(key);
            }
        }
    }
}
