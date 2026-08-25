namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioNPCControlMode
  {
    Update,
    Control,
  }

  public enum ScenarioNPCInteractCrudOperation
  {
    None,
    Create,
    Read,
    Update,
    Delete,
  }

  /// <summary>
  /// NPC의 런타임 데이터/Interact를 갱신하거나 이동을 지시하는 통합 노드.
  /// </summary>
  public sealed class ScenarioNPCControlNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.NPCControl;
    public string NextIdentifier { get; set; }

    public ScenarioNPCControlMode Mode { get; set; } = ScenarioNPCControlMode.Update;
    public string NPCIdentifier { get; set; }

    // Update
    public ScenarioNPCInteractCrudOperation InteractOperation { get; set; }
      = ScenarioNPCInteractCrudOperation.None;
    public string InteractableIdentifier { get; set; }
    public bool? InteractEnabled { get; set; }
    public string ResultStateKey { get; set; }
    public string DisplayName { get; set; }
    public bool? ShowOverheadName { get; set; }

    // Control (Move)
    public ScenarioMoveDestinationType DestinationType { get; set; }
    public string DestinationIdentifier { get; set; }
    public float DestinationX { get; set; }
    public float DestinationY { get; set; }
    public float DestinationZ { get; set; }
    public bool IgnoreGroundCheck { get; set; }
    public ScenarioMoveMode MoveMode { get; set; } = ScenarioMoveMode.BySpeed;
    public float MoveSpeed { get; set; }
    public float MoveDuration { get; set; }
  }
}
