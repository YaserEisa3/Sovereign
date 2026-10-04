namespace Sovereign.Core
{
    /// <summary>
    /// Works out which nation and sector the news could be about this week, and
    /// hands the generator the facts. Kept apart from the generator so the templates
    /// never need to know how nations or sectors are stored.
    /// </summary>
    public static class NewsDesk
    {
        public static HeadlineFacts Gather(EconomyState s, GeopoliticsConfig geo)
        {
            HeadlineFacts f = new HeadlineFacts();
            float warmest = 60f, coldest = -20f, tariff = 1f;

            for (int i = 0; i < geo.nations.Length && i < s.geopolitics.nations.Count; i++)
            {
                if (geo.nations[i].isPlayer) continue;
                NationState n = s.geopolitics.nations[i];
                string name = geo.nations[i].name;

                if (n.relationship > warmest) { warmest = n.relationship; f.warmestNation = name; }
                if (n.relationship < coldest) { coldest = n.relationship; f.coldestNation = name; }
                if (n.theirTariffOnYou > tariff) { tariff = n.theirTariffOnYou; f.tariffNation = name; f.nationTariff = n.theirTariffOnYou; }
                if (!n.buyingBonds && f.walkingCreditor == null) f.walkingCreditor = name;
            }
            f.relationship = f.warmestNation != null ? warmest : coldest;

            float strong = 80f, weak = 45f;
            foreach (EconomicSector sector in s.sectors)
            {
                if (sector.health > strong) { strong = sector.health; f.strongSector = sector.name; }
                if (sector.health < weak) { weak = sector.health; f.weakSector = sector.name; }
            }
            return f;
        }
    }
}
