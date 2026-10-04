using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;
using Sovereign.Data;

namespace Sovereign.Presentation
{
    /// <summary>
    /// Shared behaviour for every drawer. UIDocument REBUILDS its visual tree each
    /// time the panel is enabled, so anything cached from a previous opening is
    /// stale - the rows are rebuilt and the close button rebound on every OnEnable.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class DrawerPanelController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] protected SimulationRunner runner;
        [SerializeField] protected GameDatabase database;
        [SerializeField] protected UITheme theme;
        [Tooltip("PolicyRow.uxml - every stepper in this drawer is stamped from it.")]
        [SerializeField] protected VisualTreeAsset policyRowTemplate;

        protected VisualElement Root { get; private set; }
        protected PolicyState Policy { get { return runner == null ? null : runner.Policy; } }

        protected virtual void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();
            Root = document.rootVisualElement;
            if (Root == null) return;

            // The document root spans the whole screen even though the drawer itself
            // only covers the data column. Left pickable, that invisible root swallows
            // every click on the dashboard beneath - including the tab that would close
            // this drawer. Only the drawer body should catch the pointer.
            Root.pickingMode = PickingMode.Ignore;

            // Bound before anything that can bail out, so a drawer can always be closed.
            Button close = Root.Q<Button>("close-panel");
            if (close != null) close.clicked += () => gameObject.SetActive(false);

            if (runner == null || runner.State == null) return;

            Build();
            runner.OnWeekTick += OnWeek;
            Refresh(runner.State);
        }

        protected virtual void OnDisable()
        {
            if (runner != null) runner.OnWeekTick -= OnWeek;
        }

        void OnWeek(EconomyState state) { Refresh(state); }

        protected abstract void Build();
        protected abstract void Refresh(EconomyState state);

        protected static string Billions(float value) { return "$" + value.ToString("#,0") + "B"; }

        protected static string SignedBillions(float value)
        {
            return (value >= 0f ? "+$" : "-$") + Mathf.Abs(value).ToString("#,0") + "B";
        }

        protected void SetLabel(string elementName, string text)
        {
            Label label = Root.Q<Label>(elementName);
            if (label != null) label.text = text;
        }
    }
}
