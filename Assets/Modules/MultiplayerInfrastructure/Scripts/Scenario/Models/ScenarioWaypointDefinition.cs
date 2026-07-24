namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 수명주기 동안 사용할 waypoint anchor 정의.
  /// 시작 전에 등록되어 이동, 퀘스트, 하이라이트 노드에서 같은 identifier로 참조할 수 있다.
  /// </summary>
  public sealed class ScenarioWaypointDefinition
  {
    public string Identifier { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public bool DespawnOnScenarioEnd { get; set; } = true;
  }
}
