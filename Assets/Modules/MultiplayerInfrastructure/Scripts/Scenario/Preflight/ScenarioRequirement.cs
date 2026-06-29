namespace MultiplayerInfrastructure.Scenario.Preflight
{
  /// <summary>
  /// 시나리오 그래프가 정상 동작하기 위해 환경(레지스트리/씬)에 준비되어 있어야 하는
  /// 요구 요소의 종류.
  /// </summary>
  public enum ScenarioRequirementKind
  {
    /// <summary>인터랙션 노드의 대상 엔티티(Registry.Entity).</summary>
    InteractionTarget,

    /// <summary>InvokeEvent / Interaction.CompletionCondition / CombineItem.Output 의 이벤트 핸들러.</summary>
    EventHandler,

    /// <summary>웨이포인트 하이라이트 노드의 웨이포인트 식별자.</summary>
    Waypoint,

    /// <summary>엔티티 프리셋 스폰 노드의 프리셋 식별자.</summary>
    EntityPreset,

    /// <summary>아이템 조합 노드의 입력/출력 아이템 식별자.</summary>
    Item,

    /// <summary>
    /// Validator(RegistryContains, RuntimeState, sig.*) 가 기다리는 런타임 신호.
    /// 정적으로 존재 여부를 판정할 수 없으므로 정보성으로만 보고한다.
    /// </summary>
    Signal
  }

  /// <summary>요구 요소의 검사 결과 상태.</summary>
  public enum ScenarioRequirementStatus
  {
    /// <summary>아직 검사하지 않음(수집 직후 기본값).</summary>
    Unknown,

    /// <summary>환경에 존재함(충족).</summary>
    Satisfied,

    /// <summary>환경에 존재하지 않음(누락). 경고 및 누락 집계 대상.</summary>
    Missing,

    /// <summary>
    /// 정적으로 판정할 수 없음(예: 런타임 신호). 정보성으로만 보고하며 누락 집계에서 제외한다.
    /// </summary>
    Indeterminate
  }

  /// <summary>
  /// 그래프에서 추출한 단일 요구 항목. "어떤 노드가, 어떤 종류의, 어떤 식별자를" 요구하는지를
  /// 표현한다. 검사 결과(<see cref="Status"/>)는 <see cref="ScenarioRequirementsChecker"/> 가 채운다.
  /// </summary>
  public sealed class ScenarioRequirement
  {
    public ScenarioRequirementKind Kind { get; }

    /// <summary>요구하는 대상 식별자(예: 인터랙션 타깃 id, 이벤트 id, 신호 id).</summary>
    public string Identifier { get; }

    /// <summary>이 요구를 만든 그래프 노드의 식별자(진단용).</summary>
    public string SourceNodeIdentifier { get; }

    public ScenarioRequirementStatus Status { get; set; } = ScenarioRequirementStatus.Unknown;

    public ScenarioRequirement(ScenarioRequirementKind kind, string identifier, string sourceNodeIdentifier)
    {
      Kind = kind;
      Identifier = identifier;
      SourceNodeIdentifier = sourceNodeIdentifier;
    }

    public override string ToString()
      => $"{Kind} '{Identifier}' (node '{SourceNodeIdentifier}') -> {Status}";
  }
}
