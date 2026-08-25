using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Icon Sprite SO", menuName = "Multiplayer Infrastructure/Registry Preload Icon Sprite SO")]
  public class RegistryPreloadIconSpriteSO : ScriptableObject
  {
    public IconSpriteRegistryRequirement[] iconSpriteRegistryRequirements;
  }
}
