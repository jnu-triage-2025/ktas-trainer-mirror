namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// Registry.Entity 저장소에 등록되는 런타임 엔티티 종류입니다.
  /// UI, 서비스, 전역 상태값은 포함하지 않습니다.
  /// </summary>
  public enum EntityType
  {
    /// <summary>미지정(기본값). 폴백 등록에서 종류가 명시되지 않았음을 의미한다.</summary>
    Undefined = 0,
    Player,
    Npc,
    Patient,
    MovingPatientBed,
    Waypoint,
    ScenarioInteractable,
    ScenarioTriggerZone,
    ItemObject,
  }
}
