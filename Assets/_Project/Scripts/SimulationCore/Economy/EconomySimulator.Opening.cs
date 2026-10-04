namespace Sovereign.Core
{
    public partial class EconomySimulator
    {
        /// <summary>
        /// GDD 3.5. The last touches a scenario puts on a fresh state: who you just
        /// fought, and what the country is told on the first morning. Everything here
        /// comes from the scenario asset, so a new opening needs no code.
        /// </summary>
        void ApplyOpening(EconomyState state)
        {
            string hostile = _config.openingHostileNation;
            if (!string.IsNullOrEmpty(hostile))
            {
                NationProfile[] nations = _config.geopolitics.nations;
                for (int i = 0; i < nations.Length && i < state.geopolitics.nations.Count; i++)
                {
                    if (nations[i].name != hostile) continue;
                    NationState enemy = state.geopolitics.nations[i];
                    enemy.relationship = _config.openingHostileRelationship;
                    // A war does not end in forgiveness: the damage is remembered.
                    enemy.permanentScar = MathUtil.Max(enemy.permanentScar, -_config.openingHostileRelationship * 0.25f);
                    if (_config.openingHostileSanctions)
                    {
                        enemy.sanctioningYou = true;
                        enemy.buyingBonds = false;
                    }
                }
            }

            if (_config.openingBriefing == null) return;
            foreach (string line in _config.openingBriefing)
                if (!string.IsNullOrEmpty(line))
                    state.events.Post(state.week, AlertLevel.Warning, AlertChannel.Ticker, line);
        }
    }
}
