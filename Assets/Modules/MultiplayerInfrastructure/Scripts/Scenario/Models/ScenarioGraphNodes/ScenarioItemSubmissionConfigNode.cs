using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 아이템 제출 Interactable 을 사전 설정하는 시나리오 노드.
  ///
  /// 두 가지 방식으로 대상 Interactable 을 확보한다:
  ///  1) 프리셋 스폰: <see cref="PresetIdentifier"/> 로 등록된 EntityPreset(ItemSubmissionInteractable 프리팹)을 스폰한다.
  ///     스폰은 서버 권한이 필요하므로 서버/오프라인 컨텍스트에서만 수행된다.
  ///  2) 기존 참조: <see cref="TargetIdentifier"/> (또는 <see cref="TargetStateKey"/>)로 이미 배치/스폰된
  ///     ItemSubmissionInteractable 을 식별자로 찾는다(예: 의사 NPC 에 부착된 것).
  ///
  /// 확보한 Interactable 에 대해 다음을 오버라이드한다(프리셋 기본값 위에 노드 값이 우선):
  ///  - 요구 아이템 목록(<see cref="RequiredItems"/>): 비어 있지 않으면 덮어쓴다.
  ///  - 완료 시 올릴 서버 세션 전역 신호(<see cref="CompletionSignalIdentifier"/>).
  ///  - 활성/비활성(<see cref="Enabled"/>).
  ///
  /// 제출이 완료되면 ItemSubmissionInteractable 이 완료 신호를 서버 권한 경로로 올리며,
  /// 이를 Validator(RegistryContains, RuntimeState, sig.&lt;signal&gt;) 노드로 게이팅할 수 있다.
  /// </summary>
  public sealed class ScenarioItemSubmissionConfigNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.ItemSubmissionConfig;
    public string NextIdentifier { get; set; }

    /// <summary>스폰할 EntityPreset 식별자(ItemSubmissionInteractable 프리팹). 지정 시 프리셋 스폰 경로를 사용한다.</summary>
    public string PresetIdentifier { get; set; }

    /// <summary>스폰된 인스턴스에 부여할 엔티티 식별자. 비어 있으면 자동 생성된다.</summary>
    public string SpawnedEntityIdentifier { get; set; }

    /// <summary>스폰 위치의 기준이 되는 기존 엔티티 식별자(있으면 그 위치에 스폰).</summary>
    public string PositionSourceEntityIdentifier { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }

    /// <summary>프리셋을 스폰하지 않고 기존 Interactable 을 참조할 때의 식별자.</summary>
    public string TargetIdentifier { get; set; }

    /// <summary>대상 식별자를 상태 저장소 키에서 해석할 때 사용(예: 이전 스폰 노드의 결과).</summary>
    public string TargetStateKey { get; set; }

    /// <summary>요구 아이템 목록(식별자 + 수량). 비어 있으면 프리셋 기본값을 유지한다.</summary>
    public List<ScenarioItemRequirement> RequiredItems { get; set; } = new List<ScenarioItemRequirement>();

    /// <summary>제출 성공 시 올릴 서버 세션 전역 신호 식별자('sig.' 접두사는 자동 정규화).</summary>
    public string CompletionSignalIdentifier { get; set; }

    /// <summary>대상 Interactable 활성/비활성.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>확정된 대상 식별자를 기록할 상태 저장소 키(후속 노드 참조용).</summary>
    public string ResultStateKey { get; set; }
  }
}
