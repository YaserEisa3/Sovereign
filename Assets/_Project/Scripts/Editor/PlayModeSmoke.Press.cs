using UnityEngine;
using UnityEngine.UIElements;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// How the smoke test clicks things. The first version sent the event straight
    /// to the button, which proves the handler works and nothing else: it passed
    /// while every drawer covered the tab bar and five X buttons were bound to
    /// nothing. A person clicks a POINT on the screen, so this asks the panel what
    /// is actually at that point first, and fails if it is not the button.
    /// </summary>
    public static partial class PlayModeSmoke
    {
        static bool Press(Button button, string what)
        {
            if (button == null) { Finish(false, " " + what + " does not exist"); return false; }
            if (button.panel == null) { Finish(false, " " + what + " is not on any panel"); return false; }

            Vector2 point = button.worldBound.center;
            VisualElement picked = button.panel.Pick(point);
            if (picked == null || (picked != button && !button.Contains(picked)))
            {
                Finish(false, " " + what + " cannot be clicked - at its centre the pointer hits "
                              + Describe(picked) + " instead");
                return false;
            }

            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
            return true;
        }

        static string Describe(VisualElement element)
        {
            if (element == null) return "nothing";
            string name = string.IsNullOrEmpty(element.name) ? element.GetType().Name : "#" + element.name;
            VisualElement owner = element;
            while (owner.parent != null) owner = owner.parent;
            return name + " (in " + (string.IsNullOrEmpty(owner.name) ? owner.GetType().Name : owner.name) + ")";
        }

        static void Next(Stage stage) { _stage = stage; _settleFrames = 10; }

        static bool Require(bool condition, string failure)
        {
            if (!condition) Finish(false, " " + failure);
            return condition;
        }

        static Button Tab(string name)
        {
            VisualElement root = DashboardRoot();
            return root == null ? null : root.Q<Button>(name);
        }

        static VisualElement PanelRoot(GameObject panel)
        {
            return panel == null ? null : panel.GetComponent<UIDocument>().rootVisualElement;
        }

        static int CountChildren(VisualElement root, string name)
        {
            VisualElement element = root == null ? null : root.Q<VisualElement>(name);
            return element == null ? -1 : element.childCount;
        }

        static GameObject FindPanel(string name)
        {
            foreach (UIDocument document in Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (document.gameObject.name == name) return document.gameObject;
            return null;
        }

        static VisualElement DashboardRoot() { return PanelRoot(FindPanel("UIRoot")); }
    }
}
