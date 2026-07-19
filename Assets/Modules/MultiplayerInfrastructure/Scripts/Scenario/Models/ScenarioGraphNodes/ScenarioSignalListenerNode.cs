using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioSignalListenerOperation { Register, Unregister }

  /// <summary>실제 gameplay 신호를 조건부 시나리오 신호로 변환하는 리스너를 제어한다.</summary>
  public sealed class ScenarioSignalListenerNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.SignalListener;
    public string NextIdentifier { get; set; }
    public string ListenerIdentifier { get; set; }
    public ScenarioSignalListenerOperation Operation { get; set; } = ScenarioSignalListenerOperation.Register;
    public string SourceSignalIdentifier { get; set; }
    public string OutputSignalIdentifier { get; set; }
    public IReadOnlyList<string> RequiredSignalIdentifiers { get; set; } = new List<string>();
    public bool ConsumeOnce { get; set; } = true;
  }
}
