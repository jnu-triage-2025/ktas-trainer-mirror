namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioNpcInteractControlOperation
  {
    /// <summary>대상 Interactable(식별자로 참조) 을 NPC 의 상호작용 소스로 추가한다.</summary>
    Add,
    /// <summary>이전에 추가된 Interactable 소스를 NPC 에서 제거한다.</summary>
    Remove,
    /// <summary>NPC 에 부착/등록된 Interactable 을 활성화한다.</summary>
    Enable,
    /// <summary>NPC 에 부착/등록된 Interactable 을 비활성화한다.</summary>
    Disable,
  }

  /// <summary>
  /// NPC 에 부착된(혹은 참조로 연결할) Interactable 을 추가/제거하거나 활성/비활성 전환하는 시나리오 노드.
  ///
  /// 예: 의사 NPC 에게 "아이템 제출"(ItemSubmissionInteractable) 상호작용을 시나리오 진행 시점에 활성화하거나,
  /// 시나리오 종료 후 비활성화한다.
  ///
  /// - <see cref="NpcIdentifier"/>: 대상 NPC(Registry 의 Npc 식별자).
  /// - <see cref="InteractableIdentifier"/>: 대상 Interactable 의 식별자.
  ///   Add 시 Registry(InteractableEntity)에서 해당 식별자의 IInteract 컴포넌트를 찾아 NPC 의 커스텀 소스로 추가한다.
  ///   Enable/Disable 시 대상 Interactable 이 <see cref="InteractableEntity.IInteractToggleable"/> 을 구현하면 활성 상태를 전환한다.
  /// </summary>
  public sealed class ScenarioNpcInteractControlNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.NpcInteractControl;
    public string NextIdentifier { get; set; }

    public string NpcIdentifier { get; set; }
    public string InteractableIdentifier { get; set; }
    public ScenarioNpcInteractControlOperation Operation { get; set; } = ScenarioNpcInteractControlOperation.Add;
  }
}
