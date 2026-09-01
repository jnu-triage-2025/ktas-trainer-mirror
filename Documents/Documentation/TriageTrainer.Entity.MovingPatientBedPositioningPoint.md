# <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint"></a> Class MovingPatientBedPositioningPoint

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

이동식 환자 침대가 도착했을 때 정렬될 월드상의 고정 위치와 방향을 정의한다.

```csharp
public sealed class MovingPatientBedPositioningPoint : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[MovingPatientBedPositioningPoint](TriageTrainer.Entity.MovingPatientBedPositioningPoint.md)

## Properties

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_BlockWhenAnyBedIsPresent"></a> BlockWhenAnyBedIsPresent

빈 침대를 제거하지 않을 때 모든 기존 침대를 차단물로 취급할지 여부.

```csharp
public bool BlockWhenAnyBedIsPresent { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_BlockWhenPatientBedIsPresent"></a> BlockWhenPatientBedIsPresent

환자가 결합된 침대가 점유 중일 때 새 positioning을 차단할지 여부.

```csharp
public bool BlockWhenPatientBedIsPresent { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_DespawnEmptyBedWhenPresent"></a> DespawnEmptyBedWhenPresent

점유 중인 빈 침대를 새 positioning 전에 서버에서 제거할지 여부.

```csharp
public bool DespawnEmptyBedWhenPresent { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_Identifier"></a> Identifier

시나리오 신호와 세션 로그에서 이 위치를 식별하는 안정적인 키.

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_Position"></a> Position

```csharp
public Vector3 Position { get; }
```

#### Property Value

 Vector3

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_ReleaseParticipantsOnSnap"></a> ReleaseParticipantsOnSnap

스냅 완료 시 침대 조종 참가자를 모두 자동 해제할지 여부.

```csharp
public bool ReleaseParticipantsOnSnap { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_Rotation"></a> Rotation

```csharp
public Quaternion Rotation { get; }
```

#### Property Value

 Quaternion

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_SnapDistance"></a> SnapDistance

```csharp
public float SnapDistance { get; }
```

#### Property Value

 float

## Methods

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_ConfigureOccupiedArea_UnityEngine_Vector2_System_Single_"></a> ConfigureOccupiedArea\(Vector2, float\)

```csharp
public void ConfigureOccupiedArea(Vector2 occupiedSizeValue, float displayHeightValue)
```

#### Parameters

`occupiedSizeValue` Vector2

`displayHeightValue` float

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_IsWithinSnapDistance_UnityEngine_Vector3_"></a> IsWithinSnapDistance\(Vector3\)

```csharp
public bool IsWithinSnapDistance(Vector3 worldPosition)
```

#### Parameters

`worldPosition` Vector3

#### Returns

 bool

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_SetIdentifier_System_String_"></a> SetIdentifier\(string\)

```csharp
public void SetIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_MovingPatientBedPositioningPoint_SetIdentifierForEditor_System_String_"></a> SetIdentifierForEditor\(string\)

```csharp
public void SetIdentifierForEditor(string identifier)
```

#### Parameters

`identifier` string

