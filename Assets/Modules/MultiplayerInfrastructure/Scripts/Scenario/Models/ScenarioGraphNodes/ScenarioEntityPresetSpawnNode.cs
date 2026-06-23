using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioEntityPresetSpawnNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.EntityPresetSpawn;
    public string NextIdentifier { get; set; }

    public string PresetIdentifier { get; set; }

    /// <summary>
    /// 스폰된 인스턴스에 부여할 엔티티 식별자. 비어 있으면 GUID 기반 식별자가 자동 부여된다.
    /// 예) "patient_a" 로 지정하면 하나의 프리셋에서 환자별 인스턴스를 구분해 등록할 수 있다.
    /// </summary>
    public string SpawnedEntityIdentifier { get; set; }

    /// <summary>
    /// 컨테이너 프리팹에서 루트로 분리(ungroup)할 자식 NetworkObject 목록. 비어 있으면 분리 없이
    /// 단일 객체로 스폰된다(기존 동작). 지정된 자식만 분리되며, NetworkObject 가 아니면 무시된다.
    /// </summary>
    public List<ScenarioEntityChildDetachment> ChildDetachments { get; set; }

    public string PositionSourceEntityIdentifier { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }

    public string ResultStateKey { get; set; }
  }
}
