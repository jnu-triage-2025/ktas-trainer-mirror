using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioParallelNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Parallel;
    /// <summary>
    /// Parallel 브랜치가 모두 종료되었을 때의 대기 정책입니다.
    /// </summary>
    public string NextIdentifier { get; set; }

    /// <summary>
    /// 동시에 실행할 브랜치들
    /// </summary>
    public IReadOnlyList<ScenarioParallelBranch> Branches { get; set; }

    /// <summary>
    /// 브랜치 완료 감시 정책
    /// </summary>
    public ScenarioWaitMode WaitMode { get; set; } = ScenarioWaitMode.All;

    /// <summary>
    /// 병렬 브랜치를 플레이어에게 어떻게 할당할지 결정합니다.
    /// </summary>
    public ScenarioParallelAllocationType AllocationType { get; set; } = ScenarioParallelAllocationType.SelfAll;

    /// <summary>
    /// 플레이어 수와 브랜치 수가 일치하지 않을 때의 처리 방식입니다.
    /// </summary>
    public ScenarioParallelMismatchHandling WhenBranchingPlayerNotMatched { get; set; } = ScenarioParallelMismatchHandling.Panic;
  }
}
