using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Icon Sprite SO", menuName = "MultiplayerInfrastructure/Registry Preload Icon Sprite SO")]
  public class RegistryPreloadIconSpriteSO : ScriptableObject
  {
    public IconSpriteRegistryRequirement[] iconSpriteRegistryRequirements;
  }
}
