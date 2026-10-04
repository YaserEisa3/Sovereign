using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// Seeds PolicyState from the tax and spending assets, so the opening budget is
    /// whatever the designer set in the Inspector rather than a table in code.
    ///
    /// The dictionary key is the asset's name with its prefix stripped:
    /// SO_Tax_CorporateIncome becomes CorporateIncome, which is what TaxKeys spells.
    /// Add an asset and the treasury tracks a new line without a code change.
    /// </summary>
    public static class PolicyBootstrap
    {
        public static PolicyState Create(GameDatabase database, StartingConditions starting)
        {
            PolicyState policy = new PolicyState();

            TaxDefinition[] taxes = database.Taxes;
            for (int i = 0; i < taxes.Length; i++)
            {
                if (taxes[i] == null) continue;
                policy.taxRates[KeyFor(taxes[i], "SO_Tax_")] = taxes[i].defaultValue;
            }

            SpendingCategoryDefinition[] spending = database.SpendingCategories;
            for (int i = 0; i < spending.Length; i++)
            {
                if (spending[i] == null) continue;
                // Debt service is computed from the debt stock, not set by the player,
                // so it is not part of the discretionary budget the simulator adds up.
                if (spending[i].displayName == "Debt Service") continue;
                policy.spendingBillions[KeyFor(spending[i], "SO_Spend_")] =
                    spending[i].defaultBillionsPerYear * starting.spendingScale;
            }

            policy.centralBankRate = starting.centralBankRate;
            policy.foreignIssuanceShare = starting.foreignHoldingShare;

            PopulationParameters population = database.Population;
            if (population != null)
            {
                policy.immigrationInflowMillions = population.defaultImmigrationInflow;
                policy.skilledImmigrationShare = population.defaultSkilledShare;
            }

            return policy;
        }

        public static string KeyFor(ScriptableObject asset, string prefix)
        {
            string name = asset.name;
            return name.StartsWith(prefix) ? name.Substring(prefix.Length) : name;
        }
    }
}
