# <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode"></a> Class ScenarioItemSubmissionConfigNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

아이템 제출 Interactable 을 사전 설정하는 시나리오 노드.

두 가지 방식으로 대상 Interactable 을 확보한다:
 1) 프리셋 스폰: <xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.PresetIdentifier" data-throw-if-not-resolved="false"></xref> 로 등록된 EntityPreset(ItemSubmissionInteractable 프리팹)을 스폰한다.
    스폰은 서버 권한이 필요하므로 서버/오프라인 컨텍스트에서만 수행된다.
 2) 기존 참조: <xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.TargetIdentifier" data-throw-if-not-resolved="false"></xref> (또는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.TargetStateKey" data-throw-if-not-resolved="false"></xref>)로 이미 배치/스폰된
    ItemSubmissionInteractable 을 식별자로 찾는다(예: 의사 NPC 에 부착된 것).

확보한 Interactable 에 대해 다음을 오버라이드한다(프리셋 기본값 위에 노드 값이 우선):
 - 요구 아이템 목록(<xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.RequiredItems" data-throw-if-not-resolved="false"></xref>): 비어 있지 않으면 덮어쓴다.
 - 완료 시 올릴 서버 세션 전역 신호(<xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.CompletionSignalIdentifier" data-throw-if-not-resolved="false"></xref>).
 - 활성/비활성(<xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.Enabled" data-throw-if-not-resolved="false"></xref>).

제출이 완료되면 ItemSubmissionInteractable 이 완료 신호를 서버 권한 경로로 올리며,
이를 Validator(RegistryContains, RuntimeState, sig.&lt;signal&gt;) 노드로 게이팅할 수 있다.

```csharp
public sealed class ScenarioItemSubmissionConfigNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioItemSubmissionConfigNode](MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_CompletionSignalIdentifier"></a> CompletionSignalIdentifier

제출 성공 시 올릴 서버 세션 전역 신호 식별자('sig.' 접두사는 자동 정규화).

```csharp
public string CompletionSignalIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_Enabled"></a> Enabled

대상 Interactable 활성/비활성.

```csharp
public bool Enabled { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_PositionSourceEntityIdentifier"></a> PositionSourceEntityIdentifier

스폰 위치의 기준이 되는 기존 엔티티 식별자(있으면 그 위치에 스폰).

```csharp
public string PositionSourceEntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_PositionX"></a> PositionX

```csharp
public float PositionX { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_PositionY"></a> PositionY

```csharp
public float PositionY { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_PositionZ"></a> PositionZ

```csharp
public float PositionZ { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_PresetIdentifier"></a> PresetIdentifier

스폰할 EntityPreset 식별자(ItemSubmissionInteractable 프리팹). 지정 시 프리셋 스폰 경로를 사용한다.

```csharp
public string PresetIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_RequiredItems"></a> RequiredItems

요구 아이템 목록(식별자 + 수량). 비어 있으면 프리셋 기본값을 유지한다.

```csharp
public List<ScenarioItemRequirement> RequiredItems { get; set; }
```

#### Property Value

 List<[ScenarioItemRequirement](MultiplayerInfrastructure.Scenario.ScenarioItemRequirement.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_ResultStateKey"></a> ResultStateKey

확정된 대상 식별자를 기록할 상태 저장소 키(후속 노드 참조용).

```csharp
public string ResultStateKey { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_SpawnedEntityIdentifier"></a> SpawnedEntityIdentifier

스폰된 인스턴스에 부여할 엔티티 식별자. 비어 있으면 자동 생성된다.

```csharp
public string SpawnedEntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_TargetIdentifier"></a> TargetIdentifier

프리셋을 스폰하지 않고 기존 Interactable 을 참조할 때의 식별자.

```csharp
public string TargetIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioItemSubmissionConfigNode_TargetStateKey"></a> TargetStateKey

대상 식별자를 상태 저장소 키에서 해석할 때 사용(예: 이전 스폰 노드의 결과).

```csharp
public string TargetStateKey { get; set; }
```

#### Property Value

 string

