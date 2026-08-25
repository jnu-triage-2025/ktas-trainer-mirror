namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 등록된 엔티티 프리셋을 식별자로 스폰하는 시나리오 노드.
  ///
  /// 하위 오브젝트 구성(어떤 하위 EntityPreset 을 함께 스폰할지, unwrap 여부 등)은 프리셋 정의에 사전 설정되어 있으므로,
  /// 이 노드는 프리셋 식별자/위치/결과 식별자만 다룬다(노드에서 하위 구성을 재정의하지 않는다).
  /// </summary>
  public sealed class ScenarioEntityPresetSpawnNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.EntityPresetSpawn;
    public string NextIdentifier { get; set; }

    public string PresetIdentifier { get; set; }

    /// <summary>
    /// scenario 최상위 actingNpcs 정의를 참조해 같은 actingNpc 구성 경로로 스폰한다.
    /// 지정하면 preset/기본 위치/상호작용은 actingNpc 정의를 사용한다.
    /// <see cref="PositionSourceEntityIdentifier"/>가 있으면 해당 위치만 actingNpc 기본 위치보다 우선한다.
    /// </summary>
    public string ActingNpcIdentifier { get; set; }

    /// <summary>
    /// 스폰된 루트 인스턴스에 부여할 엔티티 식별자. 비어 있으면 GUID 기반 식별자가 자동 부여된다.
    /// 예) "patient_a" 로 지정하면 하나의 프리셋에서 환자별 인스턴스를 구분해 등록할 수 있다.
    /// </summary>
    public string SpawnedEntityIdentifier { get; set; }

    public string PositionSourceEntityIdentifier { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }

    /// <summary>스폰 루트에 적용할 월드 오일러 회전(도). Y축 음수는 위에서 볼 때 반시계 방향이다.</summary>
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }

    public string ResultStateKey { get; set; }
  }
}
