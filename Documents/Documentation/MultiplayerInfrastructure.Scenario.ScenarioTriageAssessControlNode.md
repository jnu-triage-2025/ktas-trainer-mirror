# <a id="MultiplayerInfrastructure_Scenario_ScenarioTriageAssessControlNode"></a> Class ScenarioTriageAssessControlNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

특정 환자(엔티티)에 대해 트리아지(Triage) 평가 인터랙션을 활성화/비활성화하는 시나리오 노드.

<p>
시나리오 진행 중 플레이어가 특정 환자를 트리아지 분류할 수 있는 시점을 제어하는 데 사용한다.
대상 엔티티는 레지스트리에서 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTriageAssessControlNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref> 로 조회되며, 해당 엔티티가
트리아지 평가 제어를 지원하는 대상(<xref href="MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget" data-throw-if-not-resolved="false"></xref>)이어야 한다.
</p>

```csharp
public sealed class ScenarioTriageAssessControlNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioTriageAssessControlNode](MultiplayerInfrastructure.Scenario.ScenarioTriageAssessControlNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriageAssessControlNode_Assessable"></a> Assessable

true 면 트리아지 평가 인터랙션을 활성화, false 면 비활성화한다.

```csharp
public bool Assessable { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriageAssessControlNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriageAssessControlNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriageAssessControlNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTriageAssessControlNode_TargetEntityIdentifier"></a> TargetEntityIdentifier

트리아지 평가를 제어할 대상 환자(엔티티) 식별자.

```csharp
public string TargetEntityIdentifier { get; set; }
```

#### Property Value

 string

