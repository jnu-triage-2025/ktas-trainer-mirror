# <a id="MultiplayerInfrastructure_Scenario_ScenarioInvokeEventNode"></a> Class ScenarioInvokeEventNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioInvokeEventNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioInvokeEventNode](MultiplayerInfrastructure.Scenario.ScenarioInvokeEventNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInvokeEventNode_EventIdentifier"></a> EventIdentifier

```csharp
public string EventIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInvokeEventNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInvokeEventNode_InvokeOnRoleClient"></a> InvokeOnRoleClient

역할 브랜치에서 true이면 서버 실행과 별도로 배정된 클라이언트에서도 표시용 핸들러를 실행한다.
로컬 UI처럼 클라이언트별 표현이 필요한 이벤트에만 사용한다.

```csharp
public bool InvokeOnRoleClient { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInvokeEventNode_MoveNextBehavior"></a> MoveNextBehavior

Controls when to move to NextIdentifier after invoking the event.
False: never moves automatically, Immediately: move right after firing, WaitUntilDone: wait for handler completion.

```csharp
public ScenarioInvokeEventMoveNextBehavior MoveNextBehavior { get; set; }
```

#### Property Value

 [ScenarioInvokeEventMoveNextBehavior](MultiplayerInfrastructure.Scenario.ScenarioInvokeEventMoveNextBehavior.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInvokeEventNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInvokeEventNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

