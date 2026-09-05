using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Tag
{
  /// <summary>
  /// 플레이어 태그 정의 목록을 관리하는 정적 서비스.
  ///
  /// <para>정의는 두 곳에서 온다.</para>
  /// <list type="number">
  ///   <item><c>Resources/Tag/*.tags.json</c> — 모듈이 내장하는 정의. 첫 조회 때 자동으로 읽는다.</item>
  ///   <item>데이터팩의 <c>tagDefinitions</c> — 데이터팩이 등록/해제될 때 함께 등록/해제된다.</item>
  /// </list>
  ///
  /// <para>
  /// 같은 식별자가 여러 출처에서 정의되면 나중에 등록된 정의가 우선하고, 그 출처가 해제되면
  /// 이전 정의로 되돌아간다. 정의되지 않은 태그는 <see cref="DefaultRequiresPermission"/> 을
  /// 따르며 기본값은 "권한 필요"다. 그래서 정의 파일에 실린 태그만 권한 없이 다룰 수 있다.
  /// </para>
  ///
  /// <para>
  /// 이 서비스는 권한 검사 정책만 담당한다. 태그 자체의 저장과 동기화는
  /// <see cref="PlayerTagService"/> 가 맡고, 어느 태그를 부여할지는 시나리오와 커맨드가 정한다.
  /// </para>
  /// </summary>
  public static class PlayerTagDefinitionService
  {
    /// <summary>내장 정의 파일을 찾는 Resources 하위 폴더.</summary>
    public const string ResourceFolder = "Tag";

    /// <summary>내장 정의 파일의 에셋 이름 접미사(<c>foo.tags.json</c> → 에셋 이름 <c>foo.tags</c>).</summary>
    public const string ResourceNameSuffix = ".tags";

    /// <summary>내장 정의 파일에서 온 등록의 출처 접두사.</summary>
    public const string BuiltInSourcePrefix = "resources:";

    private sealed class Registration
    {
      public string Source;
      public PlayerTagDefinition Definition;
    }

    // 식별자 → 등록 목록. 목록의 마지막 항목이 현재 유효한 정의다.
    private static readonly Dictionary<string, List<Registration>> _registrations = new(StringComparer.Ordinal);
    private static bool _builtInLoaded;

    /// <summary>
    /// 정의되지 않은 태그를 다룰 때 권한이 필요한지 여부. 기본값 true.
    /// </summary>
    public static bool DefaultRequiresPermission { get; set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
      // 도메인 리로드가 비활성인 환경에서 이전 실행의 등록이 남지 않도록 초기화한다.
      Clear();
    }

    /// <summary>
    /// 모든 등록을 지우고 내장 정의를 다시 읽을 수 있는 상태로 되돌린다. 테스트와 도메인 리로드 대응용.
    /// </summary>
    public static void Clear()
    {
      _registrations.Clear();
      _builtInLoaded = false;
      DefaultRequiresPermission = true;
    }

    // ── 내장 정의 로드 ────────────────────────────────────────────────────

    /// <summary>
    /// <c>Resources/Tag/*.tags.json</c> 파일을 한 번 읽어 등록한다. 조회 API 가 자동으로 호출한다.
    /// </summary>
    public static void EnsureLoaded()
    {
      if (_builtInLoaded)
        return;

      // 로드 중 재진입(Define → EnsureLoaded)을 막기 위해 먼저 표시한다.
      _builtInLoaded = true;

      TextAsset[] assets;
      try
      {
        assets = Resources.LoadAll<TextAsset>(ResourceFolder);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[PlayerTagDefinitionService] Failed to scan Resources/{ResourceFolder}: {ex.Message}");
        return;
      }

      if (assets == null)
        return;

      foreach (var asset in assets)
      {
        if (asset == null || string.IsNullOrEmpty(asset.name)
            || !asset.name.EndsWith(ResourceNameSuffix, StringComparison.OrdinalIgnoreCase))
          continue;

        if (!TryLoadFromJson(asset.text, BuiltInSourcePrefix + asset.name, out _, out string error))
          Debug.LogWarning($"[PlayerTagDefinitionService] Failed to load '{asset.name}': {error}");
      }
    }

    /// <summary>
    /// <see cref="PlayerTagDefinitionFile"/> 형식의 JSON 을 읽어 <paramref name="source"/> 출처로 등록한다.
    /// </summary>
    /// <returns>JSON 을 해석했으면 true. 항목 일부가 비어 있어도 나머지는 등록한다.</returns>
    public static bool TryLoadFromJson(string json, string source, out int registeredCount, out string error)
    {
      registeredCount = 0;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(json))
      {
        error = "Tag definition json is empty.";
        return false;
      }

      PlayerTagDefinitionFile file;
      try
      {
        file = JsonUtility.FromJson<PlayerTagDefinitionFile>(json);
      }
      catch (Exception ex)
      {
        error = $"Invalid tag definition json: {ex.Message}";
        return false;
      }

      if (file?.tags == null)
      {
        error = "Tag definition json requires a 'tags' array.";
        return false;
      }

      foreach (var definition in file.tags)
      {
        if (Define(definition, source))
          registeredCount++;
      }

      return true;
    }

    // ── 등록/해제 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 정의를 <paramref name="source"/> 출처로 등록한다. 같은 출처가 같은 식별자를 다시 정의하면 덮어쓴다.
    /// 다른 출처의 기존 정의보다 우선한다.
    /// </summary>
    public static bool Define(PlayerTagDefinition definition, string source)
    {
      if (definition == null || string.IsNullOrWhiteSpace(definition.identifier))
        return false;

      EnsureLoaded();

      string identifier = definition.identifier.Trim();
      string normalizedSource = source ?? string.Empty;

      if (!_registrations.TryGetValue(identifier, out var list))
      {
        list = new List<Registration>();
        _registrations[identifier] = list;
      }

      var stored = new PlayerTagDefinition(identifier, definition.requiresPermission, definition.description);
      for (int i = 0; i < list.Count; i++)
      {
        if (string.Equals(list[i].Source, normalizedSource, StringComparison.Ordinal))
        {
          list[i].Definition = stored;
          return true;
        }
      }

      list.Add(new Registration { Source = normalizedSource, Definition = stored });
      return true;
    }

    /// <summary>
    /// 식별자와 권한 요구 여부만으로 정의를 등록한다.
    /// </summary>
    public static bool Define(string identifier, bool requiresPermission, string source)
      => Define(new PlayerTagDefinition(identifier, requiresPermission), source);

    /// <summary>
    /// 특정 출처가 등록한 정의를 모두 해제한다. 같은 식별자에 다른 출처의 정의가 남아 있으면 그 정의가 유효해진다.
    /// </summary>
    /// <returns>해제된 정의 수.</returns>
    public static int Undefine(string source)
    {
      string normalizedSource = source ?? string.Empty;
      int removed = 0;
      var emptied = new List<string>();

      foreach (var pair in _registrations)
      {
        removed += pair.Value.RemoveAll(r => string.Equals(r.Source, normalizedSource, StringComparison.Ordinal));
        if (pair.Value.Count == 0)
          emptied.Add(pair.Key);
      }

      foreach (string key in emptied)
        _registrations.Remove(key);

      return removed;
    }

    // ── 조회 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 태그의 현재 유효한 정의를 반환한다. 정의되지 않았으면 false.
    /// </summary>
    public static bool TryGetDefinition(string tag, out PlayerTagDefinition definition)
    {
      definition = null;
      if (string.IsNullOrWhiteSpace(tag))
        return false;

      EnsureLoaded();

      if (_registrations.TryGetValue(tag.Trim(), out var list) && list.Count > 0)
      {
        definition = list[list.Count - 1].Definition;
        return true;
      }

      return false;
    }

    /// <summary>태그가 어느 출처에서든 정의되어 있는지 확인한다.</summary>
    public static bool IsDefined(string tag) => TryGetDefinition(tag, out _);

    /// <summary>
    /// 태그를 추가/제거/변경할 때 커맨드 권한이 필요한지 반환한다.
    /// 정의되지 않은 태그는 <see cref="DefaultRequiresPermission"/> 을 따른다.
    /// 빈 태그는 항상 권한을 요구한다(잘못된 입력이 면제 통로가 되지 않도록).
    /// </summary>
    public static bool RequiresPermission(string tag)
    {
      if (string.IsNullOrWhiteSpace(tag))
        return true;

      return TryGetDefinition(tag, out var definition)
        ? definition.requiresPermission
        : DefaultRequiresPermission;
    }

    /// <summary>현재 유효한 정의 목록을 식별자 순으로 반환한다.</summary>
    public static IReadOnlyList<PlayerTagDefinition> GetDefinitions()
    {
      EnsureLoaded();

      var result = new List<PlayerTagDefinition>(_registrations.Count);
      foreach (var pair in _registrations)
      {
        if (pair.Value.Count > 0)
          result.Add(pair.Value[pair.Value.Count - 1].Definition);
      }

      result.Sort((a, b) => string.CompareOrdinal(a.identifier, b.identifier));
      return result;
    }
  }
}
