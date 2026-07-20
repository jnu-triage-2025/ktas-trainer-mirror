namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioSignalCounterOperation { Register, Unregister }

  /// <summary>
  /// 접두사(prefix)로 시작하는 서로 다른(distinct) 시나리오 신호가 몇 개나 올라왔는지 세어,
  /// 임계치에 도달하면 출력 신호를 발신하는 카운터를 제어하는 노드.
  ///
  /// <para>
  /// 시나리오 신호는 sticky(존재 여부만, 최초 1회만 <c>OnSignalRegistered</c> 발생)이므로,
  /// "같은 신호가 N번" 을 셀 수는 없다. 대신 <see cref="SourceSignalPrefix"/> 로 시작하는
  /// <b>서로 다른 신호 식별자</b>의 개수를 센다. 예:
  /// </para>
  /// <list type="bullet">
  /// <item>트리아지 구역 도착 3명: prefix <c>enter_triage_zone_</c> 로
  ///   <c>enter_triage_zone_patient_b</c> / <c>_patient_c</c> / <c>_dummy_b</c> 3개를 세어 threshold=3.</item>
  /// <item>환자 A 18G 2개: prefix <c>insert_iv_patient_a_</c> 로
  ///   <c>insert_iv_patient_a_left</c> / <c>_right</c> 2개를 세어 threshold=2.</item>
  /// </list>
  ///
  /// <para>
  /// 등록 시점에 이미 올라와 있는(정규화 후 prefix 매칭) 신호도 초기 카운트에 포함한다.
  /// 임계치 도달 시 <see cref="OutputSignalIdentifier"/> 를 <c>Raise</c> 하고, 카운터는 자동 해제된다(1회성).
  /// 시나리오 종료 시 모든 카운터가 정리된다. 동일 <see cref="CounterIdentifier"/> 재등록은 교체된다.
  /// </para>
  /// </summary>
  public sealed class ScenarioSignalCounterNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.SignalCounter;
    public string NextIdentifier { get; set; }

    /// <summary>카운터 식별자(등록/해제 매칭용, 필수). 동일 식별자 재등록은 교체된다.</summary>
    public string CounterIdentifier { get; set; }

    public ScenarioSignalCounterOperation Operation { get; set; } = ScenarioSignalCounterOperation.Register;

    /// <summary>
    /// 셀 대상 신호의 접두사. 정규화 후 이 접두사로 시작하는 서로 다른 신호 식별자 수를 센다.
    /// (예: "enter_triage_zone_" → sig.enter_triage_zone_* 를 카운트)
    /// </summary>
    public string SourceSignalPrefix { get; set; }

    /// <summary>임계치. 서로 다른 매칭 신호 수가 이 값 이상이면 출력 신호를 발신한다(1 이상).</summary>
    public int Threshold { get; set; } = 1;

    /// <summary>임계치 도달 시 발신할 신호 식별자.</summary>
    public string OutputSignalIdentifier { get; set; }
  }
}
