using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 20 Phase 5: attached to each Nation_* object, drives the colour of its name
    /// and its status line from the live relationship. Allies go green, rivals go red,
    /// and the name says so before the ticker does.
    /// </summary>
    public class NationView : MonoBehaviour
    {
        [SerializeField] SimulationRunner runner;
        [SerializeField] UITheme theme;
        [Tooltip("This nation's position in GameDatabase.Nations.")]
        [SerializeField] int nationIndex;
        [Tooltip("The country's name on the map - it carries the relationship in its colour.")]
        [SerializeField] TextMesh nameLabel;
        [SerializeField] TextMesh statusLabel;

        void OnEnable() { if (runner != null) runner.OnWeekTick += Refresh; }
        void OnDisable() { if (runner != null) runner.OnWeekTick -= Refresh; }
        void Start() { if (runner != null && runner.State != null) Refresh(runner.State); }

        void Refresh(EconomyState state)
        {
            if (nationIndex < 0 || nationIndex >= state.geopolitics.nations.Count || theme == null) return;
            NationProfile profile = runner.Simulator.Geopolitics.Config.nations[nationIndex];
            if (profile.isPlayer) return;

            NationState n = state.geopolitics.nations[nationIndex];
            Color colour = n.relationship >= 0f
                ? Color.Lerp(theme.neutralNation, theme.allyNation, n.relationship / 100f)
                : Color.Lerp(theme.neutralNation, theme.hostileNation, -n.relationship / 100f);

            if (nameLabel != null) nameLabel.color = colour;

            if (statusLabel == null) return;
            string status = "REL " + n.relationship.ToString("+0;-0");
            if (n.sanctioningYou) status += "  SANCTIONS";
            else if (!n.buyingBonds) status += "  NOT BUYING";
            else if (n.theirTariffOnYou > 0.5f) status += "  TARIFF " + n.theirTariffOnYou.ToString("0") + "%";
            statusLabel.text = status;
        }
    }
}
