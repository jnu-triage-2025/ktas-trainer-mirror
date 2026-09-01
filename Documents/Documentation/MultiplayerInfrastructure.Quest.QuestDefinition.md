# <a id="MultiplayerInfrastructure_Quest_QuestDefinition"></a> Class QuestDefinition

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public sealed class QuestDefinition
```

#### Inheritance

object ← 
[QuestDefinition](MultiplayerInfrastructure.Quest.QuestDefinition.md)

## Properties

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_CompletionCriteria"></a> CompletionCriteria

```csharp
public List<QuestCompletionCriteria> CompletionCriteria { get; set; }
```

#### Property Value

 List<[QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_Description"></a> Description

```csharp
public string Description { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_IsAutoComplete"></a> IsAutoComplete

```csharp
public bool IsAutoComplete { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_IsOrdinal"></a> IsOrdinal

```csharp
public bool IsOrdinal { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_IsTrackable"></a> IsTrackable

```csharp
public bool IsTrackable { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_IsTrackedByDefault"></a> IsTrackedByDefault

```csharp
public bool IsTrackedByDefault { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_PersistProgressOnSessionEnd"></a> PersistProgressOnSessionEnd

세션 종료 후에도 이 정의에서 생성된 퀘스트 진행 상태를 유지할지 여부입니다.

```csharp
public bool PersistProgressOnSessionEnd { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_PresentationBindings"></a> PresentationBindings

```csharp
public List<QuestPresentationBinding> PresentationBindings { get; set; }
```

#### Property Value

 List<[QuestPresentationBinding](MultiplayerInfrastructure.Quest.QuestPresentationBinding.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_QuestContent"></a> QuestContent

```csharp
public string QuestContent { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_Scope"></a> Scope

```csharp
public QuestScopeType Scope { get; set; }
```

#### Property Value

 [QuestScopeType](MultiplayerInfrastructure.Quest.QuestScopeType.md)

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_Tasks"></a> Tasks

```csharp
public List<QuestCompletionCriteria> Tasks { get; set; }
```

#### Property Value

 List<[QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_Title"></a> Title

```csharp
public string Title { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_WaypointIdentifier"></a> WaypointIdentifier

```csharp
public string WaypointIdentifier { get; set; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Quest_QuestDefinition_Clone"></a> Clone\(\)

```csharp
public QuestDefinition Clone()
```

#### Returns

 [QuestDefinition](MultiplayerInfrastructure.Quest.QuestDefinition.md)

