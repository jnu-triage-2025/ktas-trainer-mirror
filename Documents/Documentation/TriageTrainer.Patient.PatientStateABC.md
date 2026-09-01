# <a id="TriageTrainer_Patient_PatientStateABC"></a> Class PatientStateABC

Namespace: [TriageTrainer.Patient](TriageTrainer.Patient.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public abstract class PatientStateABC : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PatientStateABC](TriageTrainer.Patient.PatientStateABC.md)

#### Derived

[PatientDummyDState](TriageTrainer.Patient.PatientDummyDState.md), 
[PatientTypeAState](TriageTrainer.Patient.PatientTypeAState.md), 
[PatientTypeBFemaleState](TriageTrainer.Patient.PatientTypeBFemaleState.md), 
[PatientTypeBMaleState](TriageTrainer.Patient.PatientTypeBMaleState.md)

## Properties

### <a id="TriageTrainer_Patient_PatientStateABC_ColliderCenterOnLayingOnPatientMovingBed"></a> ColliderCenterOnLayingOnPatientMovingBed

```csharp
public virtual Vector3 ColliderCenterOnLayingOnPatientMovingBed { get; }
```

#### Property Value

 Vector3

### <a id="TriageTrainer_Patient_PatientStateABC_ColliderDirectionOnLayingOnPatientMovingBed"></a> ColliderDirectionOnLayingOnPatientMovingBed

```csharp
public virtual int ColliderDirectionOnLayingOnPatientMovingBed { get; }
```

#### Property Value

 int

### <a id="TriageTrainer_Patient_PatientStateABC_ColliderHeightOnLayingOnPatientMovingBed"></a> ColliderHeightOnLayingOnPatientMovingBed

```csharp
public virtual float ColliderHeightOnLayingOnPatientMovingBed { get; }
```

#### Property Value

 float

### <a id="TriageTrainer_Patient_PatientStateABC_ColliderRadiusOnLayingOnPatientMovingBed"></a> ColliderRadiusOnLayingOnPatientMovingBed

```csharp
public virtual float ColliderRadiusOnLayingOnPatientMovingBed { get; }
```

#### Property Value

 float

### <a id="TriageTrainer_Patient_PatientStateABC_Descriptor"></a> Descriptor

```csharp
public PatientDescriptor Descriptor { get; }
```

#### Property Value

 [PatientDescriptor](TriageTrainer.Entity.Patient.PatientDescriptor.md)

### <a id="TriageTrainer_Patient_PatientStateABC_EulerAnglesOnLayingOnPatientMovingBed"></a> EulerAnglesOnLayingOnPatientMovingBed

```csharp
public virtual Vector3 EulerAnglesOnLayingOnPatientMovingBed { get; }
```

#### Property Value

 Vector3

### <a id="TriageTrainer_Patient_PatientStateABC_PositionOnLayingOnPatientMovingBed"></a> PositionOnLayingOnPatientMovingBed

```csharp
public virtual Vector3 PositionOnLayingOnPatientMovingBed { get; }
```

#### Property Value

 Vector3

### <a id="TriageTrainer_Patient_PatientStateABC_RestoresLegacyPatientControllerDefaultsOnInspectorReset"></a> RestoresLegacyPatientControllerDefaultsOnInspectorReset

기존 프리팹 직렬화값을 코드 기본값으로 복구해야 하는 환자 유형인지 여부.
기본값은 false이며, 해당 환자 유형만 명시적으로 opt-in 한다.

```csharp
public virtual bool RestoresLegacyPatientControllerDefaultsOnInspectorReset { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Patient_PatientStateABC_TreatmentDisplayState"></a> TreatmentDisplayState

```csharp
public abstract PatientTreatmentDisplayStateABC TreatmentDisplayState { get; }
```

#### Property Value

 [PatientTreatmentDisplayStateABC](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md)

## Methods

### <a id="TriageTrainer_Patient_PatientStateABC_Awake"></a> Awake\(\)

```csharp
protected virtual void Awake()
```

### <a id="TriageTrainer_Patient_PatientStateABC_ConfigureRuntimeReferences_TriageTrainer_Entity_PatientController_"></a> ConfigureRuntimeReferences\(PatientController\)

```csharp
protected virtual void ConfigureRuntimeReferences(PatientController controller)
```

#### Parameters

`controller` [PatientController](TriageTrainer.Entity.PatientController.md)

### <a id="TriageTrainer_Patient_PatientStateABC_FindSingleOxygenInterface"></a> FindSingleOxygenInterface\(\)

B/C의 비강 캐뉼라는 이미 <xref href="TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint" data-throw-if-not-resolved="false"></xref>로 식별된다.
별도 마커나 프리팹 fileID에 의존하지 않고 현재 환자 오브젝트에서 찾는다.
다만 둘 이상이면 임의의 첫 포트를 사용하지 않는다.

```csharp
protected OxyLineConnectionPoint FindSingleOxygenInterface()
```

#### Returns

 [OxyLineConnectionPoint](TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint.md)

### <a id="TriageTrainer_Patient_PatientStateABC_InitializeRuntimeReferences_TriageTrainer_Entity_PatientController_"></a> InitializeRuntimeReferences\(PatientController\)

```csharp
public void InitializeRuntimeReferences(PatientController controller)
```

#### Parameters

`controller` [PatientController](TriageTrainer.Entity.PatientController.md)

