using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 서버가 확정해 TargetRpc로 전달한 병렬 브랜치 배정의 클라이언트측 읽기 모델.
  ///
  /// 그래프 실행 권위는 보유하지 않는다. 표현/퀘스트/상호작용 어댑터가 "이 클라이언트가
  /// 이 parallel node의 어느 branch를 맡았는가"를 읽는 유일한 복제 결과다.
  /// </summary>
  public static class ScenarioParallelAssignmentState
  {
    private static readonly Dictionary<string, IReadOnlyList<string>> BranchesByParallel = new();

    public static event Action<string, string, IReadOnlyList<string>> Changed;

    public static void Apply(string graphIdentifier, string parallelNodeIdentifier, IEnumerable<string> branchIdentifiers)
    {
      if (string.IsNullOrWhiteSpace(graphIdentifier) || string.IsNullOrWhiteSpace(parallelNodeIdentifier))
        return;

      var branches = (branchIdentifiers ?? Enumerable.Empty<string>())
        .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
        .Select(identifier => identifier.Trim())
        .Distinct(StringComparer.Ordinal)
        .ToArray();
      BranchesByParallel[CreateKey(graphIdentifier, parallelNodeIdentifier)] = branches;
      Changed?.Invoke(graphIdentifier, parallelNodeIdentifier, branches);
    }

    public static bool IsAssigned(string graphIdentifier, string parallelNodeIdentifier, string branchIdentifier)
    {
      return !string.IsNullOrWhiteSpace(branchIdentifier)
        && BranchesByParallel.TryGetValue(CreateKey(graphIdentifier, parallelNodeIdentifier), out var branches)
        && branches.Contains(branchIdentifier, StringComparer.Ordinal);
    }

    public static bool TryGetAssignedBranches(string graphIdentifier, string parallelNodeIdentifier, out IReadOnlyList<string> branches)
    {
      return BranchesByParallel.TryGetValue(CreateKey(graphIdentifier, parallelNodeIdentifier), out branches);
    }

    public static void ClearGraph(string graphIdentifier)
    {
      if (string.IsNullOrWhiteSpace(graphIdentifier))
        return;

      var prefix = graphIdentifier + "\n";
      foreach (var key in BranchesByParallel.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
        BranchesByParallel.Remove(key);
    }

    /// <summary>현재 클라이언트의 모든 복제 배정을 지운다. 시나리오 종료/테스트 정리에서 사용한다.</summary>
    public static void ClearAll()
    {
      BranchesByParallel.Clear();
      Changed = null;
    }

    private static string CreateKey(string graphIdentifier, string parallelNodeIdentifier)
      => (graphIdentifier ?? string.Empty) + "\n" + (parallelNodeIdentifier ?? string.Empty);
  }
}
