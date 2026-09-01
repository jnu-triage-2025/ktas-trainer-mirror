# <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode"></a> Class ScenarioNPCControlNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

NPC의 런타임 데이터/Interact를 갱신하거나 이동을 지시하는 통합 노드.

```csharp
public sealed class ScenarioNPCControlNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioNPCControlNode](MultiplayerInfrastructure.Scenario.ScenarioNPCControlNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_DestinationIdentifier"></a> DestinationIdentifier

```csharp
public string DestinationIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_DestinationType"></a> DestinationType

```csharp
public ScenarioMoveDestinationType DestinationType { get; set; }
```

#### Property Value

 [ScenarioMoveDestinationType](MultiplayerInfrastructure.Scenario.ScenarioMoveDestinationType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_DestinationX"></a> DestinationX

```csharp
public float DestinationX { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_DestinationY"></a> DestinationY

```csharp
public float DestinationY { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_DestinationZ"></a> DestinationZ

```csharp
public float DestinationZ { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_DisplayName"></a> DisplayName

```csharp
public string DisplayName { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_FacingYawDegrees"></a> FacingYawDegrees

지시를 마친 NPC가 바라볼 방향(월드 Y축 회전, 도 단위). null 이면 방향을 건드리지 않는다.

<p>
이동 지시는 도착 지점만 정하고 방향은 정하지 않는다. 그래서 도착 후 방향은 스폰 당시의
회전값이 그대로 남아 연출마다 달라진다. 방향이 중요한 자리(대화 상대를 마주 보는 배치 등)는
이 값을 함께 지정해서 방향을 명시해야 한다.
</p>

<p>
Control 모드에서는 이동이 끝난 뒤에, Update 모드에서는 이동 없이 즉시 적용한다.
</p>

```csharp
public float? FacingYawDegrees { get; set; }
```

#### Property Value

 float?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_IgnoreGroundCheck"></a> IgnoreGroundCheck

```csharp
public bool IgnoreGroundCheck { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_InteractEnabled"></a> InteractEnabled

```csharp
public bool? InteractEnabled { get; set; }
```

#### Property Value

 bool?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_InteractOperation"></a> InteractOperation

```csharp
public ScenarioNPCInteractCrudOperation InteractOperation { get; set; }
```

#### Property Value

 [ScenarioNPCInteractCrudOperation](MultiplayerInfrastructure.Scenario.ScenarioNPCInteractCrudOperation.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_InteractableIdentifier"></a> InteractableIdentifier

```csharp
public string InteractableIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_Mode"></a> Mode

```csharp
public ScenarioNPCControlMode Mode { get; set; }
```

#### Property Value

 [ScenarioNPCControlMode](MultiplayerInfrastructure.Scenario.ScenarioNPCControlMode.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_MoveDuration"></a> MoveDuration

```csharp
public float MoveDuration { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_MoveMode"></a> MoveMode

```csharp
public ScenarioMoveMode MoveMode { get; set; }
```

#### Property Value

 [ScenarioMoveMode](MultiplayerInfrastructure.Scenario.ScenarioMoveMode.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_MoveSpeed"></a> MoveSpeed

```csharp
public float MoveSpeed { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_NPCIdentifier"></a> NPCIdentifier

```csharp
public string NPCIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_ResultStateKey"></a> ResultStateKey

```csharp
public string ResultStateKey { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_ShowOverheadName"></a> ShowOverheadName

```csharp
public bool? ShowOverheadName { get; set; }
```

#### Property Value

 bool?

