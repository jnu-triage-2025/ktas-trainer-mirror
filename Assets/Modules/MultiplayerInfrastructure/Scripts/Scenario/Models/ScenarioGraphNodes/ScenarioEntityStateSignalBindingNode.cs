namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioEntityStateSignalBindingOperation { Register, Unregister }

  /// <summary>
  /// 엔티티의 명명된 상태(state) 이벤트를 시나리오 신호로 변환하는 바인딩을 제어하는 노드.
  ///
  /// <para>
  /// 대상 엔티티(<see cref="TargetEntityIdentifier"/> 또는 <see cref="TargetEntityStateKey"/>)가 구현한
  /// <see cref="MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource"/> 에 리스너를 등록한다.
  /// 엔티티가 <see cref="EventName"/> 이벤트를 발생시키고(선택적 <see cref="EventKey"/> 필터에 매칭되면)
  /// <see cref="OutputSignalIdentifier"/> 신호를 <c>ScenarioInteractionSignals.Raise</c> 로 발신한다.
  /// </para>
  ///
  /// <para>
  /// 예: 환자 A의 처치 표시 <c>EndotrachealTubeInsertDone</c> 가 적용되면 <c>et_tube_done_patient_a</c> 신호를 올린다.
  /// EventKey 를 비우면 해당 이벤트의 모든 발생에 매칭된다(예: 활력 변경 <c>VitalChanged</c>).
  /// </para>
  ///
  /// <para>바인딩은 시나리오 종료 시 정리되며, 동일 <see cref="BindingIdentifier"/> 재등록은 기존 바인딩을 교체한다.</para>
  /// </summary>
  public sealed class ScenarioEntityStateSignalBindingNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.EntityStateSignalBinding;
    public string NextIdentifier { get; set; }

    /// <summary>바인딩 식별자(등록/해제 매칭용, 필수). 동일 식별자 재등록은 교체된다.</summary>
    public string BindingIdentifier { get; set; }

    public ScenarioEntityStateSignalBindingOperation Operation { get; set; }
      = ScenarioEntityStateSignalBindingOperation.Register;

    /// <summary>대상 엔티티 식별자(직접). 비어 있으면 <see cref="TargetEntityStateKey"/> 를 사용한다.</summary>
    public string TargetEntityIdentifier { get; set; }

    /// <summary>대상 엔티티 식별자를 상태 저장소에서 조회할 키(간접).</summary>
    public string TargetEntityStateKey { get; set; }

    /// <summary>관찰할 상태 이벤트 이름(엔티티 구현이 정의; 예: TreatmentApplied/VitalChanged/TriageSubmitted).</summary>
    public string EventName { get; set; }

    /// <summary>이벤트 세부 대상 필터(예: 처치 표시 항목명, 트리아지 등급명). 비우면 모든 발생에 매칭.</summary>
    public string EventKey { get; set; }

    /// <summary>이벤트 발생(및 EventKey 매칭) 시 올릴 신호 식별자.</summary>
    public string OutputSignalIdentifier { get; set; }

    /// <summary>true 이면 한 번 발신 후 바인딩을 자동 해제한다. 기본 false(반복 발신 허용).</summary>
    public bool ConsumeOnce { get; set; } = false;
  }
}
