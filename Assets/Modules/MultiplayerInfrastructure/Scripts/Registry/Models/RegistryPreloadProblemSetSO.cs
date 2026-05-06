using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Problem Set SO", menuName = "MultiplayerInfrastructure/Registry Preload Problem Set SO")]
  public class RegistryPreloadProblemSetSO : ScriptableObject
  {
    public ProblemSetRegistryRequirement[] problemSetRegistryRequirements;
  }
}
