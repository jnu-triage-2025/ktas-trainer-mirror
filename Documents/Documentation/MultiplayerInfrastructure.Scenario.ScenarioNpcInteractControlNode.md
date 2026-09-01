# <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode"></a> Class ScenarioNpcInteractControlNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

NPC 에 부착된(혹은 참조로 연결할) Interactable 을 추가/제거하거나 활성/비활성 전환하는 시나리오 노드.

예: 의사 NPC 에게 "아이템 제출"(ItemSubmissionInteractable) 상호작용을 시나리오 진행 시점에 활성화하거나,
시나리오 종료 후 비활성화한다.

- <xref href="MultiplayerInfrastructure.Scenario.ScenarioNpcInteractControlNode.NpcIdentifier" data-throw-if-not-resolved="false"></xref>: 대상 NPC(Registry 의 Npc 식별자).
- <xref href="MultiplayerInfrastructure.Scenario.ScenarioNpcInteractControlNode.InteractableIdentifier" data-throw-if-not-resolved="false"></xref>: 대상 Interactable 의 식별자.
  Add 시 Registry(InteractableEntity)에서 해당 식별자의 IInteract 컴포넌트를 찾아 NPC 의 커스텀 소스로 추가한다.
  Enable/Disable 시 대상 Interactable 이 <xref href="MultiplayerInfrastructure.InteractableEntity.IInteractToggleable" data-throw-if-not-resolved="false"></xref> 을 구현하면 활성 상태를 전환한다.

```csharp
public sealed class ScenarioNpcInteractControlNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioNpcInteractControlNode](MultiplayerInfrastructure.Scenario.ScenarioNpcInteractControlNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode_DisplayName"></a> DisplayName

```csharp
public string DisplayName { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode_InteractableIdentifier"></a> InteractableIdentifier

```csharp
public string InteractableIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode_NpcIdentifier"></a> NpcIdentifier

```csharp
public string NpcIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode_Operation"></a> Operation

```csharp
public ScenarioNpcInteractControlOperation Operation { get; set; }
```

#### Property Value

 [ScenarioNpcInteractControlOperation](MultiplayerInfrastructure.Scenario.ScenarioNpcInteractControlOperation.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNpcInteractControlNode_ShowOverheadName"></a> ShowOverheadName

```csharp
public bool? ShowOverheadName { get; set; }
```

#### Property Value

 bool?

