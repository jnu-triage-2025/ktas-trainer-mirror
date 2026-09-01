# <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode"></a> Class ScenarioEntityInitNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 그래프에 엔티티를 준비(생성 또는 참조)하고 초기 상태를 설정하는 노드.

<p>대상 엔티티는 두 가지 방식으로 결정한다(<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.PresetIdentifier" data-throw-if-not-resolved="false"></xref> 가 지정되면 스폰 우선).</p>
<ol><li><b>프리셋 스폰</b>: <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.PresetIdentifier" data-throw-if-not-resolved="false"></xref> 로 등록된 엔티티 프리셋을 스폰한다.
  스폰 위치는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.PositionSourceEntityIdentifier" data-throw-if-not-resolved="false"></xref> 또는 좌표로 지정할 수 있다.</li><li><b>기존 엔티티 참조</b>: <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref>(직접) 또는
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.TargetEntityStateKey" data-throw-if-not-resolved="false"></xref>(상태 저장소 조회)로 이미 레지스트리에 등록된 엔티티를 가리킨다.</li></ol>

<p>
<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.EntityIdentifier" data-throw-if-not-resolved="false"></xref> 를 지정하면 이후 시나리오 그래프가 그 식별자로 동일 엔티티를 계속 제어할 수 있다
(프리셋 스폰 시에는 인스턴스에 부여할 식별자, 기존 엔티티 참조 시에는 결과 식별자로 사용).
결과 식별자는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.ResultStateKey" data-throw-if-not-resolved="false"></xref> 가 지정되면 상태 저장소에도 기록되어 후속 노드가 참조할 수 있다.
</p>

<p>
<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.StateOperations" data-throw-if-not-resolved="false"></xref> 로 대상 엔티티의 초기 상태를 설정한다. 1차 목표는 환자 엔티티에 부착된
처치 부착물(주사기/거즈/경부보호대 등)의 초기 표시 여부 설정이다(DisplayState 종류).
</p>

```csharp
public sealed class ScenarioEntityInitNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioEntityInitNode](MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_EntityIdentifier"></a> EntityIdentifier

이후 그래프가 이 엔티티를 계속 제어하기 위해 부여/사용할 식별자.
프리셋 스폰 시 인스턴스에 부여할 식별자로 사용된다. 비어 있으면 자동(GUID) 부여된다.

```csharp
public string EntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_PositionSourceEntityIdentifier"></a> PositionSourceEntityIdentifier

```csharp
public string PositionSourceEntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_PositionX"></a> PositionX

```csharp
public float PositionX { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_PositionY"></a> PositionY

```csharp
public float PositionY { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_PositionZ"></a> PositionZ

```csharp
public float PositionZ { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_PresetIdentifier"></a> PresetIdentifier

스폰할 엔티티 프리셋 식별자. 지정되면 기존 엔티티 참조보다 우선해 새 인스턴스를 스폰한다.

```csharp
public string PresetIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_ResultStateKey"></a> ResultStateKey

확정된 대상 엔티티 식별자를 기록할 상태 저장소 키(선택). 후속 노드가 참조할 수 있다.

```csharp
public string ResultStateKey { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_StateOperations"></a> StateOperations

대상 엔티티에 적용할 초기 상태 항목 목록(표시/부착 상태, 상태 저장소 기록 등).

```csharp
public List<ScenarioEntityStateOperation> StateOperations { get; set; }
```

#### Property Value

 List<[ScenarioEntityStateOperation](MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_TargetEntityIdentifier"></a> TargetEntityIdentifier

제어 대상 엔티티 식별자(직접). 프리셋 스폰이 아닐 때 사용.

```csharp
public string TargetEntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityInitNode_TargetEntityStateKey"></a> TargetEntityStateKey

제어 대상 엔티티 식별자를 상태 저장소에서 조회할 키(간접). 프리셋 스폰이 아닐 때 사용.

```csharp
public string TargetEntityStateKey { get; set; }
```

#### Property Value

 string

