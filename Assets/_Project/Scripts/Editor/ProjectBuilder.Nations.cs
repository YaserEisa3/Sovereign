using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 15. Seven assets: the home nation plus the six AI nations. (GDD 3.5 says
    /// six NationDefinitions while GDD 3.3 gives Nation_Home one too - the hierarchy
    /// wins, because the home nation needs a definition to be drawn on the map.)
    ///
    /// Bond holdings sum to 0.28, matching the 28% foreign holding on the GDD 18
    /// dashboard. Trade volumes sum to 1.00 - they are shares of your trade, not of
    /// world trade.
    /// </summary>
    public static partial class ProjectBuilder
    {
        const string NationPath = Root + "/Data/Nations/";

        static NationDefinition[] BuildNations()
        {
            return new[]
            {
                Asset<NationDefinition>(NationPath + "SO_Nation_Home.asset", n =>
                {
                    n.displayName = "Home Nation"; n.archetype = NationArchetype.HomeNation;
                    n.isPlayerNation = true; n.startingRelationship = 100f;
                    n.bondHoldingPercent = 0f; n.tradeVolumePercent = 0f; n.financialLinkage = 0f;
                    n.trendGrowthRate = 2.1f; n.recessionProneness = 0f;
                    n.briefing = "You. Large, diversified, and carrying more debt than anyone is comfortable saying out loud.";
                    n.nationColor = new Color32(0x7a, 0x9c, 0xc5, 0xff);
                }),

                Asset<NationDefinition>(NationPath + "SO_Nation_Euroland.asset", n =>
                {
                    n.displayName = "Euroland"; n.archetype = NationArchetype.LargeDiversified;
                    n.startingRelationship = 55f;
                    n.bondHoldingPercent = 0.060f; n.tradeVolumePercent = 0.24f; n.financialLinkage = 0.35f;
                    n.trendGrowthRate = 1.2f; n.recessionProneness = 0.20f;
                    n.tariffRetaliationThreshold = 15f; n.aggression = 0.25f;
                    n.briefing = "Large, diversified, slow-growing. Exports regulatory standards and expects you to adopt them.";
                    n.nationColor = new Color32(0x5c, 0x8a, 0xd6, 0xff);
                }),

                Asset<NationDefinition>(NationPath + "SO_Nation_SinoPacific.asset", n =>
                {
                    n.displayName = "Sino-Pacific"; n.archetype = NationArchetype.ExportManufacturer;
                    n.startingRelationship = 5f;
                    n.bondHoldingPercent = 0.090f; n.tradeVolumePercent = 0.30f; n.financialLinkage = 0.20f;
                    n.trendGrowthRate = 4.5f; n.recessionProneness = 0.12f;
                    n.tariffRetaliationThreshold = 20f;
                    n.quietBondReductionRelationship = -40f; n.activeBondDumpingRelationship = -70f;
                    n.signatureMoveIntervalYears = 4f; n.aggression = 0.80f;
                    n.briefing = "Export manufacturing giant and your largest creditor. Holds the biggest single block of your debt - and knows it.";
                    n.nationColor = new Color32(0xd6, 0x4b, 0x4b, 0xff);
                }),

                Asset<NationDefinition>(NationPath + "SO_Nation_PetroGulf.asset", n =>
                {
                    n.displayName = "Petro-Gulf"; n.archetype = NationArchetype.OilState;
                    n.startingRelationship = 25f;
                    n.bondHoldingPercent = 0.035f; n.tradeVolumePercent = 0.10f; n.financialLinkage = 0.12f;
                    n.trendGrowthRate = 2.0f; n.recessionProneness = 0.25f;
                    n.signatureMoveIntervalYears = 5f; n.aggression = 0.55f;
                    n.briefing = "Oil state with a sovereign wealth fund. Sets the global oil price inside a band, and sanctions cost you at the pump.";
                    n.nationColor = new Color32(0xe0, 0xa3, 0x3a, 0xff);
                }),

                Asset<NationDefinition>(NationPath + "SO_Nation_EmergingSouth.asset", n =>
                {
                    n.displayName = "Emerging South"; n.archetype = NationArchetype.EmergingDebtor;
                    n.startingRelationship = 35f;
                    n.bondHoldingPercent = 0.015f; n.tradeVolumePercent = 0.13f; n.financialLinkage = 0.18f;
                    n.trendGrowthRate = 5.5f; n.recessionProneness = 0.35f;
                    n.signatureMoveIntervalYears = 6f; n.aggression = 0.35f;
                    n.briefing = "High growth, high debt, and periodically in need of restructuring. Refuse and your exposed banks take the loss.";
                    n.nationColor = new Color32(0x4f, 0xb3, 0x7a, 0xff);
                }),

                Asset<NationDefinition>(NationPath + "SO_Nation_IslandFinance.asset", n =>
                {
                    n.displayName = "Island Finance"; n.archetype = NationArchetype.FinancialHaven;
                    n.startingRelationship = 45f;
                    n.bondHoldingPercent = 0.045f; n.tradeVolumePercent = 0.05f; n.financialLinkage = 0.45f;
                    n.trendGrowthRate = 2.8f; n.recessionProneness = 0.18f;
                    n.aggression = 0.20f;
                    n.briefing = "Tax haven and financial hub. Raise corporate tax above 25% and your capital starts taking holidays here.";
                    n.nationColor = new Color32(0x9c, 0x6a, 0xde, 0xff);
                }),

                Asset<NationDefinition>(NationPath + "SO_Nation_NorthernAlliance.asset", n =>
                {
                    n.displayName = "Northern Alliance"; n.archetype = NationArchetype.ResourceDemocracy;
                    n.startingRelationship = 70f;
                    n.bondHoldingPercent = 0.035f; n.tradeVolumePercent = 0.18f; n.financialLinkage = 0.22f;
                    n.trendGrowthRate = 2.2f; n.recessionProneness = 0.15f;
                    n.aggression = 0.15f;
                    n.briefing = "Resource-rich democracy and your most reliable partner. First to join a sanctions coalition on your side.";
                    n.nationColor = new Color32(0x00, 0xd0, 0x84, 0xff);
                }),
            };
        }
    }
}
