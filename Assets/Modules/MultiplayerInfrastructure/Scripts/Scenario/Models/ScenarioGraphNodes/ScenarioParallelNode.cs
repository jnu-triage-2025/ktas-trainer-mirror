using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioParallelNode : IScenarioNode
  {
    public string Identifier { get; set;}
    public ScenarioNodeType NodeType => ScenarioNodeType.Parallel;
    /// <summary>
    /// WaitMode 충족 시 다음 노드로 이동
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
  }
}
