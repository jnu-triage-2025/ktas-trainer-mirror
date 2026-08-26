namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>체크리스트에 표시할 아이템 종류와 수량입니다.</summary>
  public sealed class ScenarioChecklistItemRequirement
  {
    public string Identifier { get; set; }
    public int Count { get; set; } = 1;
  }
}
