using System;
using System.Reflection;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 이 특성(Attribute)이 적용된 <see cref="Item"/> 파생 클래스는
  /// Glove(장갑) 장비 슬롯에 장착할 수 있음을 선언합니다.
  ///
  /// <para>
  /// 적용 대상: Item 파생 클래스 (클래스 선언부 위)
  /// </para>
  /// <example>
  /// <code>
  /// [EquippableGlove]
  /// public class SterileGloves : MedicalItem
  /// {
  ///   public const string Identifier = "sterile_gloves";
  /// }
  /// </code>
  /// </example>
  ///
  /// <para>
  /// <c>Inherited = true</c> 이므로 부모 클래스에 적용하면 자식 클래스에도 자동 상속됩니다.
  /// </para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
  public sealed class EquippableGloveAttribute : Attribute { }

  /// <summary>
  /// 장비 Attribute 검사를 위한 정적 헬퍼 클래스입니다.
  /// Item 인스턴스의 타입에 적용된 장비 Attribute를 리플렉션으로 확인합니다.
  /// 결과는 타입별로 캐시되어 반복 조회 시 O(1)입니다.
  /// </summary>
  public static class EquipmentAttributeHelper
  {
    private static readonly System.Collections.Generic.Dictionary<Type, bool> _gloveCacheByType = new();

    /// <summary>
    /// 지정 Item 인스턴스의 클래스에 <see cref="EquippableGloveAttribute"/> 가
    /// 적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.
    /// </summary>
    public static bool IsEquippableGlove(Item item)
    {
      if (item == null) return false;
      return IsEquippableGlove(item.GetType());
    }

    /// <summary>
    /// 지정 Type에 <see cref="EquippableGloveAttribute"/> 가
    /// 적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.
    /// </summary>
    public static bool IsEquippableGlove(Type itemType)
    {
      if (itemType == null) return false;
      if (_gloveCacheByType.TryGetValue(itemType, out var cached)) return cached;
      bool has = itemType.GetCustomAttribute<EquippableGloveAttribute>() != null;
      _gloveCacheByType[itemType] = has;
      return has;
    }

    /// <summary>캐시를 초기화합니다(테스트/도메인 리로드 용).</summary>
    public static void ClearCache()
    {
      _gloveCacheByType.Clear();
    }
  }
}
