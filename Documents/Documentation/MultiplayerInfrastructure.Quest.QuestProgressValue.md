# <a id="MultiplayerInfrastructure_Quest_QuestProgressValue"></a> Class QuestProgressValue

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public sealed class QuestProgressValue
```

#### Inheritance

object ← 
[QuestProgressValue](MultiplayerInfrastructure.Quest.QuestProgressValue.md)

## Constructors

### <a id="MultiplayerInfrastructure_Quest_QuestProgressValue__ctor"></a> QuestProgressValue\(\)

```csharp
public QuestProgressValue()
```

### <a id="MultiplayerInfrastructure_Quest_QuestProgressValue__ctor_System_Int32_System_Int32_"></a> QuestProgressValue\(int, int\)

```csharp
public QuestProgressValue(int current, int target)
```

#### Parameters

`current` int

`target` int

## Properties

### <a id="MultiplayerInfrastructure_Quest_QuestProgressValue_Current"></a> Current

```csharp
public int Current { get; set; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_Quest_QuestProgressValue_SingleStep"></a> SingleStep

```csharp
public static QuestProgressValue SingleStep { get; }
```

#### Property Value

 [QuestProgressValue](MultiplayerInfrastructure.Quest.QuestProgressValue.md)

### <a id="MultiplayerInfrastructure_Quest_QuestProgressValue_Target"></a> Target

```csharp
public int Target { get; set; }
```

#### Property Value

 int

## Methods

### <a id="MultiplayerInfrastructure_Quest_QuestProgressValue_Clone"></a> Clone\(\)

```csharp
public QuestProgressValue Clone()
```

#### Returns

 [QuestProgressValue](MultiplayerInfrastructure.Quest.QuestProgressValue.md)

### <a id="MultiplayerInfrastructure_Quest_QuestProgressValue_ToDisplayText"></a> ToDisplayText\(\)

```csharp
public string ToDisplayText()
```

#### Returns

 string

