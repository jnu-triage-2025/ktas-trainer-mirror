using System;
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
    /// <see cref="GetOrLoadIconSprite(string, Type)"/> 를 사용할 수 없는 경우에만 사용합니다.
    /// </summary>
    public static Sprite GetOrLoadIconSprite(string identifier)
      => GetOrLoadIconSprite(identifier, null);

    /// <summary>
    /// identifier에 해당하는 아이콘 스프라이트를 반환합니다.
    ///
    /// 조회 우선순위:
    ///   1. IconSprite 레지스트리 (이미 등록된 Sprite 또는 string 경로로부터 지연 로드)
    ///   2. Resources/{DefaultsItemRegistry.ItemTexturesPath}/{identifier} 에서 직접 로드
    ///   3. DefaultsResource.FallbackSprite 반환
    ///
    /// <paramref name="itemType"/> 을 전달하면 <see cref="IntendedMissingItemSpriteAttribute"/>
    /// 기반 Type 검사를 우선 수행하여 더 정확한 억제 판정이 가능합니다.
    /// </summary>
    public static Sprite GetOrLoadIconSprite(string identifier, Type itemType)
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

      // 3. 억제 판정: Type 기반(정확) → identifier 기반(폴백)
      bool suppressed = itemType != null
        ? ItemMissingAssetSuppression.ShouldSuppressSpriteMissingWarning(itemType)
        : ItemMissingAssetSuppression.ShouldSuppressSpriteMissingWarning(identifier);

      if (!suppressed)
      {
        string hint = itemType != null
          ? $"{itemType.Name} 클래스에 [IntendedMissingItemSprite] 특성을 적용하세요."
          : $"의도된 누락이면 해당 Item 클래스에 [IntendedMissingItemSprite] 특성을 적용하거나, " +
            $"Item 클래스가 아닌 식별자라면 " +
            $"{nameof(ItemMissingAssetSuppression)}.{nameof(ItemMissingAssetSuppression.RegisterSuppressedSpriteIdentifier)}" +
            $"(\"{identifier}\") 로 등록하세요.";

        Debug.LogWarning(
          $"[Registry] IconSprite '{identifier}' 를 찾지 못했습니다. " +
          $"경로: Resources/{DefaultsItemRegistry.ItemTexturesPath}/{identifier} " + hint);
      }
      return DefaultsResource.FallbackSprite;
    }

    /// <summary>스프라이트를 IconSprite 레지스트리에 직접 등록합니다.</summary>
    public static void RegisterIconSprite(string identifier, Sprite sprite)
    {
      if (string.IsNullOrWhiteSpace(identifier) || sprite == null)
        return;
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
