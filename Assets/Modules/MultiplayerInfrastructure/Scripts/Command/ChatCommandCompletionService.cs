using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// 채팅 입력창의 Tab 키 자동완성을 담당하는 서비스.
  /// <para>
  /// 핵심 설계: Tab Cycling 시 <see cref="_completionOriginalText"/>를 보존하여
  /// 자동완성된 텍스트가 다음 검색 쿼리로 사용되는 것을 방지합니다.
  /// </para>
  /// </summary>
  public class ChatCommandCompletionService
  {
    private static readonly string[] TargetSelectors = { "@s", "@a", "@p", "@r", "@e", "@n" };

    private readonly ChatCommandService _commandService;

    // ── Tab Cycling 상태 ─────────────────────────────────────────────
    private bool _hasActiveSession;
    private string _completionOriginalText;
    private List<string> _completionCandidates;
    private int _completionIndex;
    private int _completionTokenStart;
    private int _completionTokenEnd;
    private int _completionCursorPosition;

    public ChatCommandCompletionService(ChatCommandService commandService)
    {
      _commandService = commandService;
    }

    // ── Public API ─────────────────────────────────────────────────────

    /// <summary>
    /// Tab 키 입력 시 호출. 자동완성 결과를 반환합니다.
    /// </summary>
    /// <param name="currentText">현재 입력창의 전체 텍스트</param>
    /// <param name="cursorPos">현재 커서 위치</param>
    /// <returns>(완성된 텍스트, 새 커서 위치) 또는 null (후보 없음)</returns>
    public (string text, int cursorPos)? HandleTabPress(string currentText, int cursorPos)
    {
      if (string.IsNullOrEmpty(currentText))
      {
        ResetSession();
        return null;
      }

      cursorPos = Mathf.Clamp(cursorPos, 0, currentText.Length);

      // 1) 기존 세션이 있고, 현재 텍스트가 마지막 후보 적용 결과와 일치하면 → 다음 후보로 순환
      if (_hasActiveSession && IsContinuationOfCurrentSession(currentText, cursorPos))
      {
        _completionIndex = (_completionIndex + 1) % _completionCandidates.Count;
        return BuildResult(_completionCandidates[_completionIndex]);
      }

      // 2) 새 자동완성 세션 시작
      var candidates = CollectCandidates(currentText, cursorPos,
        out int tokenStart, out int tokenEnd);

      if (candidates == null || candidates.Count == 0)
      {
        ResetSession();
        return null;
      }

      _completionOriginalText = currentText;
      _completionCandidates = candidates;
      _completionIndex = 0;
      _completionTokenStart = tokenStart;
      _completionTokenEnd = tokenEnd;
      _hasActiveSession = true;

      var result = BuildResult(_completionCandidates[0]);
      _completionCursorPosition = result.cursorPos;
      return result;
    }

    /// <summary>
    /// Tab 외의 키가 입력되었을 때 호출하여 자동완성 세션을 리셋합니다.
    /// </summary>
    public void ResetSession()
    {
      _hasActiveSession = false;
      _completionOriginalText = null;
      _completionCandidates = null;
      _completionIndex = 0;
      _completionTokenStart = 0;
      _completionTokenEnd = 0;
      _completionCursorPosition = 0;
    }

    // ── 내부: 세션 연속성 확인 ──────────────────────────────────────────

    /// <summary>
    /// 현재 입력창 텍스트가 마지막 후보를 적용한 결과와 일치하는지 확인합니다.
    /// 일치하면 사용자가 다른 키를 누르지 않고 Tab만 연속으로 누른 것으로 판단합니다.
    /// </summary>
    private bool IsContinuationOfCurrentSession(string currentText, int cursorPos)
    {
      if (_completionCandidates == null || _completionCandidates.Count == 0)
        return false;

      string lastApplied = _completionCandidates[_completionIndex];
      int expectedCursorPosition = _completionCursorPosition;
      var expectedResult = BuildResult(lastApplied);
      return string.Equals(currentText, expectedResult.text, StringComparison.Ordinal)
        && cursorPos == expectedCursorPosition;
    }

    /// <summary>
    /// 원본 텍스트의 토큰 영역을 현재 후보로 치환한 결과를 생성합니다.
    /// 완료된 토큰 뒤에 공백을 추가하여 다음 인수를 입력할 수 있게 합니다.
    /// </summary>
    private (string text, int cursorPos) BuildResult(string candidate)
    {
      string before = _completionOriginalText.Substring(0, _completionTokenStart);
      string after = _completionOriginalText.Substring(_completionTokenEnd);
      // Do not add a second separator when the original token is followed by
      // whitespace (for example, completing "/gi foo" must not produce
      // "/give  foo"). A trailing space is still added at the end of the
      // input so the next argument can be typed immediately.
      bool needsSeparator = after.Length == 0 || !char.IsWhiteSpace(after[0]);
      string separator = needsSeparator ? " " : string.Empty;
      string newText = before + candidate + separator + after;
      int existingSeparatorLength = needsSeparator ? 0 : CountLeadingWhitespace(after);
      int newCursorPos = before.Length + candidate.Length
        + separator.Length + existingSeparatorLength;
      _completionCursorPosition = newCursorPos;
      return (newText, newCursorPos);
    }

    private static int CountLeadingWhitespace(string value)
    {
      int count = 0;
      while (count < value.Length && char.IsWhiteSpace(value[count]))
        count++;
      return count;
    }

    // ── 내부: 컨텍스트 파싱 및 후보 수집 ───────────────────────────────

    private List<string> CollectCandidates(string text, int cursorPos,
      out int tokenStart, out int tokenEnd)
    {
      cursorPos = Mathf.Clamp(cursorPos, 0, text.Length);

      // 1) 커서 위치에서 현재 토큰의 경계를 찾습니다
      tokenStart = cursorPos;
      while (tokenStart > 0 && !IsTokenSeparator(text[tokenStart - 1]))
        tokenStart--;

      tokenEnd = cursorPos;
      while (tokenEnd < text.Length && !IsTokenSeparator(text[tokenEnd]))
        tokenEnd++;

      string partial = text.Substring(tokenStart, tokenEnd - tokenStart);

      // 2) 명령어 모드 vs 일반 채팅 모드 분기
      if (!text.StartsWith("/", StringComparison.Ordinal))
      {
        // 일반 채팅: 플레이어 이름 자동완성
        return FilterCandidates(CollectPlayerNames(), partial);
      }

      // 3) 명령어 모드: 첫 토큰(명령어 이름) 경계를 확인
      int commandNameStart = 1; // '/' 바로 다음
      int firstSpace = FindTokenSeparator(text, commandNameStart);

      // 커서가 명령어 이름 위에 있는 경우
      if (firstSpace < 0 || tokenStart < firstSpace)
      {
        // 명령어 이름 자동완성
        // Command candidates include the leading slash because that slash is
        // part of the replacement token. Keep it in the filter as well.
        partial = "/" + text.Substring(commandNameStart, tokenEnd - commandNameStart);
        tokenStart = 0; // '/' 부터 치환하여 '/command' 형태로 완성
        tokenEnd = firstSpace >= 0 ? firstSpace : text.Length;
        return FilterCandidates(CollectCommandNames(), partial);
      }

      // 4) 인수 자동완성
      string commandName = text.Substring(commandNameStart, firstSpace - commandNameStart);

      // 현재 토큰 이전의 인수들을 추출
      string argsRegion = text.Substring(firstSpace + 1, tokenStart - firstSpace - 1);
      string[] previousArgs = argsRegion.Length > 0
        ? argsRegion.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        : Array.Empty<string>();

      int argIndex = previousArgs.Length;

      // 명령어가 IChatCommandCompletion 을 구현하면 위임
      if (_commandService != null
          && _commandService.TryGetCommand(commandName, out var command)
          && command is IChatCommandCompletion completionProvider)
      {
        try
        {
          var provided = completionProvider.GetCompletions(argIndex, previousArgs, partial);
          if (provided != null && provided.Count > 0)
          {
            var filteredProvided = FilterCandidates(provided, partial);
            if (filteredProvided != null && filteredProvided.Count > 0)
              return filteredProvided;
          }
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[ChatCommandCompletion] Error getting completions from /{commandName}: {ex.Message}");
        }
      }

      // Commands can expose their syntax through IChatCommandUsage without
      // having to duplicate every literal subcommand in a second API. This
      // also provides dynamic candidates for the common placeholders used by
      // the command definitions (item, target, waypoint, and so on).
      if (_commandService != null
          && _commandService.TryGetCommand(commandName, out var usageCommand))
      {
        var usageCandidates = CollectUsageCandidates(usageCommand, argIndex, previousArgs, partial);
        if (usageCandidates.Count > 0)
          return FilterCandidates(usageCandidates, partial);
      }

      // Fallback: @ 셀렉터 자동완성
      if (partial.StartsWith("@", StringComparison.Ordinal))
        return FilterCandidates(CollectTargetSelectors(), partial);

      return null;
    }

    private static bool IsTokenSeparator(char value)
    {
      return char.IsWhiteSpace(value);
    }

    private static int FindTokenSeparator(string text, int startIndex)
    {
      for (int i = startIndex; i < text.Length; i++)
      {
        if (IsTokenSeparator(text[i]))
          return i;
      }

      return -1;
    }

    private List<string> CollectUsageCandidates(
      IChatCommandModel command,
      int argIndex,
      string[] previousArgs,
      string partial)
    {
      var result = new List<string>();
      if (!(command is IChatCommandUsage usageProvider)
          || usageProvider.UsageLines == null)
        return result;

      foreach (var usageLine in usageProvider.UsageLines)
      {
        string[] tokens = TokenizeUsage(usageLine.Syntax);
        if (tokens.Length == 0)
          continue;

        // UsageLine entries are normally prefixed with the command name. A
        // continuation entry such as "<target>" has no command token and is
        // not useful for locating a subcommand path.
        if (!string.Equals(tokens[0], command.CommandEntry, StringComparison.OrdinalIgnoreCase))
          continue;

        int argumentCount = tokens.Length - 1;
        if (argIndex >= argumentCount)
          continue;

        bool pathMatches = true;
        for (int i = 0; i < argIndex; i++)
        {
          if (!MatchesUsageToken(tokens[i + 1], previousArgs[i]))
          {
            pathMatches = false;
            break;
          }
        }

        if (!pathMatches)
          continue;

        string currentToken = tokens[argIndex + 1];
        var currentCandidates = new List<string>();
        if (IsUsagePlaceholder(currentToken))
        {
          currentCandidates.AddRange(GetPlaceholderCandidates(currentToken));

          // Optional positional arguments such as `/give <item> [count]
          // [target]` may legally skip the count. Offer the following target
          // candidates at the count position when the user has started typing
          // a non-numeric token.
          if (currentCandidates.Count == 0
              && currentToken[0] == '['
              && !string.IsNullOrEmpty(partial)
              && !int.TryParse(partial, out _)
              && argIndex + 2 < tokens.Length
              && IsUsagePlaceholder(tokens[argIndex + 2]))
          {
            currentCandidates.AddRange(GetPlaceholderCandidates(tokens[argIndex + 2]));
          }
        }
        else if (!IsCoordinateLiteral(currentToken))
        {
          currentCandidates.Add(currentToken);
        }

        result.AddRange(currentCandidates);
      }

      return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsCoordinateLiteral(string token)
    {
      return string.Equals(token, "x", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "z", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] TokenizeUsage(string syntax)
    {
      if (string.IsNullOrWhiteSpace(syntax))
        return Array.Empty<string>();

      return syntax.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool MatchesUsageToken(string usageToken, string actualToken)
    {
      if (IsUsagePlaceholder(usageToken))
        return true;

      return string.Equals(usageToken, actualToken, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUsagePlaceholder(string token)
    {
      return token.Length >= 2
        && ((token[0] == '<' && token[token.Length - 1] == '>')
          || (token[0] == '[' && token[token.Length - 1] == ']'));
    }

    private List<string> GetPlaceholderCandidates(string token)
    {
      string inner = token.Substring(1, token.Length - 2);
      string[] alternatives = inner.Split('|');
      if (alternatives.Length > 1)
        return alternatives.ToList();

      string key = inner.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
      if (key.Contains("item"))
        return CollectItemIdentifiers();
      if (key.Contains("target") || key.Contains("player") || key.Contains("user") || key.Contains("name"))
        return CollectPlayersAndSelectors();
      if (key.Contains("waypoint"))
        return CollectWaypointIdentifiers();
      if (key.Contains("preset"))
        return CollectEntityPresetIdentifiers();
      if (key.Contains("entrypoint"))
        return CollectManualEntrypointIdentifiers();
      if (key.Contains("scenario"))
        return CollectScenarioIdentifiers();
      if (key.Contains("problem"))
        return CollectProblemSetIdentifiers();
      if (key.Contains("model") || key.Contains("character"))
        return CollectPlayerModelIdentifiers();
      if (key.Contains("command"))
        return CollectCommandNames()
          .Select(value => value.TrimStart('/'))
          .ToList();

      return new List<string>();
    }

    // ── 내부: 후보 소스 ──────────────────────────────────────────────

    private List<string> CollectCommandNames()
    {
      var result = new List<string>();
      if (_commandService == null) return result;

      foreach (var cmd in _commandService.GetCommands())
      {
        if (!string.IsNullOrEmpty(cmd.CommandEntry))
          result.Add("/" + cmd.CommandEntry);
      }

      return result
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    private List<string> CollectPlayerNames()
    {
      var result = new List<string>();

      try
      {
        foreach (var pair in UserDescriptorService.GetAll())
        {
          if (!string.IsNullOrWhiteSpace(pair.Value?.DisplayName))
            result.Add(pair.Value.DisplayName);
        }
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[ChatCommandCompletion] Error collecting player names: {ex.Message}");
      }

      // 타겟 셀렉터도 함께 제공
      result.AddRange(TargetSelectors);
      return result
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    private static List<string> CollectTargetSelectors()
    {
      return new List<string>(TargetSelectors);
    }

    // ── 내부: 후보 필터링/정렬 ─────────────────────────────────────────

    private static List<string> FilterCandidates(IReadOnlyList<string> candidates, string partial)
    {
      if (candidates == null || candidates.Count == 0)
        return null;

      List<string> filtered;

      if (string.IsNullOrEmpty(partial))
      {
        filtered = candidates.ToList();
      }
      else
      {
        filtered = new List<string>();
        for (int i = 0; i < candidates.Count; i++)
        {
          if (!string.IsNullOrEmpty(candidates[i])
              && candidates[i].StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            filtered.Add(candidates[i]);
        }
      }

      filtered = filtered
        .Where(value => !string.IsNullOrEmpty(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

      if (filtered.Count == 0)
        return null;

      filtered.Sort((a, b) =>
      {
        int lenCmp = a.Length.CompareTo(b.Length);
        return lenCmp != 0 ? lenCmp : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
      });

      return filtered;
    }

    // ── 정적 헬퍼: Registry 기반 후보 수집 (명령어 구현에서 사용) ─────

    /// <summary>등록된 모든 아이템 식별자를 반환합니다.</summary>
    public static List<string> CollectItemIdentifiers()
    {
      try
      {
        return Registry.Registry.GetAll<Type>(RegistryType.Item).Keys.ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    /// <summary>등록된 모든 엔티티 프리셋 식별자를 반환합니다.</summary>
    public static List<string> CollectEntityPresetIdentifiers()
    {
      try
      {
        return Registry.Registry.GetAllEntityPresets().Keys.ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    /// <summary>등록된 모든 웨이포인트 식별자를 반환합니다.</summary>
    public static List<string> CollectWaypointIdentifiers()
    {
      try
      {
        return Registry.Registry.GetAll<Vector3>(RegistryType.Waypoint).Keys.ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    /// <summary>등록된 모든 플레이어 모델 식별자를 반환합니다.</summary>
    public static List<string> CollectPlayerModelIdentifiers()
    {
      try
      {
        return Registry.Registry.GetAll<GameObject>(RegistryType.PlayerModel).Keys.ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    /// <summary>등록된 모든 시나리오 식별자를 반환합니다.</summary>
    /// <summary>현재 재생 중인 시나리오가 선언한 ManualEntrypoint 별칭 목록.</summary>
    public static List<string> CollectManualEntrypointIdentifiers()
    {
      try
      {
        var controller = Scenario.ScenarioController.Instance;
        if (controller == null)
          return new List<string>();

        return controller.GetManualEntrypointIdentifiers()
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
          .ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    public static List<string> CollectScenarioIdentifiers()
    {
      try
      {
        return Registry.Registry.GetAll<object>(RegistryType.ScenarioGraph).Keys.ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    /// <summary>등록된 모든 문제 세트 식별자를 반환합니다.</summary>
    public static List<string> CollectProblemSetIdentifiers()
    {
      try
      {
        return Registry.Registry.GetAll<object>(RegistryType.ProblemSet).Keys.ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    /// <summary>현재 접속 중인 플레이어 이름과 타겟 셀렉터를 함께 반환합니다.</summary>
    public static List<string> CollectPlayersAndSelectors()
    {
      var result = new List<string>();
      try
      {
        foreach (var pair in UserDescriptorService.GetAll())
        {
          if (!string.IsNullOrWhiteSpace(pair.Value?.DisplayName))
            result.Add(pair.Value.DisplayName);
        }
      }
      catch
      {
        // 무시
      }

      result.AddRange(TargetSelectors);
      return result
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }
  }
}
