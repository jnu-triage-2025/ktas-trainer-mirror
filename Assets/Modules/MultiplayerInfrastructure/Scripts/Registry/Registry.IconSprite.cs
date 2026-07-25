using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.ItemSystem;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    // =========================================================================
    // IconSprite helpers
    // =========================================================================

    /// <summary>
    /// identifier에 해당하는 아이콘 스프라이트를 반환합니다.
    ///
    /// 조회 우선순위:
    ///   1. IconSprite 레지스트리 (이미 등록된 Sprite 또는 string 경로로부터 지연 로드)
    ///   2. Resources/{DefaultsItemRegistry.ItemTexturesPath}/{identifier} 에서 직접 로드
    ///   3. DefaultsResource.FallbackSprite 반환
    /// </summary>
    public static Sprite GetOrLoadIconSprite(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return DefaultsResource.FallbackSprite;

      // 1. 레지스트리 확인 (string 경로의 지연 로드까지 처리됨)
      if (TryGet<Sprite>(RegistryType.IconSprite, identifier, out var sprite) && sprite != null)
        return sprite;

      // 2. 관례 경로에서 Resources.Load 시도
      sprite = Resources.Load<Sprite>($"{DefaultsItemRegistry.ItemTexturesPath}/{identifier}");
      if (sprite != null)
      {
        Register(RegistryType.IconSprite, identifier, sprite);
        return sprite;
      }

      if (!ItemMissingAssetSuppression.ShouldSuppressSpriteMissingWarning(identifier))
      {
        Debug.LogWarning(
          $"[Registry] IconSprite '{identifier}' 를 찾지 못했습니다. " +
          $"경로: Resources/{DefaultsItemRegistry.ItemTexturesPath}/{identifier} " +
          $"의도된 누락이면 해당 Item 클래스에 [IntendedMissingItemSprite] 특성을 적용하거나, " +
          $"Item 클래스가 아닌 식별자라면 " +
          $"{nameof(ItemMissingAssetSuppression)}.{nameof(ItemMissingAssetSuppression.RegisterSuppressedSpriteIdentifier)}" +
          $"(\"{identifier}\") 로 등록하세요.");
      }
      return DefaultsResource.FallbackSprite;
    }

    /// <summary>스프라이트를 IconSprite 레지스트리에 직접 등록합니다.</summary>
    public static void RegisterIconSprite(string identifier, Sprite sprite)
    {
      if (string.IsNullOrWhiteSpace(identifier) || sprite == null) return;
      Register(RegistryType.IconSprite, identifier, sprite);
    }

    /// <summary>특정 identifier의 IconSprite 등록을 제거합니다.</summary>
    public static void InvalidateIconSprite(string identifier)
    {
      if (!string.IsNullOrWhiteSpace(identifier))
        Unregister(RegistryType.IconSprite, identifier);
    }

    /// <summary>IconSprite 레지스트리 전체를 비웁니다.</summary>
    public static void InvalidateAllIconSprites()
    {
      EnsureBuiltInRegistryInitialized();
      _iconSpriteRegistry.Clear();
    }
  }
}
