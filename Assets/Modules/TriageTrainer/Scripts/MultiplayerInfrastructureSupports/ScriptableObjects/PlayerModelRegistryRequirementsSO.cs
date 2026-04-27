using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [CreateAssetMenu(
    fileName = "New PlayerModel Registry Requirements SO",
    menuName = "TriageTrainer/Multiplayer Infrastructure/PlayerModel Registry Requirements SO")]
  public class PlayerModelRegistryRequirementsSO : ScriptableObject
  {
    public PlayerModelRegistryRequirement[] playerModelRegistryRequirements;
  }
}
