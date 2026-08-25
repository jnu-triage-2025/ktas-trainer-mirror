using System.Collections.Generic;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 장비 슬롯의 그림자 스프라이트를 제공하는 정적 클래스입니다.
  ///
  /// <para>
  /// 그림자 스프라이트는 <c>.png</c> 파일로 Resources 폴더에 존재하며,
  /// 리소스 경로가 장비 슬롯 타입별로 하드코딩됩니다.
  /// 최초 로드 후 캐시되어 재사용됩니다.
  /// </para>
  ///
  /// <para>
  /// Z-Ordering: 장비 슬롯에서 그림자는 z+2(UI 위, 아이템 아래)에 위치합니다.
  /// </para>
  /// </summary>
  public static class EquipmentShadowSpriteProvider
  {
    /// <summary>
    /// 장비 슬롯 타입별 그림자 스프라이트 Resources 경로 (하드코딩).
    /// 확장자(.png)는 포함하지 않습니다 — Unity 의 Resources.Load 규칙.
    /// </summary>
    private static readonly Dictionary<EquipmentSlotType, string> _shadowPathsByType = new()
    {
      { EquipmentSlotType.Glove, "Textures/Icons/glove-slot" },
    };

    private static readonly Dictionary<EquipmentSlotType, Texture2D> _cacheByType = new();

    /// <summary>
    /// 지정 장비 슬롯 타입에 해당하는 그림자 텍스처를 반환합니다.
    /// 최초 호출 시 Resources 에서 로드하며, 이후 캐시된 값을 반환합니다.
    /// 로드 실패 시 null 을 반환합니다.
    /// </summary>
    public static Texture2D GetShadowTexture(EquipmentSlotType slotType)
    {
      if (_cacheByType.TryGetValue(slotType, out var cached))
        return cached;

      if (!_shadowPathsByType.TryGetValue(slotType, out var path))
        return null;

      var texture = Resources.Load<Texture2D>(path);
      _cacheByType[slotType] = texture;
      return texture;
    }

    /// <summary>
    /// 지정 아이템이 특정 장비 슬롯에 장착되었을 때의 그림자 텍스처를 반환합니다.
    /// 현재 구현은 슬롯 타입 기반으로 그림자를 반환합니다(아이템과 무관).
    /// </summary>
    public static Texture2D GetShadowTextureForSlot(Item item, EquipmentSlotType slotType)
    {
      return GetShadowTexture(slotType);
    }

    /// <summary>캐시를 초기화합니다(Resources 언로드/도메인 리로드 시).</summary>
    public static void ClearCache()
    {
      _cacheByType.Clear();
    }
  }
}
