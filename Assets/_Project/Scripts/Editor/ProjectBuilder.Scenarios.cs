using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 3.5. Two openings as assets: the war's aftermath, which the game starts
    /// from, and the calm GDD 18 dashboard position, which the economy tests measure
    /// the model against.
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string ScenarioPath = Root + "/Data/Scenarios/";

        public const string PostwarScenarioAsset = ScenarioPath + "SO_Scenario_Aftermath.asset";
        public const string PeacetimeScenarioAsset = ScenarioPath + "SO_Scenario_Peacetime.asset";

        static ScenarioDefinition BuildPostwarScenario()
        {
            return Asset<ScenarioDefinition>(PostwarScenarioAsset, s =>
            {
                s.displayName = "The Aftermath";
                s.summary = "The war ended four months ago. A third of the industrial base is rubble, "
                            + "the currency is barely trusted, and the reconstruction loan - the largest "
                            + "any government has ever signed - has just cleared. You have one chance to "
                            + "spend it well.";

                StartingConditions c = s.conditions;
                // Output: a smaller economy than the one that went to war, running well
                // under even its reduced capacity, with a high rebound potential.
                c.nominalGdpBillions = 18400f;
                c.potentialGrowthRate = 3.6f;
                c.worldGrowthRate = 2.4f;
                c.outputGapPercent = -14f;
                c.productivityScar = 0.5f;
                c.infrastructureHealth = 29f;

                // Prices and labour: demobilised soldiers, shattered supply, high prices.
                c.unemployment = 15.5f;
                c.inflation = 9.4f;

                // Money and debt: the loan is enormous, foreign, and not cheap.
                c.centralBankRate = 8f;
                c.debtToGdp = 1.35f;
                c.averageCoupon = 4.2f;
                c.foreignHoldingShare = 0.54f;
                // The loan is long and fixed - thirty-year money from people who wanted the
                // country rebuilt, not a stack of bills to roll every quarter.
                c.maturingWithinOneYear = 0.08f;
                c.maturingOneToFive = 0.27f;
                c.fxReservesBillions = 95f;

                // A broken tax base collects far less than a working one.
                c.openingRevenueShareOfGdp = 0.17f;
                // The authored budget belongs to a 27T economy. This one cannot pay for it.
                c.spendingScale = 0.54f;

                c.hostileNation = "Northern Alliance";
                c.hostileRelationship = -68f;
                c.hostileSanctions = true;

                // What winning means here: the country rebuilt, the loan repaid down to
                // something a normal state carries, and the people better off than they
                // were - held together for two years, not touched once and lost again.
                c.victoryYears = 2f;
                c.victoryTitle = "THE COUNTRY IS WHOLE AGAIN";
                c.victorySummary = "Debt under 60% of GDP, infrastructure rebuilt, people in work "
                                 + "and wages worth more than before the war - and it has held.";
                c.victoryDebtToGdp = 0.6f;
                c.victoryUnemployment = 5.5f;
                c.victoryInfrastructure = 75f;
                c.victoryRealWage = 110f;
                c.victoryApproval = 50f;

                c.briefing = new[]
                {
                    "THE WAR IS OVER. Reconstruction begins under a government nobody elected in peacetime.",
                    "The reconstruction loan has cleared: debt now stands at 135% of a shrunken GDP, most of it owed abroad.",
                    "Assessors put infrastructure at 29 out of 100. Roads, grid and rail need spending before anything else can grow.",
                    "The Northern Alliance has not lifted its sanctions and is not buying your bonds.",
                    "ADVICE: infrastructure is the cheapest growth you can buy, and money at 8% is strangling "
                    + "the recovery. Raise taxes a little, cut rates, and spend on the grid - the bond market "
                    + "pays you for a debt that is FALLING, long before it is low.",
                };
            });
        }

        static ScenarioDefinition BuildPeacetimeScenario()
        {
            return Asset<ScenarioDefinition>(PeacetimeScenarioAsset, s =>
            {
                s.displayName = "Steady State";
                s.summary = "The GDD 18 dashboard position: a large, indebted, peaceful economy. "
                            + "The economy tests measure the model against this opening.";
                // Field initialisers on StartingConditions are exactly this position.
                s.conditions = new StartingConditions();
            });
        }
    }
}
