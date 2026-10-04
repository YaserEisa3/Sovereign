using Sovereign.Core;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 20 Phase 5. A loaded game must be the SAME game: every figure, every open
    /// event, the queue and the dice. So this plays a seeded world for three years,
    /// saves, and plays on two more; then loads that save into a freshly booted
    /// simulation, plays the same two years, and demands the two futures match to the
    /// last bit - compared as whole saves, so no field can quietly diverge.
    /// </summary>
    public static partial class EconomyTest
    {
        static void TestSaveLoad(SimulationRunner runner)
        {
            const uint seed = 777u;
            FreshWithEvents(runner, seed);
            runner.Policy.QueueScalar("centralBankRate", runner.Policy.centralBankRate + 1f, runner.Policy.centralBankRate);
            runner.Step(3 * Year);
            // Something left in the queue, so the save has to carry intentions as well as facts.
            runner.Policy.QueueScalar("centralBankRate", runner.Policy.centralBankRate - 0.5f, runner.Policy.centralBankRate);
            string save = runner.SaveToJson();
            int savedWeek = runner.State.week;

            runner.Step(2 * Year);
            string original = SaveGame.Write(runner.State, runner.Policy, "t");

            runner.LoadFromJson(save);
            Check(runner.State.week == savedWeek, "save/load: the loaded game is at week " + runner.State.week + ", not the saved " + savedWeek);
            string reSaved = SaveGame.Write(runner.State, runner.Policy, "t");
            Check(reSaved == Stamp(save), "save/load: saving straight after loading did not reproduce the file");

            runner.Simulator.Approval.RevoltEnabled = false;
            runner.Step(2 * Year);
            string replayed = SaveGame.Write(runner.State, runner.Policy, "t");
            Check(replayed == original, "save/load: the loaded game diverged from the original - first difference at char "
                  + FirstDifference(original, replayed) + ": " + Around(original, replayed));
            CheckFinite(runner.State, "save/load");

            bool refused = false;
            try { SaveGame.Read(save.Replace("\"version\":1", "\"version\":99"), runner.State, runner.Policy); }
            catch (System.Exception) { refused = true; }
            Check(refused, "save/load: a save from an unknown version was loaded instead of refused");
        }

        /// <summary>The same save with its timestamp replaced, for comparing content.</summary>
        static string Stamp(string save)
        {
            int start = save.IndexOf("\"savedAt\":\"") + 11;
            int end = save.IndexOf('"', start);
            return save.Substring(0, start) + "t" + save.Substring(end);
        }

        static int FirstDifference(string a, string b)
        {
            int n = System.Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++) if (a[i] != b[i]) return i;
            return a.Length == b.Length ? -1 : n;
        }

        static string Around(string a, string b)
        {
            int i = FirstDifference(a, b);
            if (i < 0) return "";
            int from = System.Math.Max(0, i - 120);
            return a.Substring(from, System.Math.Min(160, a.Length - from)) + "  VS  " + b.Substring(from, System.Math.Min(160, b.Length - from));
        }
    }
}
