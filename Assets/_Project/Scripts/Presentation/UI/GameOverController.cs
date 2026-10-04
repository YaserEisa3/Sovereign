using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 4 and 14. Listens for OnGameOver and shows the event popup with what
    /// happened and how long you lasted. It lives on an always-active object because
    /// the popup itself starts inactive, and an inactive object hears nothing.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] SimulationRunner runner;
        [Tooltip("Panel_EventPopup - the modal card the ending is shown on.")]
        [SerializeField] GameObject popupPanel;

        void OnEnable()
        {
            if (runner == null) return;
            runner.OnGameOver += Show;
            runner.OnVictory += ShowVictory;
        }

        void OnDisable()
        {
            if (runner == null) return;
            runner.OnGameOver -= Show;
            runner.OnVictory -= ShowVictory;
        }

        /// <summary>
        /// The other ending. It is shown on the same card, because a run reaching
        /// prosperity deserves the same stop-everything moment a revolt gets - and
        /// then the clock keeps running, because there is always further to go.
        /// </summary>
        void ShowVictory(EconomyState state)
        {
            if (popupPanel == null) return;
            popupPanel.SetActive(true);

            VisualElement root = popupPanel.GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            VictoryConfig victory = runner.Simulator.Victory.Config;
            int years = state.week / 52, weeks = state.week % 52;

            Set(root, "popup-category", "VICTORY");
            Set(root, "popup-severity", "PROSPERITY");
            Set(root, "popup-title", victory.title);
            Set(root, "popup-description", victory.summary
                + "\n\nIt took " + years + " years and " + weeks + " weeks, to Q" + state.Quarter + " " + state.Year + "."
                + "\nDebt " + (state.DebtToGdp * 100f).ToString("0") + "% of GDP, rated " + state.bonds.creditRating
                + ". Unemployment " + state.unemployment.ToString("0.0") + "%, infrastructure "
                + state.infrastructureHealth.ToString("0") + "/100."
                + "\nReal wages " + state.Series("realWage").Latest.ToString("0") + " against 100 at the start, approval "
                + state.approval.overall.ToString("0") + "%.");
            Set(root, "popup-footnote", "The clock keeps running. Nothing stops a country losing this again.");

            Button restart = root.Q<Button>("popup-open-panel");
            if (restart != null)
            {
                restart.text = "Start a new run";
                restart.clicked += Restart;
            }

            Button carryOn = root.Q<Button>("popup-dismiss");
            if (carryOn != null)
            {
                carryOn.text = "Carry on governing";
                carryOn.clicked += () => popupPanel.SetActive(false);
            }
        }

        void Show(EconomyState state)
        {
            if (popupPanel == null) return;
            popupPanel.SetActive(true);

            VisualElement root = popupPanel.GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            int years = state.week / 52, weeks = state.week % 52;
            Set(root, "popup-category", "GAME OVER");
            Set(root, "popup-severity", "REVOLT");
            Set(root, "popup-title", "The government has fallen");
            Set(root, "popup-description", state.approval.revoltReason
                + "\n\nYou governed for " + years + " years and " + weeks + " weeks, to Q" + state.Quarter + " " + state.Year + "."
                + "\nDebt " + (state.DebtToGdp * 100f).ToString("0") + "% of GDP, rated " + state.bonds.creditRating
                + ". Unemployment " + state.unemployment.ToString("0.0") + "%, inflation " + state.inflation.ToString("0.0") + "%."
                + "\nApproval " + state.approval.overall.ToString("0") + "% overall - poor " + state.approval.poor.ToString("0")
                + ", middle " + state.approval.middle.ToString("0") + ", wealthy " + state.approval.wealthy.ToString("0") + ".");
            Set(root, "popup-footnote", "The clock has stopped. The dashboard behind this card shows the state you left it in.");

            Button restart = root.Q<Button>("popup-open-panel");
            if (restart != null)
            {
                restart.text = "Start a new run";
                restart.clicked += Restart;
            }

            Button look = root.Q<Button>("popup-dismiss");
            if (look != null)
            {
                look.text = "Look at the wreckage";
                look.clicked += () => popupPanel.SetActive(false);
            }
        }

        void Restart()
        {
            popupPanel.SetActive(false);
            runner.Boot();
            runner.Speed = GameSpeed.Normal;
        }

        static void Set(VisualElement root, string name, string text)
        {
            Label label = root.Q<Label>(name);
            if (label != null) label.text = text;
        }
    }
}
