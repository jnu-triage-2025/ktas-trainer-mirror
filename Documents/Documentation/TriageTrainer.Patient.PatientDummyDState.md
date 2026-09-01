# <a id="TriageTrainer_Patient_PatientDummyDState"></a> Class PatientDummyDState

Namespace: [TriageTrainer.Patient](TriageTrainer.Patient.md)  
Assembly: Assembly\-CSharp.dll  

시나리오의 분류 전용 D 더미 환자 상태입니다.

```csharp
public class PatientDummyDState : PatientStateABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PatientStateABC](TriageTrainer.Patient.PatientStateABC.md) ← 
[PatientDummyDState](TriageTrainer.Patient.PatientDummyDState.md)

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

### <a id="TriageTrainer_Patient_PatientDummyDState_TreatmentDisplayState"></a> TreatmentDisplayState

```csharp
public override PatientTreatmentDisplayStateABC TreatmentDisplayState { get; }
```

#### Property Value

 [PatientTreatmentDisplayStateABC](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md)

