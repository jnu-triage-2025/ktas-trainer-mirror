# <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode"></a> Class ScenarioEntityStateSignalBindingNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

엔티티의 명명된 상태(state) 이벤트를 시나리오 신호로 변환하는 바인딩을 제어하는 노드.

<p>
대상 엔티티(<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref> 또는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.TargetEntityStateKey" data-throw-if-not-resolved="false"></xref>)가 구현한
<xref href="MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource" data-throw-if-not-resolved="false"></xref> 에 리스너를 등록한다.
엔티티가 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.EventName" data-throw-if-not-resolved="false"></xref> 이벤트를 발생시키고(선택적 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.EventKey" data-throw-if-not-resolved="false"></xref> 필터에 매칭되면)
<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.OutputSignalIdentifier" data-throw-if-not-resolved="false"></xref> 신호를 <code>ScenarioInteractionSignals.Raise</code> 로 발신한다.
</p>

<p>
예: 환자 A의 처치 표시 <code>EndotrachealTubeInsertDone</code> 가 적용되면 <code>et_tube_done_patient_a</code> 신호를 올린다.
EventKey 를 비우면 해당 이벤트의 모든 발생에 매칭된다(예: 활력 변경 <code>VitalChanged</code>).
</p>

<p>바인딩은 시나리오 종료 시 정리되며, 동일 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.BindingIdentifier" data-throw-if-not-resolved="false"></xref> 재등록은 기존 바인딩을 교체한다.</p>

```csharp
public sealed class ScenarioEntityStateSignalBindingNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioEntityStateSignalBindingNode](MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_BindingIdentifier"></a> BindingIdentifier

바인딩 식별자(등록/해제 매칭용, 필수). 동일 식별자 재등록은 교체된다.

```csharp
public string BindingIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_ConsumeOnce"></a> ConsumeOnce

true 이면 한 번 발신 후 바인딩을 자동 해제한다. 기본 false(반복 발신 허용).

```csharp
public bool ConsumeOnce { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_EventKey"></a> EventKey

이벤트 세부 대상 필터(예: 처치 표시 항목명, 트리아지 등급명). 비우면 모든 발생에 매칭.

```csharp
public string EventKey { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_EventName"></a> EventName

관찰할 상태 이벤트 이름(엔티티 구현이 정의; 예: TreatmentApplied/VitalChanged/TriageSubmitted).

```csharp
public string EventName { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_Operation"></a> Operation

```csharp
public ScenarioEntityStateSignalBindingOperation Operation { get; set; }
```

#### Property Value

 [ScenarioEntityStateSignalBindingOperation](MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingOperation.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_OutputSignalIdentifier"></a> OutputSignalIdentifier

이벤트 발생(및 EventKey 매칭) 시 올릴 신호 식별자.

```csharp
public string OutputSignalIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_TargetEntityIdentifier"></a> TargetEntityIdentifier

대상 엔티티 식별자(직접). 비어 있으면 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.TargetEntityStateKey" data-throw-if-not-resolved="false"></xref> 를 사용한다.

```csharp
public string TargetEntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateSignalBindingNode_TargetEntityStateKey"></a> TargetEntityStateKey

대상 엔티티 식별자를 상태 저장소에서 조회할 키(간접).

```csharp
public string TargetEntityStateKey { get; set; }
```

#### Property Value

 string

