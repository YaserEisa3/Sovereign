using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>Shared scene lookup for the economy tools.</summary>
    public static class EconomyTestAccess
    {
        public static SimulationRunner LoadRunner()
        {
            Scene scene = EditorSceneManager.OpenScene(ProjectBuilder.ScenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SimulationRunner found = root.GetComponentInChildren<SimulationRunner>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
