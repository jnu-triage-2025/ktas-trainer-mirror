# <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode"></a> Class ScenarioTimeControlNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시간 표시(HUD)를 제어하는 시나리오 노드.

<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.Operation" data-throw-if-not-resolved="false"></xref> 에 따라 생성/흐름/표시/삭제를 각각 수행하며, 실행 즉시
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeRelay" data-throw-if-not-resolved="false"></xref> 를 통해 서버 권한으로 모든 클라이언트에 전파하고
곧바로 다음 노드로 진행한다(대기하지 않는다).

식별자(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.TimerId" data-throw-if-not-resolved="false"></xref>)로 여러 타이머를 동시에 보유할 수 있으나, 화면에 표시되는
타이머는 항상 최대 1개다(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeOperationType.Show" data-throw-if-not-resolved="false"></xref> 로 전환).
카운트다운이 0 에 도달해도 자동으로 숨겨지지 않는다(표시 전환은 Show/Hide/Remove 로만).

```csharp
public sealed class ScenarioTimeControlNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioTimeControlNode](MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode_Direction"></a> Direction

흐름 방향(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeOperationType.Create" data-throw-if-not-resolved="false"></xref> 에서만 사용).
정방향=스톱워치, 역방향=카운트다운.

```csharp
public ScenarioTimeDirection Direction { get; set; }
```

#### Property Value

 [ScenarioTimeDirection](MultiplayerInfrastructure.Scenario.ScenarioTimeDirection.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode_DurationSeconds"></a> DurationSeconds

Create 에서는 카운트다운 목표(총) 시간(초). Set 에서는 카운트다운 목표 재설정(0 이면 유지).
스톱워치에서는 무시된다.

```csharp
public float DurationSeconds { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode_Operation"></a> Operation

가할 연산. 기본값은 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeOperationType.Create" data-throw-if-not-resolved="false"></xref>.

```csharp
public ScenarioTimeOperationType Operation { get; set; }
```

#### Property Value

 [ScenarioTimeOperationType](MultiplayerInfrastructure.Scenario.ScenarioTimeOperationType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode_StartSeconds"></a> StartSeconds

Create 에서는 시작 시 표시값(스톱워치=경과, 카운트다운=남은값. 카운트다운 0 이면 목표로 대체).
Set 에서는 설정할 현재 표시값(절대값). 다른 연산에서는 무시된다.

```csharp
public float StartSeconds { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeControlNode_TimerId"></a> TimerId

대상 타이머 식별자. <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeOperationType.Hide" data-throw-if-not-resolved="false"></xref> 외 모든 연산에서 사용.

```csharp
public string TimerId { get; set; }
```

#### Property Value

 string

