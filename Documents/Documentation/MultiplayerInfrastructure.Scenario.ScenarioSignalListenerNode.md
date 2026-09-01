# <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode"></a> Class ScenarioSignalListenerNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

실제 gameplay 신호를 조건부 시나리오 신호로 변환하는 리스너를 제어한다.

```csharp
public sealed class ScenarioSignalListenerNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioSignalListenerNode](MultiplayerInfrastructure.Scenario.ScenarioSignalListenerNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_ConsumeOnce"></a> ConsumeOnce

```csharp
public bool ConsumeOnce { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_ListenerIdentifier"></a> ListenerIdentifier

```csharp
public string ListenerIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_Operation"></a> Operation

```csharp
public ScenarioSignalListenerOperation Operation { get; set; }
```

#### Property Value

 [ScenarioSignalListenerOperation](MultiplayerInfrastructure.Scenario.ScenarioSignalListenerOperation.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_OutputSignalIdentifier"></a> OutputSignalIdentifier

```csharp
public string OutputSignalIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_RequiredSignalIdentifiers"></a> RequiredSignalIdentifiers

```csharp
public IReadOnlyList<string> RequiredSignalIdentifiers { get; set; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioSignalListenerNode_SourceSignalIdentifier"></a> SourceSignalIdentifier

```csharp
public string SourceSignalIdentifier { get; set; }
```

#### Property Value

 string

