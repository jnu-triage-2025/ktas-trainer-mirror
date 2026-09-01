# <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria"></a> Class QuestCompletionCriteria

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public sealed class QuestCompletionCriteria
```

#### Inheritance

object ← 
[QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)

## Fields

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_DefaultReachDistance"></a> DefaultReachDistance

```csharp
public const float DefaultReachDistance = 4
```

#### Field Value

 float

## Properties

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_Completed"></a> Completed

```csharp
[JsonIgnore]
public bool Completed { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_Conditions"></a> Conditions

```csharp
public List<QuestCompletionCriteria> Conditions { get; set; }
```

#### Property Value

 List<[QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_Count"></a> Count

```csharp
public int Count { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_DisplayTextContent"></a> DisplayTextContent

```csharp
public string DisplayTextContent { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_Identifier"></a> Identifier

```csharp
public string Identifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_ItemId"></a> ItemId

```csharp
public string ItemId { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_OnCompleteSignalIdentifier"></a> OnCompleteSignalIdentifier

```csharp
public string OnCompleteSignalIdentifier { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_Progress"></a> Progress

```csharp
[JsonIgnore]
public QuestProgressValue Progress { get; set; }
```

#### Property Value

 [QuestProgressValue](MultiplayerInfrastructure.Quest.QuestProgressValue.md)

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_ReachDistance"></a> ReachDistance

```csharp
public float ReachDistance { get; set; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_SignalId"></a> SignalId

```csharp
public string SignalId { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_Type"></a> Type

```csharp
public QuestCompletionCriteriaType Type { get; set; }
```

#### Property Value

 [QuestCompletionCriteriaType](MultiplayerInfrastructure.Quest.QuestCompletionCriteriaType.md)

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_WaypointIdentifier"></a> WaypointIdentifier

```csharp
public string WaypointIdentifier { get; set; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Quest_QuestCompletionCriteria_Clone_System_Boolean_"></a> Clone\(bool\)

```csharp
public QuestCompletionCriteria Clone(bool includeRuntimeState = true)
```

#### Parameters

`includeRuntimeState` bool

#### Returns

 [QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)

