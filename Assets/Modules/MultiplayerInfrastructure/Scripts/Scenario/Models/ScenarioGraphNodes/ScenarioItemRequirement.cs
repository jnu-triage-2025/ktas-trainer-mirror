namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 그래프에서 요구 아이템을 (식별자 + 수량)으로 표현하는 도메인 모델.
  /// 런타임에는 <see cref="InteractableEntity.ItemRequirement"/> 로 변환되어 사용된다.
  /// </summary>
  public sealed class ScenarioItemRequirement
  {
    public string ItemIdentifier { get; set; }
    public int Count { get; set; } = 1;
  }
}
