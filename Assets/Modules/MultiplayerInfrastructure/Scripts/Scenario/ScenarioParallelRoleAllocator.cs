using System;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 서버 권위 병렬 실행에서 사용하는 결정적 역할 배정기.
  ///
  /// 후보 목록은 호출자가 서버의 세션/태그 상태로 계산해 전달한다. 이 타입은 Unity·FishNet·Registry에
  /// 의존하지 않으므로, 서버와 테스트가 같은 배정 규칙을 사용한다.
  /// </summary>
  public static class ScenarioParallelRoleAllocator
  {
    /// <summary>
    /// 각 브랜치에 서로 다른 적격 플레이어를 하나씩 배정한다. 후보가 적은 브랜치부터 처리하고,
    /// 동률은 원래 브랜치 순서를 보존해 모든 피어에서 재현 가능한 결과를 만든다.
    /// </summary>
    public static bool TryAllocateDistinct(
      IReadOnlyList<ScenarioParallelBranch> branches,
      IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int>> candidatesByBranch,
      IDictionary<ScenarioParallelBranch, int?> allocation)
    {
      if (branches == null) throw new ArgumentNullException(nameof(branches));
      if (candidatesByBranch == null) throw new ArgumentNullException(nameof(candidatesByBranch));
      if (allocation == null) throw new ArgumentNullException(nameof(allocation));

      allocation.Clear();

      var ordered = new List<KeyValuePair<ScenarioParallelBranch, int>>(branches.Count);
      for (var index = 0; index < branches.Count; index++)
        ordered.Add(new KeyValuePair<ScenarioParallelBranch, int>(branches[index], index));
      ordered.Sort((left, right) =>
      {
        var candidateCountComparison = GetCandidates(candidatesByBranch, left.Key).Count
          .CompareTo(GetCandidates(candidatesByBranch, right.Key).Count);
        if (candidateCountComparison != 0)
          return candidateCountComparison;

        // List.Sort는 안정 정렬이 아니므로, 동률에서는 원래 배열상의 순서를
        // 보존하는 별도 인덱스를 사용한다.
        return left.Value.CompareTo(right.Value);
      });
      // client -> branch 역매핑을 유지한 증강 경로(augmenting path) 매칭이다.
      // 단순히 후보가 적은 브랜치부터 첫 빈 자리를 고르는 그리디 방식은
      // A=[1,2], B=[1,3], C=[1,3] 같은 경우 A=1, B=3으로 먼저 고정되어
      // C를 배정하지 못하지만, A=2, B=1, C=3이라는 완전 배정은 존재한다.
      // 이미 배정된 브랜치를 재배치할 수 있어야 서버 권위 배정이 가능한
      // 역할 조합을 거짓으로 실패 처리하지 않는다.
      var branchByClient = new Dictionary<int, ScenarioParallelBranch>();
      var complete = true;

      foreach (var entry in ordered)
      {
        var branch = entry.Key;
        if (!TryAssign(branch, candidatesByBranch, branchByClient, new HashSet<int>()))
        {
          allocation[branch] = null;
          complete = false;
        }
      }

      // 증강 중 기존 브랜치가 재배치될 수 있으므로 최종 역매핑에서 정방향 결과를 만든다.
      foreach (var branch in branches)
      {
        if (!allocation.ContainsKey(branch))
          allocation[branch] = null;
      }
      foreach (var pair in branchByClient)
        allocation[pair.Value] = pair.Key;

      return complete;
    }

    private static bool TryAssign(
      ScenarioParallelBranch branch,
      IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int>> candidatesByBranch,
      IDictionary<int, ScenarioParallelBranch> branchByClient,
      ISet<int> visitedClientIds)
    {
      foreach (var clientId in GetCandidates(candidatesByBranch, branch))
      {
        if (!visitedClientIds.Add(clientId))
          continue;

        if (!branchByClient.TryGetValue(clientId, out var occupiedBy)
            || TryAssign(occupiedBy, candidatesByBranch, branchByClient, visitedClientIds))
        {
          branchByClient[clientId] = branch;
          return true;
        }
      }

      return false;
    }

    private static IReadOnlyList<int> GetCandidates(
      IReadOnlyDictionary<ScenarioParallelBranch, IReadOnlyList<int>> candidatesByBranch,
      ScenarioParallelBranch branch)
      => branch != null && candidatesByBranch.TryGetValue(branch, out var candidates) && candidates != null
        ? candidates
        : Array.Empty<int>();
  }
}
