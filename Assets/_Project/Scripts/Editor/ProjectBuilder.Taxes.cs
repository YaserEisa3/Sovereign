using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 8.1 and 11. Thirteen revenue lines, including the Child Tax Credit,
    /// which is a control on this panel that happens to run in reverse (GDD 22.2:
    /// a dollar amount, not an on/off).
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string TaxPath = Root + "/Data/Taxes/";

        static TaxDefinition Tax(string file, string name, float min, float max, float def, float step,
                                 RevenueBase revenueBase, float poor, float middle, float wealthy, int order,
                                 System.Action<TaxDefinition> extra = null)
        {
            return Asset<TaxDefinition>(TaxPath + "SO_Tax_" + file + ".asset", t =>
            {
                t.displayName = name;
                t.unit = PolicyUnit.Percent;
                t.minimumValue = min; t.maximumValue = max; t.defaultValue = def; t.stepSize = step;
                t.revenueBase = revenueBase;
                t.poorIncidence = poor; t.middleIncidence = middle; t.wealthyIncidence = wealthy;
                t.sortOrder = order;
                t.baseShare = 1f;
                // Every line explains itself; a tax may still override this below.
                t.description = TaxExplanation(file);
                extra?.Invoke(t);
            });
        }

        static TaxDefinition[] BuildTaxes()
        {
            return new[]
            {
                Tax("CorporateIncome", "Corporate Tax Rate", 0f, 55f, 21f, 1f,
                    RevenueBase.CorporateProfits, 0.10f, 0.30f, 0.60f, 10, t =>
                    { t.growthDrag = 0.18f; t.baseElasticity = 0.35f; t.capitalFlightRisk = 0.45f;
                      t.collectionEfficiency = 0.78f; }),

                Tax("IncomeTop", "Top Income Rate", 10f, 90f, 37f, 1f,
                    RevenueBase.TaxableIncomePool, 0f, 0.05f, 0.95f, 20, t =>
                    { t.baseShare = 0.20f; t.growthDrag = 0.10f; t.baseElasticity = 0.30f; t.capitalFlightRisk = 0.35f;
                      }),

                Tax("IncomeUpperMiddle", "Upper-Middle Rate", 10f, 60f, 32f, 1f,
                    RevenueBase.TaxableIncomePool, 0f, 0.55f, 0.45f, 30, t =>
                    { t.baseShare = 0.25f; t.growthDrag = 0.09f; t.baseElasticity = 0.18f; }),

                Tax("IncomeMiddle", "Middle Income Rate", 5f, 50f, 24f, 1f,
                    RevenueBase.TaxableIncomePool, 0.10f, 0.85f, 0.05f, 40, t =>
                    { t.baseShare = 0.35f; t.growthDrag = 0.14f; t.baseElasticity = 0.12f;
                      }),

                Tax("IncomeLower", "Lower Income Rate", 0f, 30f, 12f, 1f,
                    RevenueBase.TaxableIncomePool, 0.80f, 0.20f, 0f, 50, t =>
                    { t.baseShare = 0.20f; t.growthDrag = 0.16f; t.baseElasticity = 0.08f; }),

                Tax("CapitalGains", "Capital Gains Tax", 0f, 40f, 20f, 1f,
                    RevenueBase.CapitalGains, 0.02f, 0.18f, 0.80f, 60, t =>
                    { t.growthDrag = 0.12f; t.baseElasticity = 0.55f; t.capitalFlightRisk = 0.40f;
                      t.sectorSpecific = true; t.primarySector = SectorId.Finance; }),

                Tax("Payroll", "Payroll Tax", 0f, 25f, 15.3f, 0.1f,
                    RevenueBase.Payroll, 0.35f, 0.55f, 0.10f, 70, t =>
                    { t.growthDrag = 0.15f; t.baseElasticity = 0.10f; t.collectionEfficiency = 0.95f;
                      }),

                Tax("ValueAdded", "VAT / Sales Tax", 0f, 25f, 7f, 0.5f,
                    RevenueBase.ConsumerSpending, 0.45f, 0.42f, 0.13f, 80, t =>
                    { t.growthDrag = 0.12f; t.inflationPassThrough = 0.75f; t.collectionEfficiency = 0.92f;
                      }),

                Tax("Carbon", "Carbon Tax", 0f, 200f, 0f, 5f,
                    RevenueBase.CarbonEmissions, 0.35f, 0.40f, 0.25f, 90, t =>
                    { t.unit = PolicyUnit.DollarsPerTon; t.growthDrag = 0.10f; t.inflationPassThrough = 0.55f;
                      t.sectorSpecific = true; t.primarySector = SectorId.Energy; }),

                Tax("Estate", "Estate Tax", 0f, 60f, 40f, 1f,
                    RevenueBase.Estates, 0f, 0.05f, 0.95f, 100, t =>
                    { t.growthDrag = 0.03f; t.baseElasticity = 0.45f; t.collectionEfficiency = 0.55f; }),

                Tax("FinancialTransaction", "Financial Transaction Tax", 0f, 1f, 0f, 0.05f,
                    RevenueBase.FinancialTransactions, 0.05f, 0.25f, 0.70f, 110, t =>
                    { t.growthDrag = 0.05f; t.baseElasticity = 0.85f;
                      t.sectorSpecific = true; t.primarySector = SectorId.Finance;
                      }),

                Tax("ImportTariff", "Import Tariff", 0f, 50f, 3f, 1f,
                    RevenueBase.Imports, 0.40f, 0.40f, 0.20f, 120, t =>
                    { t.growthDrag = 0.20f; t.inflationPassThrough = 0.80f; t.baseElasticity = 0.50f;
                      t.sectorSpecific = true; t.primarySector = SectorId.Manufacturing;
                      }),

                Tax("ChildCredit", "Child Tax Credit", 0f, 5000f, 2000f, 250f,
                    RevenueBase.PerChildCredit, 0.45f, 0.50f, 0.05f, 130, t =>
                    { t.unit = PolicyUnit.DollarsPerUnit; t.isCredit = true; t.collectionEfficiency = 1f;
                      t.growthDrag = -0.08f;
                      }),
            };
        }
    }
}
