# <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint"></a> Class DefibrillatorCartSnapPoint

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

제세동 카트가 도착했을 때 정렬될 월드상의 고정 위치와 방향을 정의한다.

```csharp
public sealed class DefibrillatorCartSnapPoint : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[DefibrillatorCartSnapPoint](TriageTrainer.Entity.DefibrillatorCartSnapPoint.md)

## Properties

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_BlockWhenAnyDefibrillatorCartIsPresent"></a> BlockWhenAnyDefibrillatorCartIsPresent

```csharp
public bool BlockWhenAnyDefibrillatorCartIsPresent { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_BlockWhenDefibrillatorCartIsPresent"></a> BlockWhenDefibrillatorCartIsPresent

```csharp
public bool BlockWhenDefibrillatorCartIsPresent { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_DespawnExistingDefibrillatorCartWhenPresent"></a> DespawnExistingDefibrillatorCartWhenPresent

```csharp
public bool DespawnExistingDefibrillatorCartWhenPresent { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_Position"></a> Position

```csharp
public Vector3 Position { get; }
```

#### Property Value

 Vector3

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_ReleaseParticipantsOnSnap"></a> ReleaseParticipantsOnSnap

```csharp
public bool ReleaseParticipantsOnSnap { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_Rotation"></a> Rotation

```csharp
public Quaternion Rotation { get; }
```

#### Property Value

 Quaternion

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_SnapDistance"></a> SnapDistance

```csharp
public float SnapDistance { get; }
```

#### Property Value

 float

## Methods

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_ConfigureOccupiedArea_UnityEngine_Vector2_System_Single_"></a> ConfigureOccupiedArea\(Vector2, float\)

```csharp
public void ConfigureOccupiedArea(Vector2 occupiedSizeValue, float displayHeightValue)
```

#### Parameters

`occupiedSizeValue` Vector2

`displayHeightValue` float

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_IsWithinSnapDistance_UnityEngine_Vector3_"></a> IsWithinSnapDistance\(Vector3\)

```csharp
public bool IsWithinSnapDistance(Vector3 worldPosition)
```

#### Parameters

`worldPosition` Vector3

#### Returns

 bool

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_SetIdentifier_System_String_"></a> SetIdentifier\(string\)

```csharp
public void SetIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_DefibrillatorCartSnapPoint_SetIdentifierForEditor_System_String_"></a> SetIdentifierForEditor\(string\)

```csharp
public void SetIdentifierForEditor(string identifier)
```

#### Parameters

`identifier` string

