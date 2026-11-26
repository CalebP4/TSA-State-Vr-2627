using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>Ordered list of scenarios shown on the main menu. Lives in Resources/FirstAidVR.</summary>
    [CreateAssetMenu(menuName = "First Aid VR/Scenario Catalog", fileName = "ScenarioCatalog")]
    public class ScenarioCatalog : ScriptableObject
    {
        public List<ScenarioDefinition> scenarios = new List<ScenarioDefinition>();
    }
}
