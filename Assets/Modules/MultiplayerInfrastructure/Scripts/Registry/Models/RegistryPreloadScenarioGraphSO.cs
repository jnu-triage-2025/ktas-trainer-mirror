using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Scenario Graph SO", menuName = "MultiplayerInfrastructure/Registry Preload Scenario Graph SO")]
  public class RegistryPreloadScenarioGraphSO : ScriptableObject
  {
    public ScenarioGraphRegistryRequirement[] scenarioGraphRegistryRequirements;
  }
}
