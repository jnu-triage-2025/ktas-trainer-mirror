namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// Registry.Entity 저장소에 등록되는 런타임 엔티티 종류입니다.
  /// UI, 서비스, 전역 상태값은 포함하지 않습니다.
  /// </summary>
  public enum EntityType
  {
    Player,
    Npc,
    MovingPatientBed,
    Waypoint,
    ScenarioInteractable,
    ScenarioTriggerZone,
    ItemObject,
  }
}
