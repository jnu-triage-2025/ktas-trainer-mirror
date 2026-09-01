# <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode"></a> Class ScenarioEntityPresetSpawnNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

등록된 엔티티 프리셋을 식별자로 스폰하는 시나리오 노드.

하위 오브젝트 구성(어떤 하위 EntityPreset 을 함께 스폰할지, unwrap 여부 등)은 프리셋 정의에 사전 설정되어 있으므로,
이 노드는 프리셋 식별자/위치/결과 식별자만 다룬다(노드에서 하위 구성을 재정의하지 않는다).

```csharp
public sealed class ScenarioEntityPresetSpawnNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioEntityPresetSpawnNode](MultiplayerInfrastructure.Scenario.ScenarioEntityPresetSpawnNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_ActingNpcIdentifier"></a> ActingNpcIdentifier

scenario 최상위 actingNpcs 정의를 참조해 같은 actingNpc 구성 경로로 스폰한다.
지정하면 preset/기본 위치/상호작용은 actingNpc 정의를 사용한다.
<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityPresetSpawnNode.PositionSourceEntityIdentifier" data-throw-if-not-resolved="false"></xref>가 있으면 해당 위치만 actingNpc 기본 위치보다 우선한다.

```csharp
public string ActingNpcIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_PositionSourceEntityIdentifier"></a> PositionSourceEntityIdentifier

```csharp
public string PositionSourceEntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_PositionX"></a> PositionX

```csharp
public float PositionX { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_PositionY"></a> PositionY

```csharp
public float PositionY { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_PositionZ"></a> PositionZ

```csharp
public float PositionZ { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_PresetIdentifier"></a> PresetIdentifier

```csharp
public string PresetIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_ResultStateKey"></a> ResultStateKey

```csharp
public string ResultStateKey { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_RotationX"></a> RotationX

스폰 루트에 적용할 월드 오일러 회전(도). Y축 음수는 위에서 볼 때 반시계 방향이다.

```csharp
public float RotationX { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_RotationY"></a> RotationY

```csharp
public float RotationY { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_RotationZ"></a> RotationZ

```csharp
public float RotationZ { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityPresetSpawnNode_SpawnedEntityIdentifier"></a> SpawnedEntityIdentifier

스폰된 루트 인스턴스에 부여할 엔티티 식별자. 비어 있으면 GUID 기반 식별자가 자동 부여된다.
예) "patient_a" 로 지정하면 하나의 프리셋에서 환자별 인스턴스를 구분해 등록할 수 있다.

```csharp
public string SpawnedEntityIdentifier { get; set; }
```

#### Property Value

 string

