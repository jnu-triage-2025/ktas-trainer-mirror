using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Definitions
{
  public static class DefaultsResource
  {
    public const string ItemTexturesPath = DefaultsItemRegistry.ItemTexturesPath;
    public const string FallbackSpritePath = "Textures/fallback";
    private static Sprite _fallbackSprite;
    public static Sprite FallbackSprite
    {
      get
      {
        if (_fallbackSprite.IsUnityNull())
          _fallbackSprite = Resources.Load<Sprite>(FallbackSpritePath);
        return _fallbackSprite;
      }
    }
  }
}
