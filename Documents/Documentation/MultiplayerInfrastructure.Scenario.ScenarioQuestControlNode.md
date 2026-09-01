# <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode"></a> Class ScenarioQuestControlNode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class ScenarioQuestControlNode : IScenarioNode
```

#### Inheritance

object ← 
[ScenarioQuestControlNode](MultiplayerInfrastructure.Scenario.ScenarioQuestControlNode.md)

#### Implements

[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_FailureStrategy"></a> FailureStrategy

```csharp
public ScenarioQuestFailureStrategy FailureStrategy { get; set; }
```

#### Property Value

 [ScenarioQuestFailureStrategy](MultiplayerInfrastructure.Scenario.ScenarioQuestFailureStrategy.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_NextIdentifier"></a> NextIdentifier

```csharp
public string NextIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_NodeType"></a> NodeType

```csharp
public ScenarioNodeType NodeType { get; }
```

#### Property Value

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_Operation"></a> Operation

```csharp
public ScenarioQuestOperationType Operation { get; set; }
```

#### Property Value

 [ScenarioQuestOperationType](MultiplayerInfrastructure.Scenario.ScenarioQuestOperationType.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_PersistProgressOnSessionEnd"></a> PersistProgressOnSessionEnd

null이면 QuestDefinition/인라인 QuestData의 설정을 사용합니다.
지정하면 해당 QuestControl 실행으로 생성·갱신되는 퀘스트의 세션 종료 후 진행 유지 여부를 덮어씁니다.

```csharp
public bool? PersistProgressOnSessionEnd { get; set; }
```

#### Property Value

 bool?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_Quest"></a> Quest

```csharp
public QuestData Quest { get; set; }
```

#### Property Value

 [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_QuestDefinitionIdentifier"></a> QuestDefinitionIdentifier

```csharp
public string QuestDefinitionIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioQuestControlNode_SkipCompletionDisplayDelay"></a> SkipCompletionDisplayDelay

true이면 목표 완료 연출을 기다리지 않고 이 변경을 HUD에 즉시 표시합니다.

```csharp
public bool SkipCompletionDisplayDelay { get; set; }
```

#### Property Value

 bool

