using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 18. The bars of the all-meters view: what each one reads, the range it is
    /// drawn against, and the reading it is judged by. Wider ranges than a healthy
    /// country needs, because this game is about the weeks when they are not healthy.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static MeterParameters BuildMeterParameters()
        {
            return Asset<MeterParameters>(ParamPath + "SO_MeterParameters.asset", m =>
            {
                m.entries = new[]
                {
                    Meter("gdpGrowth", "Growth", "%", -8f, 8f, 2.5f, 1.5f, true, false, 1),
                    Meter("inflation", "Inflation", "%", -4f, 20f, 2f, 1.5f, false, true, 1),
                    Meter("unemployment", "Unemp", "%", 0f, 25f, 4.5f, 2f, false, false, 1),
                    Meter("debtToGdp", "Debt", "%", 0f, 250f, 90f, 30f, false, false, 0),
                    Meter("budgetBalance", "Budget", "%", -20f, 6f, 0f, 3f, true, false, 1),
                    Meter("longYield", "30y", "%", 0f, 20f, 4f, 2f, false, false, 2),
                    Meter("currency", "Currency", "", 40f, 160f, 100f, 10f, false, true, 1),
                    Meter("currentAccount", "Trade", "%", -12f, 12f, 0f, 3f, true, false, 1),
                    Meter("approvalOverall", "Approval", "%", 0f, 100f, 50f, 15f, true, false, 0),
                    Meter("unrest", "Unrest", "", 0f, 100f, 15f, 15f, false, false, 0),
                    Meter("infrastructure", "Infra", "", 0f, 100f, 75f, 15f, true, false, 0),
                    Meter("realWage", "Wages", "", 60f, 160f, 100f, 10f, true, false, 1),
                };
            });
        }

        static MeterParameters.Entry Meter(string series, string label, string unit, float min, float max,
                                          float target, float tolerance, bool higherIsBetter,
                                          bool judgeByDistance, int decimals)
        {
            return new MeterParameters.Entry
            {
                series = series, label = label, unit = unit, explanation = MeterExplanation(series),
                min = min, max = max, target = target, tolerance = tolerance,
                higherIsBetter = higherIsBetter, judgeByDistance = judgeByDistance, decimals = decimals
            };
        }
    }
}
