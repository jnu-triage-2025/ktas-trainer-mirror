namespace MultiplayerInfrastructure.Registry
{
  public enum RegistryType
  {
    /// <summary>
    /// ItemSystem.Item 파생 클래스의 System.Type을 등록합니다. 나중에 인스턴스화할 수 있도록 클래스 자체를 값으로 가집니다.
    /// </summary>
    Item,
    ScenarioGraph,
    IconSprite,
    Npc,
    Waypoint,
    Entity,
    InteractableEntity,
    UI,
  }
}
