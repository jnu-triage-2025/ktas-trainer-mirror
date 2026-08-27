namespace MultiplayerInfrastructure.Registry
{
  public enum RegistryType
  {
    /// <summary>
    /// ItemSystem.Item 파생 클래스의 System.Type을 등록합니다. 나중에 인스턴스화할 수 있도록 클래스 자체를 값으로 가집니다.
    /// </summary>
    Item,
    ScenarioEvent,
    ScenarioGraph,
    IconSprite,
    Npc,
    Waypoint,
    SpawnPoint,
    /// <summary>
    /// 월드에 존재하는 엔티티 저장소. 값은 EntityDescriptor 이며, Get&lt;GameObject&gt; / Get&lt;Component&gt; 해석을 지원합니다.
    /// </summary>
    Entity,
    /// <summary>
    /// 씬/런타임 서비스 저장소. QuestManager, MainCameraController 등의 싱글턴성 컨트롤러를 등록합니다.
    /// </summary>
    Service,
    /// <summary>
    /// 씬 전환 의도, 접속 정보 등 전역 상태값을 등록합니다.
    /// </summary>
    RuntimeState,
    InteractableEntity,
    UI,
    PlayerModel,
    EntityPreset,
    ProblemSet,
    ProblemFigure,
    /// <summary>
    /// 플레이어 태그 레지스트리. 키는 UserDescriptor.Identifier(UUID), 값은 List&lt;string&gt;입니다.
    /// </summary>
    PlayerTag,
    /// <summary>
    /// 플레이어별 퀘스트 상태 플래그 풀. 키는 UserDescriptor.Identifier(UUID),
    /// 값은 HashSet&lt;string&gt;입니다. 역할 태그와 달리 퀘스트 진행 중에만 유지되는 임시 상태를 담습니다.
    /// </summary>
    PlayerQuestStateFlag,
  }
}
