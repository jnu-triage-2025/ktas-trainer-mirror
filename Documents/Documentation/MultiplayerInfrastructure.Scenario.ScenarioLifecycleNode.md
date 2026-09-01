# <a id="MultiplayerInfrastructure_Scenario_ScenarioLifecycleNode"></a> Class ScenarioLifecycleNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 실행 수명주기를 그래프 안에서 명시적으로 제어한다.
Cleanup은 다음 노드로 진행하고, End와 Restart는 현재 흐름을 종료한다.

```csharp
public sealed class ScenarioLifecycleNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioLifecycleNode](MultiplayerInfrastructure.Scenario.ScenarioLifecycleNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioLifecycleNode_ClearRuntimeState"></a> ClearRuntimeState

```csharp
public bool ClearRuntimeState { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioLifecycleNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioLifecycleNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioLifecycleNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioLifecycleNode_Operation"></a> Operation

```csharp
public ScenarioLifecycleOperation Operation { get; set; }
```

#### Property Value

 [ScenarioLifecycleOperation](MultiplayerInfrastructure.Scenario.ScenarioLifecycleOperation.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioLifecycleNode_RestartEntrypointIdentifier"></a> RestartEntrypointIdentifier

```csharp
public string RestartEntrypointIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioLifecycleNode_RevertTrackedChanges"></a> RevertTrackedChanges

```csharp
public bool RevertTrackedChanges { get; set; }
```

#### Property Value

 bool

