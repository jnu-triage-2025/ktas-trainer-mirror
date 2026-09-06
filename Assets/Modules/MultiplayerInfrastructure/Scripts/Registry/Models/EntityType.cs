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
    Level1RapidInfuser,
    Waypoint,
    /// <summary>
    /// 제출 인터렉션을 위임받는 오브젝트. 같은 이름의 ScenarioInteractable 컴포넌트는 인터렉션 레지스트리
    /// 도입과 함께 폐기했지만, 레지스트리가 만드는 <c>ItemSubmissionInteractable</c> 이 이 종류로 등록되므로 남긴다.
    /// </summary>
    ScenarioInteractable,
    ScenarioTriggerZone,
    ItemObject,
    /// <summary>에디터에 사전 배치되는 정적 아이템(<c>StaticPlacedItem</c>). 물리 스폰 없이 맵의 일부처럼 취급된다.</summary>
    StaticPlacedItem,
    /// <summary>제세동 카트. 1인 조종 이동체(DefibrillatorCartController)로 동작한다.</summary>
    DefibrillatorCart,
    /// <summary>환자 모니터. 상호작용 주소와 태그 참조를 위해 등록한다.</summary>
    PatientMonitor,
    /// <summary>씬에 배치된 소품(튜토리얼 미끼 등). 인터렉션 주소를 갖기 위해 등록한다.</summary>
    Prop,
  }
}
