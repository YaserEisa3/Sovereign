using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 3.3 / Phase 0 step 4. The single object holding an Inspector reference to
    /// every ScriptableObject asset in the game, and the ONLY place anything looks up
    /// config. Nothing in this project calls Resources.Load or GameObject.Find - a
    /// system that needs a coefficient gets a [SerializeField] GameDatabase and asks.
    /// </summary>
    public class GameDatabase : MonoBehaviour
    {
        [Header("Parameters - GDD 3.5")]
        [SerializeField] MacroParameters macro;
        [SerializeField] PopulationParameters population;
        [SerializeField] WarParameters war;
        [SerializeField] InfrastructureParameters infrastructure;
        [SerializeField] ApprovalWeights approvalWeights;
        [SerializeField] UITheme uiTheme;
        [SerializeField] EventParameters eventParameters;
        [SerializeField] GeopoliticsParameters geopoliticsParameters;
        [SerializeField] AchievementParameters achievementParameters;

        [Header("Content")]
        [Tooltip("Headlines.txt - the ticker's headline templates, one TOPIC|headline per line.")]
        [SerializeField] TextAsset headlineLibrary;

        [Header("Sectors - GDD 6, seven of them")]
        [SerializeField] SectorDefinition[] sectors;

        [Header("Nations - GDD 15, the home nation plus six AI nations")]
        [SerializeField] NationDefinition[] nations;

        [Header("Fiscal - GDD 8")]
        [SerializeField] TaxDefinition[] taxes;
        [SerializeField] SpendingCategoryDefinition[] spendingCategories;

        [Header("Events - GDD 17")]
        [SerializeField] EventDefinition[] events;

        public MacroParameters Macro => macro;
        public PopulationParameters Population => population;
        public WarParameters War => war;
        public InfrastructureParameters Infrastructure => infrastructure;
        public ApprovalWeights ApprovalWeights => approvalWeights;
        public UITheme Theme => uiTheme;
        public EventParameters EventParameters => eventParameters;
        public GeopoliticsParameters GeopoliticsParameters => geopoliticsParameters;
        public AchievementParameters AchievementParameters => achievementParameters;
        public TextAsset HeadlineLibrary => headlineLibrary;

        public SectorDefinition[] Sectors => sectors;
        public NationDefinition[] Nations => nations;
        public TaxDefinition[] Taxes => taxes;
        public SpendingCategoryDefinition[] SpendingCategories => spendingCategories;
        public EventDefinition[] Events => events;

        public SectorDefinition GetSector(SectorId id)
        {
            for (int i = 0; i < sectors.Length; i++)
                if (sectors[i] != null && sectors[i].sectorId == id) return sectors[i];
            return null;
        }

        public NationDefinition PlayerNation
        {
            get
            {
                for (int i = 0; i < nations.Length; i++)
                    if (nations[i] != null && nations[i].isPlayerNation) return nations[i];
                return null;
            }
        }

        /// <summary>
        /// Phase 0 safety net. Catches the two mistakes that are invisible until the
        /// economy quietly misbehaves: an unassigned slot, and shares that do not sum to 1.
        /// </summary>
        [ContextMenu("Validate Database")]
        public bool Validate(bool logResults = true)
        {
            bool ok = true;
            ok &= Require(macro, "MacroParameters", logResults);
            ok &= Require(population, "PopulationParameters", logResults);
            ok &= Require(war, "WarParameters", logResults);
            ok &= Require(infrastructure, "InfrastructureParameters", logResults);
            ok &= Require(approvalWeights, "ApprovalWeights", logResults);
            ok &= Require(uiTheme, "UITheme", logResults);
            ok &= Require(eventParameters, "EventParameters", logResults);
            ok &= Require(geopoliticsParameters, "GeopoliticsParameters", logResults);
            ok &= Require(achievementParameters, "AchievementParameters", logResults);
            ok &= Require(headlineLibrary, "HeadlineLibrary", logResults);
            ok &= RequireAll(sectors, "Sectors", 7, logResults);
            ok &= RequireAll(nations, "Nations", 7, logResults);
            ok &= RequireAll(taxes, "Taxes", 1, logResults);
            ok &= RequireAll(spendingCategories, "SpendingCategories", 1, logResults);
            ok &= RequireAll(events, "Events", 20, logResults);

            if (sectors != null && sectors.Length > 0)
            {
                float gdp = 0f, emp = 0f;
                foreach (SectorDefinition s in sectors)
                {
                    if (s == null) continue;
                    gdp += s.gdpShare;
                    emp += s.employmentShare;
                }
                ok &= SumsToOne(gdp, "Sector GDP shares", logResults);
                ok &= SumsToOne(emp, "Sector employment shares", logResults);
            }

            if (approvalWeights != null)
            {
                float classShare = approvalWeights.poorShare + approvalWeights.middleShare + approvalWeights.wealthyShare;
                ok &= SumsToOne(classShare, "Income class shares", logResults);
            }

            if (logResults && ok) Debug.Log("GameDatabase: all references assigned and shares balanced.", this);
            return ok;
        }

        bool Require(Object asset, string label, bool log)
        {
            if (asset != null) return true;
            if (log) Debug.LogError("GameDatabase: " + label + " is not assigned.", this);
            return false;
        }

        bool RequireAll(Object[] assets, string label, int expectedMinimum, bool log)
        {
            if (assets == null || assets.Length < expectedMinimum)
            {
                if (log) Debug.LogError("GameDatabase: " + label + " expects at least " + expectedMinimum
                                        + " assets, found " + (assets == null ? 0 : assets.Length) + ".", this);
                return false;
            }
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != null) continue;
                if (log) Debug.LogError("GameDatabase: " + label + " slot " + i + " is empty.", this);
                return false;
            }
            return true;
        }

        bool SumsToOne(float total, string label, bool log)
        {
            if (Mathf.Abs(total - 1f) <= 0.005f) return true;
            if (log) Debug.LogWarning("GameDatabase: " + label + " sum to " + total.ToString("0.###") + ", expected 1.", this);
            return false;
        }
    }
}
