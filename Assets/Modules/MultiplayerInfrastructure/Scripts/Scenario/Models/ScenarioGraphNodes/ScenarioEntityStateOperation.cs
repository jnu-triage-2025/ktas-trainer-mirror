namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// <see cref="ScenarioEntityInitNode"/> 가 대상 엔티티에 적용하는 단일 초기 상태 항목.
  ///
  /// <para><see cref="Kind"/> 에 따라 의미가 달라진다.</para>
  /// <list type="bullet">
  /// <item><see cref="ScenarioEntityStateOperationKind.StateStore"/>:
  ///   <see cref="Key"/>/<see cref="Value"/> 를 시나리오 상태 저장소에 기록한다.</item>
  /// <item><see cref="ScenarioEntityStateOperationKind.DisplayState"/>:
  ///   <see cref="Key"/> 가 표시/부착 상태 이름(예: 환자의 <c>CervicalCollarOnNeck</c>)이고
  ///   <see cref="DisplayActive"/> 로 표시(true)/비표시(false)를 정한다.</item>
  /// </list>
  /// </summary>
  public sealed class ScenarioEntityStateOperation
  {
    public ScenarioEntityStateOperationKind Kind { get; set; } = ScenarioEntityStateOperationKind.DisplayState;

    /// <summary>
    /// StateStore: 상태 키. DisplayState: 표시/부착 상태 이름.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// StateStore: 기록할 값. DisplayState 에서는 사용하지 않는다(<see cref="DisplayActive"/> 사용).
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// DisplayState: 표시(true)/비표시(false). 기본값 true.
    /// </summary>
    public bool DisplayActive { get; set; } = true;
  }
}
