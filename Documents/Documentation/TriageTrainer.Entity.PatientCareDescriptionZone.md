# <a id="TriageTrainer_Entity_PatientCareDescriptionZone"></a> Class PatientCareDescriptionZone

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

환자 케어에 사용할 벽면 장비의 인식 범위를 정의한다.

<p>구역에 들어온 환자에게 구역 안의 벽면 석션과 산소 유량계를
환자 측 장비 역참조로 연결한다. 장비는 의도상 하나지만, 배치 오류나
확장 시에도 누락되지 않도록 감지 결과는 목록으로 보관한다.</p>

```csharp
[RequireComponent(typeof(BoxCollider))]
public sealed class PatientCareDescriptionZone : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PatientCareDescriptionZone](TriageTrainer.Entity.PatientCareDescriptionZone.md)

## Properties

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_CurrentPatient"></a> CurrentPatient

```csharp
public PatientController CurrentPatient { get; }
```

#### Property Value

 [PatientController](TriageTrainer.Entity.PatientController.md)

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_DefibrillatorCarts"></a> DefibrillatorCarts

이 구역 범위 안에 있는 제세동 카트 목록입니다.
이동식 장비이므로 조회 직전에 위치를 다시 확인합니다.

```csharp
public IReadOnlyList<DefibrillatorCartController> DefibrillatorCarts { get; }
```

#### Property Value

 IReadOnlyList<[DefibrillatorCartController](TriageTrainer.Entity.DefibrillatorCartController.md)\>

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_Oxyflowmeters"></a> Oxyflowmeters

```csharp
public IReadOnlyList<WallAttachedOxyflowmeter> Oxyflowmeters { get; }
```

#### Property Value

 IReadOnlyList<[WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)\>

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_WallSuction"></a> WallSuction

```csharp
public IReadOnlyList<WallAttachedWallSuction> WallSuction { get; }
```

#### Property Value

 IReadOnlyList<[WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)\>

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_WorldCenter"></a> WorldCenter

```csharp
public Vector3 WorldCenter { get; }
```

#### Property Value

 Vector3

## Methods

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_ConfigureArea_UnityEngine_Vector3_UnityEngine_Vector3_"></a> ConfigureArea\(Vector3, Vector3\)

```csharp
public void ConfigureArea(Vector3 center, Vector3 size)
```

#### Parameters

`center` Vector3

`size` Vector3

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_ConfigureSize_UnityEngine_Vector3_"></a> ConfigureSize\(Vector3\)

```csharp
public void ConfigureSize(Vector3 size)
```

#### Parameters

`size` Vector3

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_ContainsWorldPosition_UnityEngine_Vector3_"></a> ContainsWorldPosition\(Vector3\)

```csharp
public bool ContainsWorldPosition(Vector3 worldPosition)
```

#### Parameters

`worldPosition` Vector3

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_GetPatientForOxyflowmeter_TriageTrainer_Entity_WallAttachedOxyflowmeter_"></a> GetPatientForOxyflowmeter\(WallAttachedOxyflowmeter\)

```csharp
public PatientController GetPatientForOxyflowmeter(WallAttachedOxyflowmeter flowmeter)
```

#### Parameters

`flowmeter` [WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)

#### Returns

 [PatientController](TriageTrainer.Entity.PatientController.md)

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_SetIdentifierForEditor_System_String_"></a> SetIdentifierForEditor\(string\)

```csharp
public void SetIdentifierForEditor(string identifier)
```

#### Parameters

`identifier` string

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_TryReconcileOxygenLineFor_TriageTrainer_Entity_WallAttachedOxyflowmeter_"></a> TryReconcileOxygenLineFor\(WallAttachedOxyflowmeter\)

Reconciles the oxygen line after a flowmeter operation when this zone contains
both the flowmeter and its current patient.

```csharp
public void TryReconcileOxygenLineFor(WallAttachedOxyflowmeter flowmeter)
```

#### Parameters

`flowmeter` [WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_PatientEntered"></a> PatientEntered

```csharp
public event Action<PatientController> PatientEntered
```

#### Event Type

 Action<[PatientController](TriageTrainer.Entity.PatientController.md)\>

### <a id="TriageTrainer_Entity_PatientCareDescriptionZone_PatientExited"></a> PatientExited

```csharp
public event Action<PatientController> PatientExited
```

#### Event Type

 Action<[PatientController](TriageTrainer.Entity.PatientController.md)\>

