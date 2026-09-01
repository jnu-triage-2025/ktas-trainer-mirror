# <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode"></a> Class ScenarioQuestMarkNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

퀘스트 마크(상호작용 아이콘 대체 / NPC 머리 위 아이콘)를 시나리오 그래프에서 명시적으로 켜고 끈다.
퀘스트 정의의 presentationBindings 와 동일한 표시 경로(<xref href="MultiplayerInfrastructure.Quest.QuestPresentationService" data-throw-if-not-resolved="false"></xref>)를 사용하므로
대상 종류·아이콘·우선순위 규칙이 퀘스트가 만든 마크와 같다. 표시 수명이 퀘스트 수명과 다를 때 사용한다.

```csharp
public sealed class ScenarioQuestMarkNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioQuestMarkNode](MultiplayerInfrastructure.Scenario.ScenarioQuestMarkNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_EntityIdentifier"></a> EntityIdentifier

대상 엔티티 식별자. NPC 대상은 Npc 레지스트리 식별자를 사용한다.

```csharp
public string EntityIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_IconIdentifier"></a> IconIdentifier

비우면 대상 종류별 기본 퀘스트 마크 아이콘을 사용한다.

```csharp
public string IconIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_InteractionIdentifier"></a> InteractionIdentifier

<xref href="MultiplayerInfrastructure.Scenario.ScenarioQuestMarkNode.TargetType" data-throw-if-not-resolved="false"></xref>이 Interaction일 때 엔티티 내부의 상호작용 행을 구분하는 식별자.

```csharp
public string InteractionIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_Operation"></a> Operation

```csharp
public ScenarioQuestMarkOperationType Operation { get; set; }
```

#### Property Value

 [ScenarioQuestMarkOperationType](MultiplayerInfrastructure.Scenario.ScenarioQuestMarkOperationType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_Priority"></a> Priority

같은 대상에 여러 마크가 겹칠 때의 우선순위. 값이 클수록 우선한다.

```csharp
public int Priority { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestMarkNode_TargetType"></a> TargetType

마크를 붙일 대상 종류. NPC는 머리 위 아이콘, Interaction은 상호작용 힌트의 기본 아이콘을 대체한다.

```csharp
public QuestPresentationTargetType TargetType { get; set; }
```

#### Property Value

 [QuestPresentationTargetType](MultiplayerInfrastructure.Quest.QuestPresentationTargetType.md)

