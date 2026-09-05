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
    private readonly ChatCommandService _commandService;

    // ── Tab Cycling 상태 ─────────────────────────────────────────────
    private bool _hasActiveSession;
    private string _completionOriginalText;
    private List<string> _completionCandidates;
    private List<CompletionCandidate> _completionCandidateViews;
    private int _completionIndex;
    private int _completionTokenStart;
    private int _completionTokenEnd;
    private int _completionCursorPosition;

    public ChatCommandCompletionService(ChatCommandService commandService)
    {
      _commandService = commandService;
    }

    /// <summary>
    /// 후보 목록 오버레이에 한 행으로 표시되는 후보 하나의 정보.
    /// </summary>
    public readonly struct CompletionCandidate
    {
      /// <summary>입력창에 실제로 채워 넣는 문자열.</summary>
      public readonly string Text;

      /// <summary>후보 오른쪽에 함께 표시하는 설명. 없으면 빈 문자열.</summary>
      public readonly string Description;

      public CompletionCandidate(string text, string description)
      {
        Text = text ?? string.Empty;
        Description = description ?? string.Empty;
      }
    }

    // ── 진행 중인 세션 조회 (후보 목록 오버레이용) ─────────────────────

    /// <summary>Tab 순환 세션이 진행 중이면 true 입니다.</summary>
    public bool HasActiveSession => _hasActiveSession;

    /// <summary>현재 세션의 후보 목록. 세션이 없으면 null 입니다.</summary>
    public IReadOnlyList<CompletionCandidate> ActiveCandidates =>
      _hasActiveSession ? _completionCandidateViews : null;

    /// <summary>현재 선택되어 입력창에 채워져 있는 후보의 인덱스.</summary>
    public int ActiveCandidateIndex => _completionIndex;

    /// <summary>
    /// 후보로 치환되는 토큰이 입력 문자열에서 시작하는 위치. 후보 목록을 입력창의
    /// 해당 열 위에 정렬하는 데 사용합니다.
    /// </summary>
    public int ActiveTokenStart => _completionTokenStart;

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
      _completionCandidateViews = BuildCandidateViews(candidates);
      _completionIndex = 0;
      _completionTokenStart = tokenStart;
      _completionTokenEnd = tokenEnd;
      _hasActiveSession = true;

      var result = BuildResult(_completionCandidates[0]);
      _completionCursorPosition = result.cursorPos;
      return result;
    }

    /// <summary>
    /// 후보 목록이 열려 있는 동안 화살표 키로 선택을 한 칸 옮깁니다. 목록의 양 끝에서는
    /// 반대편으로 이어집니다. 옮겨진 후보를 입력창에 채운 결과를 반환하며, 진행 중인
    /// 세션이 없으면 null 입니다.
    /// </summary>
    /// <param name="delta">-1 이면 이전 후보, +1 이면 다음 후보.</param>
    public (string text, int cursorPos)? MoveSelection(int delta)
    {
      if (!_hasActiveSession || _completionCandidates == null || _completionCandidates.Count == 0)
        return null;

      int count = _completionCandidates.Count;
      _completionIndex = ((_completionIndex + delta) % count + count) % count;
      return BuildResult(_completionCandidates[_completionIndex]);
    }

    /// <summary>
    /// Tab 외의 키가 입력되었을 때 호출하여 자동완성 세션을 리셋합니다.
    /// </summary>
    public void ResetSession()
    {
      _hasActiveSession = false;
      _completionOriginalText = null;
      _completionCandidates = null;
      _completionCandidateViews = null;
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
      if (!string.Equals(currentText, expectedResult.text, StringComparison.Ordinal))
        return false;

      // 커서는 방금 채워 넣은 토큰 구간 안에 있으면 연속 입력으로 인정한다.
      // 텍스트 필드가 값 반영 직후에 캐럿을 구간의 다른 지점으로 옮겨 놓는
      // 경우가 있어, 정확한 한 지점만 허용하면 순환이 끊긴다. 사용자가 다른
      // 토큰으로 캐럿을 옮겼다면 구간을 벗어나므로 새 세션으로 처리된다.
      return cursorPos >= _completionTokenStart && cursorPos <= expectedCursorPosition;
    }

    /// <summary>
    /// 후보 문자열 목록에 화면 표시용 설명을 붙입니다. 커맨드 이름 후보에는
    /// 커맨드가 스스로 노출하는 한 줄 설명을 함께 보여 줍니다.
    /// </summary>
    private List<CompletionCandidate> BuildCandidateViews(List<string> candidates)
    {
      var views = new List<CompletionCandidate>(candidates.Count);
      for (int i = 0; i < candidates.Count; i++)
        views.Add(new CompletionCandidate(candidates[i], DescribeCandidate(candidates[i])));

      return views;
    }

    private string DescribeCandidate(string candidate)
    {
      if (string.IsNullOrEmpty(candidate))
        return string.Empty;

      try
      {
        if (candidate[0] == '/')
        {
          if (_commandService != null
              && _commandService.TryGetCommand(candidate.Substring(1), out var command))
            return ChatCommandHelp.GetSummary(command);

          return string.Empty;
        }

        // id:<uuid> 와 fish:<clientId> 후보에는 그 토큰이 가리키는 플레이어 이름을,
        // 대상 선택자에는 그 뜻을 함께 보여 준다. 표시 이름 후보는 그 자체로 읽히므로
        // 빈 문자열이 돌아온다.
        return PlayerTargetResolver.DescribeToken(candidate);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[ChatCommandCompletion] Error describing {candidate}: {ex.Message}");
      }

      return string.Empty;
    }

    /// <summary>
    /// 원본 텍스트의 토큰 영역을 현재 후보로 치환한 결과를 생성합니다.
    /// 완료된 토큰 뒤에 공백을 추가하여 다음 인수를 입력할 수 있게 합니다.
    /// </summary>
    private (string text, int cursorPos) BuildResult(string candidate)
    {
      string before = _completionOriginalText.Substring(0, _completionTokenStart);
      string after = _completionOriginalText.Substring(_completionTokenEnd);
      // 원본 토큰 뒤에 이미 공백이 있으면 구분자를 두 번 추가하지 않는다
      // (예: "/gi foo" 를 완성해 "/give  foo" 가 되어서는 안 된다). 대신 입력
      // 끝에는 항상 공백을 붙여 다음 인수를 바로 입력할 수 있게 한다.
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

      ExpandQuotedToken(text, cursorPos, ref tokenStart, ref tokenEnd);

      string partial = text.Substring(tokenStart, tokenEnd - tokenStart);

      // 2) 명령어 모드 vs 일반 채팅 모드 분기
      if (!text.StartsWith('/'))
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
        // 커맨드 후보에는 선행 슬래시가 포함된다. 그 슬래시가 치환 토큰의
        // 일부이기 때문이다. 필터에도 슬래시를 유지한다.
        partial = "/" + text.Substring(commandNameStart, tokenEnd - commandNameStart);
        tokenStart = 0; // '/' 부터 치환하여 '/command' 형태로 완성
        tokenEnd = firstSpace >= 0 ? firstSpace : text.Length;
        return FilterCandidates(CollectCommandNames(), partial);
      }

      // 4) 인수 자동완성
      string commandName = text.Substring(commandNameStart, firstSpace - commandNameStart);

      // 현재 토큰 이전의 인수들을 추출
      string argsRegion = text.Substring(firstSpace + 1, tokenStart - firstSpace - 1);
      string[] previousArgs = SplitArguments(argsRegion);

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

      // 커맨드는 IChatCommandUsage 로 자기 구문을 노출할 수 있어, 모든 리터럴
      // 하위 커맨드를 두 번째 API 에 중복 정의하지 않아도 된다. 또한 커맨드
      // 정의에서 쓰는 공통 플레이스홀더(item, target, waypoint 등)에 대한
      // 동적 후보도 여기서 제공한다.
      if (_commandService != null
          && _commandService.TryGetCommand(commandName, out var usageCommand))
      {
        var usageCandidates = CollectUsageCandidates(usageCommand, argIndex, previousArgs, partial);
        if (usageCandidates.Count > 0)
          return FilterCandidates(usageCandidates, partial);
      }

      // Fallback: @ 셀렉터 자동완성
      if (partial.StartsWith('@'))
        return FilterCandidates(CollectTargetSelectors(), partial);

      return null;
    }

    private static bool IsTokenSeparator(char value)
    {
      return char.IsWhiteSpace(value);
    }

    /// <summary>
    /// 커서가 따옴표로 감싼 낱말 안에 있으면 토큰 경계를 그 따옴표 바깥까지 넓힙니다.
    /// 공백이 들어 있는 표시 이름을 한 덩어리로 완성하기 위해 필요합니다.
    /// 닫는 따옴표 바깥에 커서가 있으면 다음 인수를 입력하려는 상황이므로 넓히지 않습니다.
    /// </summary>
    private static void ExpandQuotedToken(string text, int cursorPos, ref int tokenStart, ref int tokenEnd)
    {
      int openQuote = -1;
      for (int i = 0; i < text.Length; i++)
      {
        if (text[i] != '"')
          continue;

        if (openQuote < 0)
        {
          openQuote = i;
          continue;
        }

        if (cursorPos >= openQuote && cursorPos <= i)
        {
          tokenStart = openQuote;
          tokenEnd = i + 1;
          return;
        }

        openQuote = -1;
      }

      // 닫히지 않은 따옴표 뒤에 커서가 있으면 입력 끝까지를 한 토큰으로 본다.
      if (openQuote >= 0 && cursorPos >= openQuote)
      {
        tokenStart = openQuote;
        tokenEnd = text.Length;
      }
    }

    /// <summary>
    /// 인수 구간을 공백으로 나누되, 따옴표로 감싼 구간은 한 인수로 유지합니다.
    /// 표시 이름에 공백이 있어도 인수 위치가 밀리지 않게 합니다.
    /// </summary>
    private static string[] SplitArguments(string region)
    {
      if (string.IsNullOrWhiteSpace(region))
        return Array.Empty<string>();

      var arguments = new List<string>();
      bool inQuotes = false;
      int start = 0;
      for (int i = 0; i < region.Length; i++)
      {
        char current = region[i];
        if (current == '"')
          inQuotes = !inQuotes;

        if (inQuotes || !IsTokenSeparator(current))
          continue;

        if (i > start)
          arguments.Add(region.Substring(start, i - start));

        start = i + 1;
      }

      if (start < region.Length)
        arguments.Add(region.Substring(start));

      return arguments.ToArray();
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

        // UsageLine 항목은 보통 커맨드 이름으로 시작한다. "<target>" 같은
        // 연속 항목에는 커맨드 토큰이 없어 하위 커맨드 경로를 찾는 데 쓸 수 없다.
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

          // `/give <item> [count] [target]` 의 count 처럼 선택 위치 인수는
          // 건너뛰어도 된다. 사용자가 숫자가 아닌 토큰을 입력하기 시작했다면
          // count 위치에서 다음 target 후보를 제안한다.
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
      if (key.Contains("role"))
        return CollectRoleIdentifiers();
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
      if (_commandService == null)
        return result;

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

    /// <summary>일반 채팅 입력에서는 문장에 그대로 넣을 수 있는 표시 이름만 제안한다.</summary>
    private List<string> CollectPlayerNames()
      => PlayerNameQuery.CollectDisplayNameSuggestions().ToList();

    private static List<string> CollectTargetSelectors()
      => PlayerTargetResolver.CollectSelectorSuggestions().ToList();

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
          if (MatchesPartial(candidates[i], partial))
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

    /// <summary>
    /// 입력한 조각이 후보의 앞부분과 일치하는지 확인합니다. 공백이 있는 표시 이름은
    /// 따옴표로 감싸 제안하므로, 따옴표를 벗긴 형태로도 한 번 더 비교합니다.
    /// </summary>
    private static bool MatchesPartial(string candidate, string partial)
    {
      if (string.IsNullOrEmpty(candidate))
        return false;

      if (candidate.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
        return true;

      string bareCandidate = candidate.Trim('"');
      if (string.Equals(bareCandidate, candidate, StringComparison.Ordinal))
        return false;

      return bareCandidate.StartsWith(partial.Trim('"'), StringComparison.OrdinalIgnoreCase);
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

    /// <summary>권한 서비스에 등록된 모든 role 이름을 반환합니다.</summary>
    public static List<string> CollectRoleIdentifiers()
    {
      try
      {
        return Permission.PermissionService.GetRoles()
          .Where(value => !string.IsNullOrWhiteSpace(value))
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
          .ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    /// <summary>
    /// 커맨드 인수 자리에 넣을 수 있는 플레이어 지정 토큰을 모두 반환합니다.
    /// 표시 이름, id:&lt;uuid&gt;, fish:&lt;clientId&gt;, 대상 선택자가 함께 들어갑니다.
    /// </summary>
    public static List<string> CollectPlayersAndSelectors()
      => PlayerTargetResolver.CollectTokenSuggestions().ToList();
  }
}
