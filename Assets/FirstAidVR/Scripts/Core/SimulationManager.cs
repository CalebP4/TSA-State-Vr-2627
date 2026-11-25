using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstAidVR
{
    /// <summary>
    /// Remembers what the user picked on the main menu (scenario + mode) and moves
    /// between scenes. Static so it survives scene loads without a manager object.
    /// </summary>
    public static class SimulationManager
    {
        public const string MenuScene = "MainMenu";

        public static SimMode Mode { get; private set; } = SimMode.Training;
        public static string ScenarioId { get; private set; }
        public static bool HasSelection { get; private set; }
        public static ScenarioResult LastResult { get; set; }

        static ScenarioCatalog catalog;

        /// <summary>The list of scenarios, loaded from Resources/FirstAidVR/ScenarioCatalog.</summary>
        public static ScenarioCatalog Catalog
        {
            get
            {
                if (catalog == null) catalog = Resources.Load<ScenarioCatalog>("FirstAidVR/ScenarioCatalog");
                return catalog;
            }
        }

        public static IReadOnlyList<ScenarioDefinition> Scenarios =>
            Catalog != null ? Catalog.scenarios : (IReadOnlyList<ScenarioDefinition>)new List<ScenarioDefinition>();

        public static ScenarioDefinition Find(string id)
        {
            foreach (var s in Scenarios) if (s != null && s.id == id) return s;
            return null;
        }

        public static void Select(string scenarioId, SimMode mode)
        {
            ScenarioId = scenarioId;
            Mode = mode;
            HasSelection = true;
        }

        public static void StartScenario(string scenarioId, SimMode mode)
        {
            var def = Find(scenarioId);
            if (def == null) { Debug.LogError("[FA] Unknown scenario " + scenarioId); return; }
            Select(scenarioId, mode);
            SceneManager.LoadScene(def.sceneName);
        }

        public static void Restart()
        {
            if (ScenarioId != null && Find(ScenarioId) != null) StartScenario(ScenarioId, Mode);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static void ReturnToMenu() => SceneManager.LoadScene(MenuScene);
    }
}
