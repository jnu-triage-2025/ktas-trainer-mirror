# <a id="MultiplayerInfrastructure_Quest_QuestData"></a> Class QuestData

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public class QuestData
```

#### Inheritance

object ← 
[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

## Constructors

### <a id="MultiplayerInfrastructure_Quest_QuestData__ctor"></a> QuestData\(\)

```csharp
public QuestData()
```

### <a id="MultiplayerInfrastructure_Quest_QuestData__ctor_System_String_System_String_System_String_System_String_System_Boolean_System_String_"></a> QuestData\(string, string, string, string, bool, string\)

```csharp
public QuestData(string id, string title, string description, string questContent, bool isTracked = false, string waypointIdentifier = null)
```

#### Parameters

`id` string

`title` string

`description` string

`questContent` string

`isTracked` bool

`waypointIdentifier` string

## Properties

### <a id="MultiplayerInfrastructure_Quest_QuestData_Completed"></a> Completed

```csharp
public bool Completed { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestData_CompletionCriteria"></a> CompletionCriteria

```csharp
public List<QuestCompletionCriteria> CompletionCriteria { get; set; }
```

#### Property Value

 List<[QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestData_DefinitionIdentifier"></a> DefinitionIdentifier

```csharp
public string DefinitionIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestData_Description"></a> Description

```csharp
public string Description { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestData_Id"></a> Id

```csharp
public string Id { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestData_IsAutoComplete"></a> IsAutoComplete

```csharp
public bool IsAutoComplete { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestData_IsOrdinal"></a> IsOrdinal

```csharp
public bool IsOrdinal { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestData_IsTrackable"></a> IsTrackable

```csharp
public bool IsTrackable { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestData_IsTracked"></a> IsTracked

```csharp
public bool IsTracked { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestData_PersistProgressOnSessionEnd"></a> PersistProgressOnSessionEnd

세션이 종료된 뒤에도 이 퀘스트의 진행 상태를 유지할지 여부입니다.

```csharp
public bool PersistProgressOnSessionEnd { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestData_PresentationBindings"></a> PresentationBindings

```csharp
public List<QuestPresentationBinding> PresentationBindings { get; set; }
```

#### Property Value

 List<[QuestPresentationBinding](MultiplayerInfrastructure.Quest.QuestPresentationBinding.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestData_Progress"></a> Progress

```csharp
public QuestProgressValue Progress { get; set; }
```

#### Property Value

 [QuestProgressValue](MultiplayerInfrastructure.Quest.QuestProgressValue.md)

### <a id="MultiplayerInfrastructure_Quest_QuestData_QuestContent"></a> QuestContent

```csharp
public string QuestContent { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestData_Scope"></a> Scope

```csharp
public QuestScopeType Scope { get; set; }
```

#### Property Value

 [QuestScopeType](MultiplayerInfrastructure.Quest.QuestScopeType.md)

### <a id="MultiplayerInfrastructure_Quest_QuestData_SourceScenarioIdentifier"></a> SourceScenarioIdentifier

```csharp
[JsonIgnore]
public string SourceScenarioIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestData_Tasks"></a> Tasks

```csharp
public List<QuestCompletionCriteria> Tasks { get; set; }
```

#### Property Value

 List<[QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestData_Title"></a> Title

```csharp
public string Title { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestData_WaypointIdentifier"></a> WaypointIdentifier

```csharp
public string WaypointIdentifier { get; set; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Quest_QuestData_Clone"></a> Clone\(\)

```csharp
public QuestData Clone()
```

#### Returns

 [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

