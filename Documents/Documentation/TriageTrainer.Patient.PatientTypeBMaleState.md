# <a id="TriageTrainer_Patient_PatientTypeBMaleState"></a> Class PatientTypeBMaleState

Namespace: [TriageTrainer.Patient](TriageTrainer.Patient.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class PatientTypeBMaleState : PatientStateABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PatientStateABC](TriageTrainer.Patient.PatientStateABC.md) ← 
[PatientTypeBMaleState](TriageTrainer.Patient.PatientTypeBMaleState.md)

#### Inherited Members

[PatientStateABC.Descriptor](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_Descriptor), 
[PatientStateABC.PositionOnLayingOnPatientMovingBed](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_PositionOnLayingOnPatientMovingBed), 
[PatientStateABC.EulerAnglesOnLayingOnPatientMovingBed](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_EulerAnglesOnLayingOnPatientMovingBed), 
[PatientStateABC.ColliderCenterOnLayingOnPatientMovingBed](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_ColliderCenterOnLayingOnPatientMovingBed), 
[PatientStateABC.ColliderHeightOnLayingOnPatientMovingBed](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_ColliderHeightOnLayingOnPatientMovingBed), 
[PatientStateABC.ColliderRadiusOnLayingOnPatientMovingBed](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_ColliderRadiusOnLayingOnPatientMovingBed), 
[PatientStateABC.ColliderDirectionOnLayingOnPatientMovingBed](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_ColliderDirectionOnLayingOnPatientMovingBed), 
[PatientStateABC.TreatmentDisplayState](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_TreatmentDisplayState), 
[PatientStateABC.RestoresLegacyPatientControllerDefaultsOnInspectorReset](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_RestoresLegacyPatientControllerDefaultsOnInspectorReset), 
[PatientStateABC.Awake\(\)](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_Awake), 
[PatientStateABC.InitializeRuntimeReferences\(PatientController\)](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_InitializeRuntimeReferences\_TriageTrainer\_Entity\_PatientController\_), 
[PatientStateABC.ConfigureRuntimeReferences\(PatientController\)](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_ConfigureRuntimeReferences\_TriageTrainer\_Entity\_PatientController\_), 
[PatientStateABC.FindSingleOxygenInterface\(\)](TriageTrainer.Patient.PatientStateABC.md\#TriageTrainer\_Patient\_PatientStateABC\_FindSingleOxygenInterface)

## Properties

### <a id="TriageTrainer_Patient_PatientTypeBMaleState_IntravenousLineConnectionPoint"></a> IntravenousLineConnectionPoint

이 환자 유형의 정맥로 측 IV 연결 지점(Inspector 배선).

```csharp
public IntravenousLineConnectionPoint IntravenousLineConnectionPoint { get; }
```

#### Property Value

 [IntravenousLineConnectionPoint](TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint.md)

### <a id="TriageTrainer_Patient_PatientTypeBMaleState_RestoresLegacyPatientControllerDefaultsOnInspectorReset"></a> RestoresLegacyPatientControllerDefaultsOnInspectorReset

기존 프리팹 직렬화값을 코드 기본값으로 복구해야 하는 환자 유형인지 여부.
기본값은 false이며, 해당 환자 유형만 명시적으로 opt-in 한다.

```csharp
public override bool RestoresLegacyPatientControllerDefaultsOnInspectorReset { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Patient_PatientTypeBMaleState_TreatmentDisplayState"></a> TreatmentDisplayState

```csharp
public override PatientTreatmentDisplayStateABC TreatmentDisplayState { get; }
```

#### Property Value

 [PatientTreatmentDisplayStateABC](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md)

## Methods

### <a id="TriageTrainer_Patient_PatientTypeBMaleState_ConfigureRuntimeReferences_TriageTrainer_Entity_PatientController_"></a> ConfigureRuntimeReferences\(PatientController\)

```csharp
protected override void ConfigureRuntimeReferences(PatientController controller)
```

#### Parameters

`controller` [PatientController](TriageTrainer.Entity.PatientController.md)

