# <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode"></a> Class ScenarioPlayerTagNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

플레이어 태그를 추가·제거·변경하는 노드.
Scope가 Current일 때는 시나리오 실행 중인 플레이어(_scenarioOwnerClientId)를 대상으로 합니다.
Scope가 All일 때는 현재 접속한 모든 플레이어에게 적용됩니다.

```csharp
public sealed class ScenarioPlayerTagNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioPlayerTagNode](MultiplayerInfrastructure.Scenario.ScenarioPlayerTagNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_FromTag"></a> FromTag

Change 시 교체 대상 원래 태그.

```csharp
public string FromTag { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_Operation"></a> Operation

태그 조작 타입.

```csharp
public ScenarioPlayerTagOperationType Operation { get; set; }
```

#### Property Value

 [ScenarioPlayerTagOperationType](MultiplayerInfrastructure.Scenario.ScenarioPlayerTagOperationType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_Scope"></a> Scope

대상 플레이어 범위.

```csharp
public ScenarioPlayerTagScope Scope { get; set; }
```

#### Property Value

 [ScenarioPlayerTagScope](MultiplayerInfrastructure.Scenario.ScenarioPlayerTagScope.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_SwapTagA"></a> SwapTagA

Swap 시 교환 대상 A 태그. SwapTagA 보유 플레이어가 SwapTagB 를 갖게 된다.

```csharp
public string SwapTagA { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_SwapTagB"></a> SwapTagB

Swap 시 교환 대상 B 태그. SwapTagB 보유 플레이어가 SwapTagA 를 갖게 된다.

```csharp
public string SwapTagB { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_Tag"></a> Tag

Add / Remove 시 사용할 태그 값.

```csharp
public string Tag { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagNode_ToTag"></a> ToTag

Change 시 새로 교체될 태그.

```csharp
public string ToTag { get; set; }
```

#### Property Value

 string

