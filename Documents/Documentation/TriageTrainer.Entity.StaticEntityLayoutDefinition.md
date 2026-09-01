# <a id="TriageTrainer_Entity_StaticEntityLayoutDefinition"></a> Class StaticEntityLayoutDefinition

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[CreateAssetMenu(menuName = "Triage Trainer/Overworld/Static Entity Layout", fileName = "StaticEntityLayout")]
public sealed class StaticEntityLayoutDefinition : ScriptableObject
```

#### Inheritance

object ← 
Object ← 
ScriptableObject ← 
[StaticEntityLayoutDefinition](TriageTrainer.Entity.StaticEntityLayoutDefinition.md)

## Fields

### <a id="TriageTrainer_Entity_StaticEntityLayoutDefinition_groups"></a> groups

```csharp
public List<StaticEntityLayoutGroup> groups
```

#### Field Value

 List<[StaticEntityLayoutGroup](TriageTrainer.Entity.StaticEntityLayoutGroup.md)\>

### <a id="TriageTrainer_Entity_StaticEntityLayoutDefinition_identifier"></a> identifier

```csharp
public string identifier
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_StaticEntityLayoutDefinition_oxyflowmeterPrefab"></a> oxyflowmeterPrefab

```csharp
public GameObject oxyflowmeterPrefab
```

#### Field Value

 GameObject

### <a id="TriageTrainer_Entity_StaticEntityLayoutDefinition_wallSuctionPrefab"></a> wallSuctionPrefab

```csharp
public GameObject wallSuctionPrefab
```

#### Field Value

 GameObject

## Methods

### <a id="TriageTrainer_Entity_StaticEntityLayoutDefinition_SupportsAttachCompletionSignal_TriageTrainer_Entity_StaticEntityLayoutType_"></a> SupportsAttachCompletionSignal\(StaticEntityLayoutType\)

```csharp
public static bool SupportsAttachCompletionSignal(StaticEntityLayoutType type)
```

#### Parameters

`type` [StaticEntityLayoutType](TriageTrainer.Entity.StaticEntityLayoutType.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_StaticEntityLayoutDefinition_Validate_System_String__"></a> Validate\(out string\)

```csharp
public bool Validate(out string error)
```

#### Parameters

`error` string

#### Returns

 bool

