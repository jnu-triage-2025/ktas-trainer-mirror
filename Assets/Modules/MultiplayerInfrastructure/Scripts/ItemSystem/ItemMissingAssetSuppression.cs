using System;
using System.Collections.Generic;
using System.Reflection;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 아이템 3D 모델(프리팹) 및 아이콘 스프라이트 누락 warning의 노이즈를 줄이기 위한 억제 헬퍼입니다.
  ///
  /// <para>
  /// <b>선언적(Attribute) 억제</b>: <see cref="IntendedMissing3DModelAttribute"/> 또는
  /// <see cref="IntendedMissingItemSpriteAttribute"/> 를 Item 파생 클래스에 적용하면
  /// 해당 클래스(및 <c>Inherited=true</c> 로 자식 클래스)의 누락 warning이 자동 생략됩니다.
  /// </para>
  ///
  /// <para>
  /// <b>프로그래밍(RegisterIdentifier) 억제</b>: Item 클래스가 아닌 식별자(UI 아이콘 등)는
  /// <see cref="RegisterSuppressedSpriteIdentifier"/> / <see cref="RegisterSuppressedModelIdentifier"/>
  /// 로 등록하면 해당 identifier의 warning이 생략됩니다.
  /// </para>
  /// </summary>
  public static class ItemMissingAssetSuppression
  {
    private const BindingFlags ConstFlags =
      BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    // Type 기반 캐시 (Item 인스턴스가 있을 때 사용 — O(1) 반복 조회)
    private static readonly Dictionary<Type, bool> _modelCacheByType = new();
    private static readonly Dictionary<Type, bool> _spriteCacheByType = new();

    // Identifier 기반 캐시 (Item 인스턴스가 없을 때 사용)
    private static readonly HashSet<string> _suppressedModelIdentifiers = new(StringComparer.Ordinal);
    private static readonly HashSet<string> _suppressedSpriteIdentifiers = new(StringComparer.Ordinal);

    // 프로그래밍 등록 (비 Item 식별자용)
    private static readonly HashSet<string> _registeredModelIdentifiers = new(StringComparer.Ordinal);
    private static readonly HashSet<string> _registeredSpriteIdentifiers = new(StringComparer.Ordinal);

    private static bool _scanned;

    // ─── 초기화 ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 로딩된 모든 어셈블리에서 <see cref="Item"/> 파생 클래스를 스캔하여
    /// <see cref="IntendedMissing3DModelAttribute"/> / <see cref="IntendedMissingItemSpriteAttribute"/>
    /// 가 적용된 클래스의 <c>Identifier</c> const 값을 캐시에 등록합니다.
    /// 최초 호출 시 1회만 실행되며 이후 호출은 무시됩니다.
    /// </summary>
    public static void EnsureScanned()
    {
      if (_scanned) return;
      _scanned = true;
      ScanItemSubclasses();
    }

    private static void ScanItemSubclasses()
    {
      var itemType = typeof(Item);
      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        Type[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types; }
        catch { continue; }

        if (types == null) continue;

        foreach (var t in types)
        {
          try
          {
            if (t == null || t.IsAbstract || !itemType.IsAssignableFrom(t)) continue;

            bool hasModel = t.GetCustomAttribute<IntendedMissing3DModelAttribute>() != null;
            bool hasSprite = t.GetCustomAttribute<IntendedMissingItemSpriteAttribute>() != null;

            if (!hasModel && !hasSprite) continue;

            // Identifier const 값을 리플렉션으로 읽기
            var idField = t.GetField("Identifier", ConstFlags);
            string identifier = idField?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(identifier)) continue;

            if (hasModel)  _suppressedModelIdentifiers.Add(identifier);
            if (hasSprite) _suppressedSpriteIdentifiers.Add(identifier);
          }
          catch
          {
            // 개별 타입 리플렉션 실패(TypeLoadException 등)는 무시하고 다음 타입으로 진행
          }
        }
      }
    }

    // ─── Type 기반 검사 (Item 인스턴스가 있을 때 — 권장) ──────────────────────

    /// <summary>
    /// 지정 Item 인스턴스의 클래스에 <see cref="IntendedMissing3DModelAttribute"/> 가
    /// 적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.
    /// </summary>
    public static bool ShouldSuppressModelMissingWarning(Item item)
    {
      if (item == null) return false;
      return ShouldSuppressModelMissingWarning(item.GetType());
    }

    /// <summary>
    /// 지정 Type에 <see cref="IntendedMissing3DModelAttribute"/> 가
    /// 적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.
    /// </summary>
    public static bool ShouldSuppressModelMissingWarning(Type itemType)
    {
      if (itemType == null) return false;
      if (_modelCacheByType.TryGetValue(itemType, out var cached)) return cached;
      bool has = itemType.GetCustomAttribute<IntendedMissing3DModelAttribute>() != null;
      _modelCacheByType[itemType] = has;
      return has;
    }

    /// <summary>
    /// 지정 Item 인스턴스의 클래스에 <see cref="IntendedMissingItemSpriteAttribute"/> 가
    /// 적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.
    /// </summary>
    public static bool ShouldSuppressSpriteMissingWarning(Item item)
    {
      if (item == null) return false;
      return ShouldSuppressSpriteMissingWarning(item.GetType());
    }

    /// <summary>
    /// 지정 Type에 <see cref="IntendedMissingItemSpriteAttribute"/> 가
    /// 적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.
    /// </summary>
    public static bool ShouldSuppressSpriteMissingWarning(Type itemType)
    {
      if (itemType == null) return false;
      if (_spriteCacheByType.TryGetValue(itemType, out var cached)) return cached;
      bool has = itemType.GetCustomAttribute<IntendedMissingItemSpriteAttribute>() != null;
      _spriteCacheByType[itemType] = has;
      return has;
    }

    // ─── Identifier 기반 검사 (Item 인스턴스가 없을 때) ───────────────────────

    /// <summary>
    /// 지정 identifier의 3D 모델 누락 warning을 생략해야 하는지 여부입니다.
    /// Item 어트리뷰트 스캔 결과 및 <see cref="RegisterSuppressedModelIdentifier"/> 등록을 확인합니다.
    /// </summary>
    public static bool ShouldSuppressModelMissingWarning(string identifier)
    {
      EnsureScanned();
      return !string.IsNullOrWhiteSpace(identifier)
             && (_suppressedModelIdentifiers.Contains(identifier)
                 || _registeredModelIdentifiers.Contains(identifier));
    }

    /// <summary>
    /// 지정 identifier의 스프라이트 누락 warning을 생략해야 하는지 여부입니다.
    /// Item 어트리뷰트 스캔 결과 및 <see cref="RegisterSuppressedSpriteIdentifier"/> 등록을 확인합니다.
    /// </summary>
    public static bool ShouldSuppressSpriteMissingWarning(string identifier)
    {
      EnsureScanned();
      return !string.IsNullOrWhiteSpace(identifier)
             && (_suppressedSpriteIdentifiers.Contains(identifier)
                 || _registeredSpriteIdentifiers.Contains(identifier));
    }

    // ─── 프로그래밍 등록 (비 Item 식별자용) ────────────────────────────────────

    /// <summary>
    /// Item 클래스가 아닌 identifier(예: UI 아이콘)의 3D 모델 누락 warning을 억제하도록 등록합니다.
    /// </summary>
    public static void RegisterSuppressedModelIdentifier(string identifier)
    {
      if (!string.IsNullOrWhiteSpace(identifier))
        _registeredModelIdentifiers.Add(identifier);
    }

    /// <summary>
    /// Item 클래스가 아닌 identifier(예: UI 아이콘)의 스프라이트 누락 warning을 억제하도록 등록합니다.
    /// </summary>
    public static void RegisterSuppressedSpriteIdentifier(string identifier)
    {
      if (!string.IsNullOrWhiteSpace(identifier))
        _registeredSpriteIdentifiers.Add(identifier);
    }

    /// <summary>등록된 모든 프로그래밍 억제 목록을 비웁니다(테스트/초기화용).</summary>
    public static void Clear()
    {
      _registeredModelIdentifiers.Clear();
      _registeredSpriteIdentifiers.Clear();
    }
  }
}
