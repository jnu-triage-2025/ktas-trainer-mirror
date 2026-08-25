using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Problem Set SO", menuName = "Multiplayer Infrastructure/Registry Preload Problem Set SO")]
  public class RegistryPreloadProblemSetSO : ScriptableObject
  {
    public ProblemSetRegistryRequirement[] problemSetRegistryRequirements;
  }
}
