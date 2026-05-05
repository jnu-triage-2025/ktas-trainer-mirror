using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [CreateAssetMenu(
    fileName = "New EntityPreset Registry Requirements SO",
    menuName = "TriageTrainer/Multiplayer Infrastructure/EntityPreset Registry Requirements SO")]
  public class EntityPresetRegistryRequirementsSO : ScriptableObject
  {
    public EntityPresetRegistryRequirement[] entityPresetRegistryRequirements;
  }
}
