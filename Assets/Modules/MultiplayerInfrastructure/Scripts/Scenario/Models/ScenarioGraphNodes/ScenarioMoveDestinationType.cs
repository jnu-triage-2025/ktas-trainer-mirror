namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioMoveDestinationType
  {
    Position,   // 특정 좌표로 이동
    Waypoint,   // 미리 정의된 웨이포인트로 이동
    WaypointSet // waypoint set의 0번부터 순서대로 이동
  }
}
