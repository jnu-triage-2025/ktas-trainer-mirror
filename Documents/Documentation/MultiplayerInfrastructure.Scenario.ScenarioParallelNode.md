# <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelNode"></a> Class ScenarioParallelNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioParallelNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioParallelNode](MultiplayerInfrastructure.Scenario.ScenarioParallelNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelNode_AllocationType"></a> AllocationType

병렬 브랜치를 플레이어에게 어떻게 할당할지 결정합니다.

```csharp
public ScenarioParallelAllocationType AllocationType { get; set; }
```

#### Property Value

 [ScenarioParallelAllocationType](MultiplayerInfrastructure.Scenario.ScenarioParallelAllocationType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelNode_Branches"></a> Branches

동시에 실행할 브랜치들

```csharp
public IReadOnlyList<ScenarioParallelBranch> Branches { get; set; }
```

#### Property Value

 IReadOnlyList<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelNode_NextIdentifier"></a> NextIdentifier

Parallel 브랜치가 모두 종료되었을 때의 대기 정책입니다.

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelNode_WaitMode"></a> WaitMode

브랜치 완료 감시 정책

```csharp
public ScenarioWaitMode WaitMode { get; set; }
```

#### Property Value

 [ScenarioWaitMode](MultiplayerInfrastructure.Scenario.ScenarioWaitMode.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioParallelNode_WhenBranchingPlayerNotMatched"></a> WhenBranchingPlayerNotMatched

플레이어 수와 브랜치 수가 일치하지 않을 때의 처리 방식입니다.

```csharp
public ScenarioParallelMismatchHandling WhenBranchingPlayerNotMatched { get; set; }
```

#### Property Value

 [ScenarioParallelMismatchHandling](MultiplayerInfrastructure.Scenario.ScenarioParallelMismatchHandling.md)

