using System.Text;
using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 19. What the country the player actually starts in can and cannot make.
    ///
    /// The economy tests print this for the peacetime position, which is not the one
    /// anybody plays. This boots the aftermath - the war's wreckage, a shrunken economy
    /// and damaged sectors - and lists every product by how far short it falls.
    ///
    /// Menu: Sovereign -> Print Production Report.
    /// </summary>
    public static class ProductionReport
    {
        [MenuItem("Sovereign/Print Production Report", false, 47)]
        public static void RunFromMenu() { Report(); }

        public static void Run()
        {
            Report();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Report()
        {
            SimulationRunner runner = EconomyTestAccess.LoadRunner();
            if (runner == null) { Debug.LogError("Sovereign: no SimulationRunner in the scene."); return; }

            runner.Scenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ProjectBuilder.PostwarScenarioAsset);
            runner.Boot();
            runner.Simulator.Events.RandomEventsEnabled = false;
            runner.Simulator.Approval.RevoltEnabled = false;
            runner.Step(8);   // let prices settle off their opening 100

            EconomyState s = runner.State;
            ProductConfig[] products = runner.Simulator.Production.Products;

            StringBuilder report = new StringBuilder();
            report.AppendLine("=== THE AFTERMATH: what the country can and cannot make ===");
            report.AppendLine("product                 sector            made   needed  short by   cover  price  can make  bites");

            System.Array.Sort(products, (a, b) =>
                Cover(s, a.id).CompareTo(Cover(s, b.id)));

            foreach (ProductConfig c in products)
            {
                ProductState p = s.production[c.id];
                float cover = Cover(s, c.id);
                report.Append(c.name.PadRight(22))
                      .Append(c.sector.ToString().PadRight(16))
                      .Append(p.produced.ToString("#,0").PadLeft(8))
                      .Append(p.needed.ToString("#,0").PadLeft(10))
                      .Append((p.Balance < 0f ? (-p.Balance).ToString("#,0") : "-").PadLeft(11))
                      .Append((cover * 100f).ToString("0").PadLeft(8) + "%")
                      .Append(p.domesticPrice.ToString("0").PadLeft(7))
                      .Append((c.capability >= 0.999f ? "all" : (c.capability * 100f).ToString("0") + "%").PadLeft(10))
                      .Append("   ")
                      .Append(c.shortageSeverity >= 0.9f ? "hard" : c.shortageSeverity >= 0.6f ? "some" : "little")
                      .AppendLine();
            }

            Debug.Log(report.ToString());
        }

        static float Cover(EconomyState s, ProductId id)
        {
            ProductState p = s.production[id];
            return p.needed <= 0.01f ? 9f : p.produced / p.needed;
        }
    }
}
