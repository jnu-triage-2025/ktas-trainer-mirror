using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Scenario Graph SO", menuName = "Multiplayer Infrastructure/Registry Preload Scenario Graph SO")]
  public class RegistryPreloadScenarioGraphSO : ScriptableObject
  {
    public ScenarioGraphRegistryRequirement[] scenarioGraphRegistryRequirements;
  }
}
