using System.Text;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 19: what the country makes, and whether it can feed itself.</summary>
    public static partial class EconomyTest
    {
        static void TestProduction(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            runner.Step(Year);

            ProductionModel model = runner.Simulator.Production;
            StringBuilder table = new StringBuilder("Production (at base prices, $B/yr)\n");
            table.Append("product                 made     needed    balance   price\n");

            float made = 0f, needed = 0f;
            foreach (ProductConfig c in model.Products)
            {
                ProductState p = s.production[c.id];
                made += p.produced;
                needed += p.needed;
                table.Append(c.name.PadRight(22))
                     .Append(p.produced.ToString("#,0").PadLeft(8))
                     .Append(p.needed.ToString("#,0").PadLeft(10))
                     .Append(p.Balance.ToString("+#,0;-#,0").PadLeft(11))
                     .Append(p.domesticPrice.ToString("0").PadLeft(8))
                     .Append('\n');
            }
            table.Append("TOTAL".PadRight(22)).Append(made.ToString("#,0").PadLeft(8))
                 .Append(needed.ToString("#,0").PadLeft(10))
                 .Append((made - needed).ToString("+#,0;-#,0").PadLeft(11));
            Debug.Log(table.ToString());

            // A country must be roughly able to supply itself, or every run opens in a
            // famine; and it must not be so far ahead that shortage can never happen.
            float cover = needed <= 0f ? 0f : made / needed;
            Check(cover > 0.85f && cover < 1.45f,
                  "production: the country makes " + cover.ToString("0.00")
                  + "x what it needs - calibration is adrift, nothing it does will ever move a shortage");

            // Arms are not a share of GDP; they are what the equipment budget buys.
            Check(s.production[ProductId.Arms].produced > 1f,
                  "production: the arms industry makes nothing - Government/Military has a "
                  + "gdpShare of zero, so its products must come from the defence budget");

            // Health has to matter, or investing in a sector buys nothing real.
            float grainBefore = s.production[ProductId.Grain].produced;
            s.GetSector(SectorId.Agriculture).health = 20f;
            runner.Step(4);
            Check(s.production[ProductId.Grain].produced < grainBefore * 0.75f,
                  "production: wrecking agriculture barely changed how much grain is grown");

            // ...and a wrecked food supply has to be felt in the cost of living. The
            // health has to be held DOWN while this runs: the sector model pulls it back
            // to target at 4% a week, so a first version of this test wrecked farming,
            // waited two years, and measured a fully recovered country with cheap grain.
            for (int week = 0; week < 2 * Year; week++)
            {
                s.GetSector(SectorId.Agriculture).health = 20f;
                runner.Step(1);
            }
            Debug.Log("Production: grain at " + s.production[ProductId.Grain].domesticPrice.ToString("0")
                      + " after two years of ruined farming, shortage pressure "
                      + model.ShortagePressure(s).ToString("0.0"));
            Check(s.production[ProductId.Grain].domesticPrice > 110f,
                  "production: two years of ruined farming left grain at "
                  + s.production[ProductId.Grain].domesticPrice.ToString("0") + " - shortages are not reaching prices");
        }
        /// <summary>
        /// GDD 19. Money must move supply. These effects were authored and read by
        /// nothing for months, so "the coefficient is in the file" is not evidence.
        /// </summary>
        static void TestSupplyResponds(SimulationRunner runner)
        {
            EconomyState s = Fresh(runner).State;
            runner.Step(Year);
            float grainBefore = s.production[ProductId.Grain].produced;
            float powerBefore = s.production[ProductId.Electricity].produced;
            float steelBefore = s.production[ProductId.Steel].produced;

            runner.Policy.spendingBillions["AgriculturalSubsidies"] += 200f;
            runner.Policy.spendingBillions["EnergySubsidies"] += 200f;
            runner.Policy.spendingBillions["IndustrialSubsidies"] += 200f;
            runner.Step(2 * Year);

            float grain = s.production[ProductId.Grain].produced / grainBefore - 1f;
            float power = s.production[ProductId.Electricity].produced / powerBefore - 1f;
            float steel = s.production[ProductId.Steel].produced / steelBefore - 1f;
            Debug.Log("Supply after $200B each for two years: grain " + (grain * 100f).ToString("+0.0;-0.0")
                      + "%, electricity " + (power * 100f).ToString("+0.0;-0.0")
                      + "%, steel " + (steel * 100f).ToString("+0.0;-0.0") + "%");

            // Felt, but not a magic wand: a tenth or so for $200B a year sustained.
            Check(grain > 0.04f, "supply: $200B of farm subsidies moved grain by "
                  + (grain * 100f).ToString("0.0") + "% - the budget cannot feel that");
            Check(power > 0.04f, "supply: $200B of energy subsidies moved electricity by "
                  + (power * 100f).ToString("0.0") + "%");
            Check(steel > 0.04f, "supply: $200B of industrial subsidies moved steel by "
                  + (steel * 100f).ToString("0.0") + "%");
            Check(grain < 0.45f, "supply: farm subsidies moved grain by "
                  + (grain * 100f).ToString("0.0") + "% - that is a magic wand, not a policy");

            // And countries must not be interchangeable: an oil state cannot grow grain.
            NationProfile[] nations = runner.Simulator.Geopolitics.Config.nations;
            int withProfiles = 0;
            foreach (NationProfile n in nations)
                if (n.exports != null && n.exports.Length > 0 && n.needs != null && n.needs.Length > 0) withProfiles++;
            Check(withProfiles >= nations.Length - 1,
                  "supply: only " + withProfiles + " of " + nations.Length
                  + " nations know what they make and what they lack");
        }

    }
}
