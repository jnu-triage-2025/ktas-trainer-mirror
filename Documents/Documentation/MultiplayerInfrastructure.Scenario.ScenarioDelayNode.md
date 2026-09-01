# <a id="MultiplayerInfrastructure_Scenario_ScenarioDelayNode"></a> Class ScenarioDelayNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioDelayNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioDelayNode](MultiplayerInfrastructure.Scenario.ScenarioDelayNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDelayNode_Duration"></a> Duration

대기 시간. 원본 단위(Tick/Milliseconds/Seconds)를 보존한다.

```csharp
public ScenarioTimeValue Duration { get; set; }
```

#### Property Value

 [ScenarioTimeValue](MultiplayerInfrastructure.Scenario.ScenarioTimeValue.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDelayNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDelayNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDelayNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioDelayNode_WaitUntil"></a> WaitUntil

```csharp
public ScenarioDelayWaitUntil WaitUntil { get; set; }
```

#### Property Value

 [ScenarioDelayWaitUntil](MultiplayerInfrastructure.Scenario.ScenarioDelayWaitUntil.md)

