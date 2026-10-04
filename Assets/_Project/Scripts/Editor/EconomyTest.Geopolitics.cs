using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>GDD 13 and 15: nations that remember, retaliate, stop buying and sell.</summary>
    public static partial class EconomyTest
    {
        const string Sino = "Sino-Pacific", Euro = "Euroland", Petro = "Petro-Gulf", North = "Northern Alliance";

        static int NationIndex(SimulationRunner runner, string name)
        {
            NationProfile[] nations = runner.Simulator.Geopolitics.Config.nations;
            for (int i = 0; i < nations.Length; i++) if (nations[i].name == name) return i;
            return -1;
        }

        static NationState Nation(SimulationRunner runner, string name)
        {
            return runner.State.geopolitics.nations[NationIndex(runner, name)];
        }

        static void TestDiplomacy(SimulationRunner runner)
        {
            Fresh(runner);
            runner.Step(Year);
            float calmSino = Nation(runner, Sino).relationship, calmEuro = Nation(runner, Euro).relationship;
            float calmTrade = runner.State.tradeVolumeIndex;

            Fresh(runner);
            runner.Policy.SetNation(PolicyState.NationTariff, Sino, 30f);
            runner.Policy.SetNation(PolicyState.NationAgreement, Euro, 1f);
            runner.Step(Year);
            NationState sino = Nation(runner, Sino), euro = Nation(runner, Euro);

            Check(sino.relationship < calmSino - 10f, "diplomacy: a 30% tariff barely soured Sino-Pacific");
            Check(euro.relationship > calmEuro + 5f, "diplomacy: a trade agreement did not warm Euroland");
            Check(sino.theirTariffOnYou >= 25f, "diplomacy: Sino-Pacific did not retaliate against a tariff above its 20% threshold");
            Check(runner.State.tradeVolumeIndex < calmTrade, "diplomacy: a tariff war with your biggest partner did not cut trade");
            Check(runner.State.treasury.revenueByLine["NationTariffs"] > 0f, "diplomacy: a per-nation tariff raised no revenue");

            // GDD 13: walking out of the agreement scars the relationship for good.
            runner.Policy.QueueNation(PolicyState.NationAgreement, Euro, 0f);
            runner.Step(2 * 13);
            Check(euro.permanentScar > 0f && euro.theirTariffOnYou > 0f,
                  "diplomacy: withdrawing from an agreement cost nothing permanent");
        }

        /// <summary>GDD 9.3: the bond weapon - stop buying, then sell.</summary>
        static void TestBondWeapon(SimulationRunner runner)
        {
            Fresh(runner);
            runner.Step(Year);
            float calmForeign = runner.State.bonds.lastForeignDemand;

            Fresh(runner);
            runner.Policy.SetNation(PolicyState.NationSanction, Sino, 1f);
            runner.Policy.SetNation(PolicyState.NationTariff, Sino, 40f);
            bool stopped = false, dumped = false;
            for (int week = 0; week < 3 * Year; week++)
            {
                runner.Step();
                if (!Nation(runner, Sino).buyingBonds) stopped = true;
                foreach (EventInstance e in runner.State.events.live)
                    if (e.key == "BondDumpingAttack" && e.nationIndex == NationIndex(runner, Sino)) dumped = true;
            }

            Check(stopped, "bond weapon: Sino-Pacific never stopped buying despite sanctions and a 40% tariff");
            Check(runner.State.geopolitics.foreignAppetite < 1f, "bond weapon: foreign appetite did not fall");
            Check(runner.State.bonds.lastForeignDemand < calmForeign - 0.02f,
                  "bond weapon: foreign demand at auction did not fall when the biggest holder walked ("
                  + runner.State.bonds.lastForeignDemand.ToString("0.000") + " vs " + calmForeign.ToString("0.000") + ")");
            Check(dumped || HistoryHas(runner, "BondDumpingAttack"), "bond weapon: below -70 they never actively dumped your bonds");
            Check(Nation(runner, Sino).sanctioningYou, "bond weapon: a nation at -100 never sanctioned you back");
        }

        static void TestNationMoves(SimulationRunner runner)
        {
            // GDD 15: sanction the oil state and it cuts supply; the Northern Alliance joins in.
            Fresh(runner);
            float northBefore = Nation(runner, North).relationship;
            runner.Policy.QueueNation(PolicyState.NationSanction, Petro, 1f);
            runner.Step(14);
            Check(HistoryHas(runner, "OilPriceShock") || LiveHas(runner, "OilPriceShock"),
                  "nations: sanctioning Petro-Gulf did not trigger an oil supply cut");
            Check(Nation(runner, North).relationship > northBefore, "nations: the Northern Alliance did not join the sanctions");

            // GDD 15, Island Finance: capital leaves above 25% corporate tax.
            Fresh(runner);
            runner.Policy.taxRates[TaxKeys.CorporateIncome] = 40f;
            runner.Step(1);
            Check(runner.State.geopolitics.capitalFlight > 0.15f,
                  "nations: a 40% corporate tax sent no capital to the haven (" + runner.State.geopolitics.capitalFlight.ToString("0.00") + ")");

            // GDD 13: swap lines only with friends.
            Fresh(runner);
            runner.Policy.SetNation(PolicyState.NationSwapLine, North, 1f);
            runner.Policy.SetNation(PolicyState.NationSwapLine, Sino, 1f);
            runner.Step(1);
            Check(runner.State.geopolitics.swapBackstop > 100f && runner.State.geopolitics.swapBackstop < 200f,
                  "nations: expected one swap line (the ally) and a refusal (the rival), got $"
                  + runner.State.geopolitics.swapBackstop.ToString("0") + "B of backstop");
        }

        static bool HistoryHas(SimulationRunner runner, string key)
        {
            foreach (EventRecord r in runner.State.events.history) if (r.key == key) return true;
            return false;
        }

        static bool LiveHas(SimulationRunner runner, string key)
        {
            foreach (EventInstance e in runner.State.events.live) if (e.key == key) return true;
            return false;
        }
    }
}
