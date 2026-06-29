using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 그래프에 엔티티를 준비(생성 또는 참조)하고 초기 상태를 설정하는 노드.
  ///
  /// <para>대상 엔티티는 두 가지 방식으로 결정한다(<see cref="PresetIdentifier"/> 가 지정되면 스폰 우선).</para>
  /// <list type="number">
  /// <item><b>프리셋 스폰</b>: <see cref="PresetIdentifier"/> 로 등록된 엔티티 프리셋을 스폰한다.
  ///   스폰 위치는 <see cref="PositionSourceEntityIdentifier"/> 또는 좌표로 지정할 수 있다.</item>
  /// <item><b>기존 엔티티 참조</b>: <see cref="TargetEntityIdentifier"/>(직접) 또는
  ///   <see cref="TargetEntityStateKey"/>(상태 저장소 조회)로 이미 레지스트리에 등록된 엔티티를 가리킨다.</item>
  /// </list>
  ///
  /// <para>
  /// <see cref="EntityIdentifier"/> 를 지정하면 이후 시나리오 그래프가 그 식별자로 동일 엔티티를 계속 제어할 수 있다
  /// (프리셋 스폰 시에는 인스턴스에 부여할 식별자, 기존 엔티티 참조 시에는 결과 식별자로 사용).
  /// 결과 식별자는 <see cref="ResultStateKey"/> 가 지정되면 상태 저장소에도 기록되어 후속 노드가 참조할 수 있다.
  /// </para>
  ///
  /// <para>
  /// <see cref="StateOperations"/> 로 대상 엔티티의 초기 상태를 설정한다. 1차 목표는 환자 엔티티에 부착된
  /// 처치 부착물(주사기/거즈/경부보호대 등)의 초기 표시 여부 설정이다(DisplayState 종류).
  /// </para>
  /// </summary>
  public sealed class ScenarioEntityInitNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.EntityInit;
    public string NextIdentifier { get; set; }

    // ── 대상 결정: 프리셋 스폰 ──
    /// <summary>스폰할 엔티티 프리셋 식별자. 지정되면 기존 엔티티 참조보다 우선해 새 인스턴스를 스폰한다.</summary>
    public string PresetIdentifier { get; set; }

    public string PositionSourceEntityIdentifier { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }

    // ── 대상 결정: 기존 엔티티 참조 ──
    /// <summary>제어 대상 엔티티 식별자(직접). 프리셋 스폰이 아닐 때 사용.</summary>
    public string TargetEntityIdentifier { get; set; }

    /// <summary>제어 대상 엔티티 식별자를 상태 저장소에서 조회할 키(간접). 프리셋 스폰이 아닐 때 사용.</summary>
    public string TargetEntityStateKey { get; set; }

    // ── 식별자 제어 ──
    /// <summary>
    /// 이후 그래프가 이 엔티티를 계속 제어하기 위해 부여/사용할 식별자.
    /// 프리셋 스폰 시 인스턴스에 부여할 식별자로 사용된다. 비어 있으면 자동(GUID) 부여된다.
    /// </summary>
    public string EntityIdentifier { get; set; }

    /// <summary>확정된 대상 엔티티 식별자를 기록할 상태 저장소 키(선택). 후속 노드가 참조할 수 있다.</summary>
    public string ResultStateKey { get; set; }

    // ── 초기 상태 ──
    /// <summary>대상 엔티티에 적용할 초기 상태 항목 목록(표시/부착 상태, 상태 저장소 기록 등).</summary>
    public List<ScenarioEntityStateOperation> StateOperations { get; set; } = new List<ScenarioEntityStateOperation>();
  }
}
