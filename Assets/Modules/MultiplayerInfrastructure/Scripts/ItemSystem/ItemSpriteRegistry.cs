using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 아이템 아이콘 스프라이트 조회 유틸리티입니다.
  ///
  /// 조회 우선순위:
  ///   1. 내부 캐시 (이미 로드된 적 있음)
  ///   2. Registry (RegistryType.IconSprite 에 등록된 항목)
  ///   3. Resources/Textures/Items/{identifier} 에서 로드 후 캐시 및 Registry 등록
  ///
  /// 스프라이트를 찾지 못하면 DefaultsResource.FallbackSprite 를 반환합니다.
  /// </summary>
  public static class ItemSpriteRegistry
  {
    /// <summary>Resources 아래 아이템 텍스처 루트 경로 (확장자 제외)</summary>
    private const string ItemTexturesPath = DefaultsItemRegistry.ItemTexturesPath;  // "Textures/Items"

    private static readonly Dictionary<string, Sprite> _cache = new(System.StringComparer.Ordinal);

    /// <summary>
    /// identifier에 해당하는 Sprite를 반환합니다.
    /// 캐시 미스 시 Registry → Resources 순으로 폴백하며, 찾으면 등록 및 캐시합니다.
    /// </summary>
    public static Sprite GetOrLoad(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return DefaultsResource.FallbackSprite;

      // 1. 캐시 확인
      if (_cache.TryGetValue(identifier, out var cached))
        return cached;

      // 2. 제네릭 레지스트리(IconSprite)에서 확인
      if (MultiplayerInfrastructure.Registry.Registry.TryGet<Sprite>(
            RegistryType.IconSprite, identifier, out var registrySprite)
          && registrySprite != null)
      {
        _cache[identifier] = registrySprite;
        return registrySprite;
      }

      // 3. Resources/Textures/Items/{identifier} 에서 로드
      var sprite = Resources.Load<Sprite>($"{ItemTexturesPath}/{identifier}");
      if (sprite != null)
      {
        _cache[identifier] = sprite;
        // 이후 조회 일관성을 위해 레지스트리에도 등록
        MultiplayerInfrastructure.Registry.Registry.Register(RegistryType.IconSprite, identifier, sprite);
        return sprite;
      }

      Debug.LogWarning(
        $"[ItemSpriteRegistry] '{identifier}' 스프라이트를 찾지 못했습니다. " +
        $"경로: Resources/{ItemTexturesPath}/{identifier}");
      return DefaultsResource.FallbackSprite;
    }

    /// <summary>특정 identifier의 캐시를 무효화합니다. 스프라이트가 교체될 때 사용합니다.</summary>
    public static void Invalidate(string identifier)
    {
      if (!string.IsNullOrWhiteSpace(identifier))
        _cache.Remove(identifier);
    }

    /// <summary>전체 캐시를 비웁니다.</summary>
    public static void InvalidateAll() => _cache.Clear();

    /// <summary>스프라이트를 수동으로 등록합니다. 런타임 에셋 번들 로드 등에 사용합니다.</summary>
    public static void Register(string identifier, Sprite sprite)
    {
      if (string.IsNullOrWhiteSpace(identifier) || sprite == null) return;
      _cache[identifier] = sprite;
      MultiplayerInfrastructure.Registry.Registry.Register(RegistryType.IconSprite, identifier, sprite);
    }
  }
}
