using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiplayerInfrastructure.Session
{
  /// <summary>사람이 입력한 문자열이 어떤 방식으로 플레이어와 일치했는지 나타낸다.</summary>
  public enum PlayerNameMatchKind
  {
    None = 0,
    /// <summary>식별자(UUID) 완전 일치.</summary>
    Identifier,
    /// <summary>표시 이름 완전 일치(대소문자 무시).</summary>
    DisplayName,
    /// <summary>공백·구두점을 무시한 표시 이름 일치.</summary>
    DisplayNameLoose,
    /// <summary>식별자 앞부분 일치.</summary>
    IdentifierPrefix,
    /// <summary>표시 이름 앞부분 일치.</summary>
    DisplayNamePrefix,
    /// <summary>공백·구두점을 무시한 표시 이름 앞부분 일치.</summary>
    DisplayNameLoosePrefix,
    /// <summary>표시 이름 부분 문자열 일치.</summary>
    DisplayNameContains,
    /// <summary>공백·구두점을 무시한 표시 이름 부분 문자열 일치.</summary>
    DisplayNameLooseContains,
  }

  /// <summary>
  /// 사람이 입력한 이름·식별자 문자열 하나를 <see cref="UserDescriptor"/>로 해석하는 단일 모듈.
  ///
  /// 커맨드·UI·시나리오 등 플레이어를 문자열로 지정하는 모든 지점이 이 모듈을 거치도록 해서,
  /// 호출 지점마다 서로 다른 규칙으로 이름을 찾다가 실패하는 문제를 막는다.
  /// 대상 선택자(@a 등)나 FishNet 연결 해석은 <c>PlayerTargetResolver</c>가 담당한다.
  ///
  /// 일치 단계는 <see cref="PlayerNameMatchKind"/> 순서대로 평가하며, 처음으로 후보가 나온
  /// 단계에서 멈춘다. 완전 일치 단계는 동명이인이 있어도 식별자 순으로 하나를 확정하고,
  /// 추측성 단계(앞부분·부분 문자열)는 후보가 둘 이상이면 모호함으로 처리한다.
  /// </summary>
  public static class PlayerNameQuery
  {
    /// <summary>식별자 앞부분 일치를 허용하는 최소 입력 길이.</summary>
    public const int MinimumIdentifierPrefixLength = 4;

    /// <summary>추측성 단계(앞부분·부분 문자열)를 허용하는 최소 입력 길이.</summary>
    public const int MinimumFuzzyLength = 2;

    private static readonly PlayerNameMatchKind[] MatchOrder =
    {
      PlayerNameMatchKind.Identifier,
      PlayerNameMatchKind.DisplayName,
      PlayerNameMatchKind.DisplayNameLoose,
      PlayerNameMatchKind.IdentifierPrefix,
      PlayerNameMatchKind.DisplayNamePrefix,
      PlayerNameMatchKind.DisplayNameLoosePrefix,
      PlayerNameMatchKind.DisplayNameContains,
      PlayerNameMatchKind.DisplayNameLooseContains,
    };

    // ── 정규화 ───────────────────────────────────────────────────────────────

    /// <summary>앞뒤 공백과 감싼 따옴표를 제거한다. 채팅 토크나이저가 따옴표를 남기므로 필요하다.</summary>
    public static string Normalize(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return string.Empty;

      string value = raw.Trim();

      while (value.Length >= 2
             && (value[0] == '"' || value[0] == '\'')
             && value[^1] == value[0])
      {
        value = value.Substring(1, value.Length - 2).Trim();
      }

      return value;
    }

    /// <summary>
    /// 느슨한 비교용 정규화. 소문자로 낮추고 공백·구두점을 제거해서
    /// "Kim Cheolsu", "kim_cheolsu", "kimcheolsu"가 같은 값이 되게 한다.
    /// </summary>
    public static string NormalizeLoose(string raw)
    {
      string value = Normalize(raw);
      if (value.Length == 0)
        return string.Empty;

      var builder = new StringBuilder(value.Length);
      foreach (char character in value)
      {
        if (char.IsWhiteSpace(character))
          continue;
        if (char.IsLetterOrDigit(character))
          builder.Append(char.ToLowerInvariant(character));
      }

      return builder.ToString();
    }

    // ── 조회 ─────────────────────────────────────────────────────────────────

    /// <summary>현재 등록된 모든 설명자를 식별자 순으로 반환한다.</summary>
    public static IReadOnlyList<UserDescriptor> Snapshot()
      => UserDescriptorService.GetAll().Values
        .Where(descriptor => descriptor != null)
        .OrderBy(descriptor => descriptor.Identifier, StringComparer.Ordinal)
        .ToList();

    /// <summary>토큰과 일치하는 후보를 가장 강한 단계 하나만 골라 반환한다.</summary>
    public static IReadOnlyList<UserDescriptor> FindAll(string token, out PlayerNameMatchKind kind)
      => FindAll(token, Snapshot(), out kind);

    /// <summary>지정한 후보 집합 안에서 토큰과 일치하는 후보를 찾는다. 테스트·부분 집합 조회용.</summary>
    public static IReadOnlyList<UserDescriptor> FindAll(
      string token,
      IEnumerable<UserDescriptor> source,
      out PlayerNameMatchKind kind)
    {
      kind = PlayerNameMatchKind.None;

      string normalized = Normalize(token);
      if (normalized.Length == 0)
        return Array.Empty<UserDescriptor>();

      var candidates = (source ?? Array.Empty<UserDescriptor>())
        .Where(descriptor => descriptor != null)
        .OrderBy(descriptor => descriptor.Identifier, StringComparer.Ordinal)
        .ToList();

      if (candidates.Count == 0)
        return Array.Empty<UserDescriptor>();

      string loose = NormalizeLoose(normalized);

      foreach (PlayerNameMatchKind stage in MatchOrder)
      {
        var matched = candidates
          .Where(descriptor => MatchesStage(descriptor, normalized, loose, stage))
          .ToList();

        if (matched.Count == 0)
          continue;

        kind = stage;
        return matched;
      }

      return Array.Empty<UserDescriptor>();
    }

    /// <summary>
    /// 토큰을 한 명으로 확정한다. 완전 일치 단계는 동명이인이 있어도 식별자 순으로 확정하고,
    /// 추측성 단계에서 후보가 둘 이상이면 <paramref name="ambiguous"/>를 채우고 실패한다.
    /// </summary>
    public static bool TryResolve(
      string token,
      out UserDescriptor descriptor,
      out IReadOnlyList<UserDescriptor> ambiguous,
      out PlayerNameMatchKind kind)
      => TryResolve(token, Snapshot(), out descriptor, out ambiguous, out kind);

    /// <summary>지정한 후보 집합 안에서 토큰을 한 명으로 확정한다.</summary>
    public static bool TryResolve(
      string token,
      IEnumerable<UserDescriptor> source,
      out UserDescriptor descriptor,
      out IReadOnlyList<UserDescriptor> ambiguous,
      out PlayerNameMatchKind kind)
    {
      descriptor = null;
      ambiguous = Array.Empty<UserDescriptor>();

      var matched = FindAll(token, source, out kind);
      if (matched.Count == 0)
        return false;

      if (matched.Count == 1 || IsExactStage(kind))
      {
        descriptor = matched[0];
        return true;
      }

      ambiguous = matched;
      kind = PlayerNameMatchKind.None;
      return false;
    }

    /// <summary>식별자로만 조회한다. 완전 일치 후 앞부분 일치를 시도한다.</summary>
    public static bool TryResolveByIdentifier(
      string token,
      out UserDescriptor descriptor,
      out IReadOnlyList<UserDescriptor> ambiguous)
    {
      descriptor = null;
      ambiguous = Array.Empty<UserDescriptor>();

      string normalized = Normalize(token);
      if (normalized.Length == 0)
        return false;

      if (UserDescriptorService.TryGetByIdentifier(normalized, out descriptor) && descriptor != null)
        return true;

      descriptor = null;
      if (normalized.Length < MinimumIdentifierPrefixLength)
        return false;

      var matched = Snapshot()
        .Where(candidate => candidate.Identifier != null
          && candidate.Identifier.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
        .ToList();

      if (matched.Count == 1)
      {
        descriptor = matched[0];
        return true;
      }

      if (matched.Count > 1)
        ambiguous = matched;

      return false;
    }

    /// <summary>표시 이름으로만 조회한다. 완전 일치 → 느슨한 일치 → 앞부분 → 부분 문자열 순으로 시도한다.</summary>
    public static bool TryResolveByDisplayName(
      string token,
      out UserDescriptor descriptor,
      out IReadOnlyList<UserDescriptor> ambiguous)
    {
      descriptor = null;
      ambiguous = Array.Empty<UserDescriptor>();

      string normalized = Normalize(token);
      if (normalized.Length == 0)
        return false;

      string loose = NormalizeLoose(normalized);
      var candidates = Snapshot();

      foreach (PlayerNameMatchKind stage in MatchOrder)
      {
        if (stage == PlayerNameMatchKind.Identifier || stage == PlayerNameMatchKind.IdentifierPrefix)
          continue;

        var matched = candidates
          .Where(candidate => MatchesStage(candidate, normalized, loose, stage))
          .ToList();

        if (matched.Count == 0)
          continue;

        if (matched.Count == 1 || IsExactStage(stage))
        {
          descriptor = matched[0];
          return true;
        }

        ambiguous = matched;
        return false;
      }

      return false;
    }

    /// <summary>오류 메시지에 넣을 후보 목록 문자열을 만든다.</summary>
    public static string DescribeCandidates(IEnumerable<UserDescriptor> descriptors, int maximum = 5)
    {
      if (descriptors == null)
        return string.Empty;

      var names = descriptors
        .Where(descriptor => descriptor != null)
        .Select(Describe)
        .ToList();

      if (names.Count == 0)
        return string.Empty;

      if (names.Count <= maximum)
        return string.Join(", ", names);

      return string.Join(", ", names.Take(maximum)) + $", ... (+{names.Count - maximum})";
    }

    /// <summary>한 명을 사람이 읽을 수 있는 형태로 표기한다.</summary>
    public static string Describe(UserDescriptor descriptor)
    {
      if (descriptor == null)
        return "Unknown";

      string identifier = descriptor.Identifier ?? string.Empty;
      string shortIdentifier = identifier.Length > 8 ? identifier.Substring(0, 8) : identifier;

      if (string.IsNullOrWhiteSpace(descriptor.DisplayName))
        return shortIdentifier.Length > 0 ? shortIdentifier : "Unknown";

      return shortIdentifier.Length > 0
        ? $"{descriptor.DisplayName} (id:{shortIdentifier})"
        : descriptor.DisplayName;
    }

    /// <summary>자동 완성 후보로 쓸 표시 이름 목록. 공백이 있으면 따옴표로 감싼다.</summary>
    public static IReadOnlyList<string> CollectDisplayNameSuggestions()
      => Snapshot()
        .Select(descriptor => descriptor.DisplayName)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Select(Quote)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>공백이 포함된 이름을 커맨드 토큰으로 쓸 수 있도록 따옴표로 감싼다.</summary>
    public static string Quote(string value)
    {
      if (string.IsNullOrEmpty(value))
        return value;

      return value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
    }

    // ── 내부 ─────────────────────────────────────────────────────────────────

    private static bool IsExactStage(PlayerNameMatchKind kind)
      => kind == PlayerNameMatchKind.Identifier
         || kind == PlayerNameMatchKind.DisplayName
         || kind == PlayerNameMatchKind.DisplayNameLoose;

    private static bool MatchesStage(
      UserDescriptor descriptor,
      string normalized,
      string loose,
      PlayerNameMatchKind kind)
    {
      string identifier = descriptor.Identifier ?? string.Empty;
      string displayName = descriptor.DisplayName?.Trim() ?? string.Empty;
      string looseDisplayName = NormalizeLoose(displayName);

      switch (kind)
      {
        case PlayerNameMatchKind.Identifier:
          return identifier.Length > 0
            && string.Equals(identifier, normalized, StringComparison.Ordinal);

        case PlayerNameMatchKind.DisplayName:
          return displayName.Length > 0
            && string.Equals(displayName, normalized, StringComparison.OrdinalIgnoreCase);

        case PlayerNameMatchKind.DisplayNameLoose:
          return loose.Length > 0
            && looseDisplayName.Length > 0
            && string.Equals(looseDisplayName, loose, StringComparison.Ordinal);

        case PlayerNameMatchKind.IdentifierPrefix:
          return normalized.Length >= MinimumIdentifierPrefixLength
            && identifier.Length > 0
            && identifier.StartsWith(normalized, StringComparison.OrdinalIgnoreCase);

        case PlayerNameMatchKind.DisplayNamePrefix:
          return normalized.Length >= MinimumFuzzyLength
            && displayName.Length > 0
            && displayName.StartsWith(normalized, StringComparison.OrdinalIgnoreCase);

        case PlayerNameMatchKind.DisplayNameLoosePrefix:
          return loose.Length >= MinimumFuzzyLength
            && looseDisplayName.Length > 0
            && looseDisplayName.StartsWith(loose, StringComparison.Ordinal);

        case PlayerNameMatchKind.DisplayNameContains:
          return normalized.Length >= MinimumFuzzyLength
            && displayName.Length > 0
            && displayName.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0;

        case PlayerNameMatchKind.DisplayNameLooseContains:
          return loose.Length >= MinimumFuzzyLength
            && looseDisplayName.Length > 0
            && looseDisplayName.IndexOf(loose, StringComparison.Ordinal) >= 0;

        default:
          return false;
      }
    }
  }
}
