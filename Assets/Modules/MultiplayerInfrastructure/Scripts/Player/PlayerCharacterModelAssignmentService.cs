using System.Collections.Generic;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// 서버 세션 동안 접속한 플레이어에게 기본 캐릭터 모델을 무작위로 배정하는 정적 서비스.
  ///
  /// 서버에서만 사용합니다.
  /// - 배정  : PlayerController.OnStartServer 에서 클라이언트당 1회 수행됩니다.
  /// - 해제  : PlayerController.OnStopServer 에서 수행되어 배정된 모델이 풀로 돌아갑니다.
  /// - 초기화: 서버 세션 시작/종료 시 <c>FishNetSupport.ServerManager_OnServerConnectionState</c> 에서
  ///           <see cref="ClearAll"/> 가 호출됩니다. static 상태는 도메인 리로드 없이는 유지되므로
  ///           명시적 초기화가 필요합니다.
  ///
  /// 배정 규칙
  /// - 현재 세션에서 아무도 사용하지 않는 후보가 있으면 그중에서 무작위로 고릅니다.
  ///   따라서 동시 접속자 수가 후보 수 이하인 동안에는 캐릭터가 겹치지 않습니다.
  /// - 후보가 모두 사용 중이면 전체 후보 중에서 무작위로 고릅니다(중복 허용).
  /// </summary>
  public static class PlayerCharacterModelAssignmentService
  {
    /// <summary>배정 후보의 기본값. 등록된 PlayerModel 식별자와 일치해야 합니다.</summary>
    public static readonly IReadOnlyList<string> BuiltInCandidateIdentifiers = new[]
    {
      "emma",
      "ethan",
      "liam",
      "lisa",
      "maya",
      "olivia",
      "sofia",
    };

    private static readonly List<string> _candidateIdentifiers = new(BuiltInCandidateIdentifiers);

    // ClientId → 현재 이 클라이언트가 점유 중인 모델 식별자
    private static readonly Dictionary<int, string> _identifierByClientId = new();

    private static readonly System.Random _random = new();

    public static IReadOnlyList<string> CandidateIdentifiers => _candidateIdentifiers;

    /// <summary>
    /// 배정 후보 목록을 교체합니다. 비어 있는 목록이 전달되면 기본 후보로 되돌립니다.
    /// 이미 배정된 플레이어의 모델은 바꾸지 않습니다.
    /// </summary>
    public static void ConfigureCandidates(IEnumerable<string> identifiers)
    {
      _candidateIdentifiers.Clear();

      if (identifiers != null)
      {
        foreach (string identifier in identifiers)
        {
          if (string.IsNullOrWhiteSpace(identifier))
            continue;

          string trimmed = identifier.Trim();
          if (!_candidateIdentifiers.Contains(trimmed))
            _candidateIdentifiers.Add(trimmed);
        }
      }

      if (_candidateIdentifiers.Count == 0)
        _candidateIdentifiers.AddRange(BuiltInCandidateIdentifiers);
    }

    /// <summary>서버 세션 시작/종료 시 호출되어 이전 세션의 배정 상태를 제거합니다.</summary>
    public static void ClearAll()
    {
      _identifierByClientId.Clear();
    }

    /// <summary>
    /// 해당 클라이언트에 모델을 배정하고 그 식별자를 반환합니다.
    /// 이미 배정된 클라이언트라면 기존 배정을 그대로 반환합니다.
    /// 후보가 하나도 없으면 빈 문자열을 반환합니다.
    /// </summary>
    public static string Assign(int clientId)
    {
      if (_identifierByClientId.TryGetValue(clientId, out string assigned) &&
          !string.IsNullOrWhiteSpace(assigned))
        return assigned;

      if (_candidateIdentifiers.Count == 0)
        return string.Empty;

      string picked = PickIdentifier();
      _identifierByClientId[clientId] = picked;
      return picked;
    }

    /// <summary>해당 클라이언트의 배정을 해제하여 모델을 다시 배정 가능하게 만듭니다.</summary>
    public static void Release(int clientId)
    {
      _identifierByClientId.Remove(clientId);
    }

    /// <summary>현재 배정된 모델을 조회합니다.</summary>
    public static bool TryGetAssignedIdentifier(int clientId, out string modelIdentifier)
      => _identifierByClientId.TryGetValue(clientId, out modelIdentifier);

    /// <summary>
    /// 서버가 다른 경로(명령어 등)로 모델을 바꾼 경우 점유 상태를 최신으로 유지합니다.
    /// 후보 목록에 없는 식별자도 그대로 기록하며, 이 경우 해당 클라이언트가 점유하던 후보는 풀로 돌아갑니다.
    /// </summary>
    public static void NotifyIdentifierApplied(int clientId, string modelIdentifier)
    {
      if (string.IsNullOrWhiteSpace(modelIdentifier))
        return;

      _identifierByClientId[clientId] = modelIdentifier;
    }

    private static string PickIdentifier()
    {
      var available = new List<string>(_candidateIdentifiers.Count);
      for (int i = 0; i < _candidateIdentifiers.Count; i++)
      {
        if (!IsIdentifierInUse(_candidateIdentifiers[i]))
          available.Add(_candidateIdentifiers[i]);
      }

      // 동시 접속자가 후보보다 많으면 더 이상 고유 배정이 불가능하므로 전체 후보에서 고른다.
      var source = available.Count > 0 ? available : _candidateIdentifiers;
      return source[_random.Next(source.Count)];
    }

    private static bool IsIdentifierInUse(string identifier)
    {
      foreach (var pair in _identifierByClientId)
      {
        if (string.Equals(pair.Value, identifier, System.StringComparison.Ordinal))
          return true;
      }

      return false;
    }
  }
}
