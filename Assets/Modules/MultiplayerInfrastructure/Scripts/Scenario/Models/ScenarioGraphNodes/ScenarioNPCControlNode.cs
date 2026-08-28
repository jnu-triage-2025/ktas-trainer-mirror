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

    // Facing (Update / Control 공통)

    /// <summary>
    /// 지시를 마친 NPC가 바라볼 방향(월드 Y축 회전, 도 단위). null 이면 방향을 건드리지 않는다.
    ///
    /// <para>
    /// 이동 지시는 도착 지점만 정하고 방향은 정하지 않는다. 그래서 도착 후 방향은 스폰 당시의
    /// 회전값이 그대로 남아 연출마다 달라진다. 방향이 중요한 자리(대화 상대를 마주 보는 배치 등)는
    /// 이 값을 함께 지정해서 방향을 명시해야 한다.
    /// </para>
    ///
    /// <para>
    /// Control 모드에서는 이동이 끝난 뒤에, Update 모드에서는 이동 없이 즉시 적용한다.
    /// </para>
    /// </summary>
    public float? FacingYawDegrees { get; set; }
  }
}
