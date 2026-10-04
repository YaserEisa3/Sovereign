using UnityEngine;
using UnityEngine.UIElements;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// For the drawers whose systems do not exist yet. It exists so they can at least
    /// be CLOSED - without a controller their X button was bound to nothing, and
    /// opening one left the player stuck - and so they say plainly what is coming
    /// rather than showing empty lists that look broken.
    /// </summary>
    public class PlaceholderPanelController : DrawerPanelController
    {
        [Header("What this drawer will hold")]
        [Tooltip("An existing Label in this drawer's UXML to write the note into.")]
        [SerializeField] string noteElementName = "";

        [TextArea(2, 4)]
        [SerializeField] string note = "This drawer arrives in a later phase.";

        protected override void Build()
        {
            if (string.IsNullOrEmpty(noteElementName)) return;
            Label label = Root.Q<Label>(noteElementName);
            if (label != null) label.text = note;
        }

        protected override void Refresh(EconomyState state) { }
    }
}
