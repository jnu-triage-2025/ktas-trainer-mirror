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
        return null;

      // 1) 기존 세션이 있고, 현재 텍스트가 마지막 후보 적용 결과와 일치하면 → 다음 후보로 순환
      if (_hasActiveSession && IsContinuationOfCurrentSession(currentText))
      {
        _completionIndex = (_completionIndex + 1) % _completionCandidates.Count;
        return BuildResult(_completionCandidates[_completionIndex]);
      }

      // 2) 새 자동완성 세션 시작
      var candidates = CollectCandidates(currentText, cursorPos,
        out int tokenStart, out int tokenEnd);

      if (candidates == null || candidates.Count == 0)
        return null;

      _completionOriginalText = currentText;
      _completionCandidates = candidates;
      _completionIndex = 0;
      _completionTokenStart = tokenStart;
      _completionTokenEnd = tokenEnd;
      _hasActiveSession = true;

      return BuildResult(_completionCandidates[0]);
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
    }

    // ── 내부: 세션 연속성 확인 ──────────────────────────────────────────

    /// <summary>
    /// 현재 입력창 텍스트가 마지막 후보를 적용한 결과와 일치하는지 확인합니다.
    /// 일치하면 사용자가 다른 키를 누르지 않고 Tab만 연속으로 누른 것으로 판단합니다.
    /// </summary>
    private bool IsContinuationOfCurrentSession(string currentText)
    {
      if (_completionCandidates == null || _completionCandidates.Count == 0)
        return false;

      string lastApplied = _completionCandidates[_completionIndex];
      var (expectedText, _) = BuildResult(lastApplied);
      return string.Equals(currentText, expectedText, StringComparison.Ordinal);
    }

    /// <summary>
    /// 원본 텍스트의 토큰 영역을 현재 후보로 치환한 결과를 생성합니다.
    /// 완료된 토큰 뒤에 공백을 추가하여 다음 인수를 입력할 수 있게 합니다.
    /// </summary>
    private (string text, int cursorPos) BuildResult(string candidate)
    {
      string before = _completionOriginalText.Substring(0, _completionTokenStart);
      string after = _completionOriginalText.Substring(_completionTokenEnd);
      string newText = before + candidate + " " + after;
      int newCursorPos = before.Length + candidate.Length + 1; // +1 for trailing space
      return (newText, newCursorPos);
    }

    // ── 내부: 컨텍스트 파싱 및 후보 수집 ───────────────────────────────

    private List<string> CollectCandidates(string text, int cursorPos,
      out int tokenStart, out int tokenEnd)
    {
      // 1) 커서 위치에서 현재 토큰의 경계를 찾습니다
      tokenStart = cursorPos;
      while (tokenStart > 0 && text[tokenStart - 1] != ' ')
        tokenStart--;

      tokenEnd = cursorPos;
      while (tokenEnd < text.Length && text[tokenEnd] != ' ')
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
      int firstSpace = text.IndexOf(' ', commandNameStart);

      // 커서가 명령어 이름 위에 있는 경우
      if (firstSpace < 0 || tokenStart < firstSpace)
      {
        // 명령어 이름 자동완성
        partial = text.Substring(commandNameStart, tokenEnd - commandNameStart);
        tokenStart = 0; // '/' 부터 치환하여 '/command' 형태로 완성
        tokenEnd = firstSpace >= 0 ? firstSpace : text.Length;
        return FilterCandidates(CollectCommandNames(), partial);
      }

      // 4) 인수 자동완성
      string commandName = text.Substring(commandNameStart, firstSpace - commandNameStart);

      // 현재 토큰 이전의 인수들을 추출
      string argsRegion = text.Substring(firstSpace + 1, tokenStart - firstSpace - 1);
      string[] previousArgs = argsRegion.Length > 0
        ? argsRegion.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
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
            return FilterCandidates(provided, partial);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[ChatCommandCompletion] Error getting completions from /{commandName}: {ex.Message}");
        }
      }

      // Fallback: @ 셀렉터 자동완성
      if (partial.StartsWith("@", StringComparison.Ordinal))
        return FilterCandidates(CollectTargetSelectors(), partial);

      return null;
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

      result.Sort(StringComparer.OrdinalIgnoreCase);
      return result;
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
      result.Sort(StringComparer.OrdinalIgnoreCase);
      return result;
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
          if (candidates[i].StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            filtered.Add(candidates[i]);
        }
      }

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
      result.Sort(StringComparer.OrdinalIgnoreCase);
      return result;
    }
  }
}
