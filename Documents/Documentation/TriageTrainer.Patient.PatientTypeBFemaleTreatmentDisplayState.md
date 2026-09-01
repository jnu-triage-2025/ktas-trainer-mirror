# <a id="TriageTrainer_Patient_PatientTypeBFemaleTreatmentDisplayState"></a> Class PatientTypeBFemaleTreatmentDisplayState

Namespace: [TriageTrainer.Patient](TriageTrainer.Patient.md)  
Assembly: Assembly\-CSharp.dll  

시나리오1의 환자 유형 B(남성)에 대한 치료 표시 상태를 정의

```csharp
[Serializable]
public class PatientTypeBFemaleTreatmentDisplayState : PatientTreatmentDisplayStateABC
```

#### Inheritance

object ← 
[PatientTreatmentDisplayStateABC](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md) ← 
[PatientTypeBFemaleTreatmentDisplayState](TriageTrainer.Patient.PatientTypeBFemaleTreatmentDisplayState.md)

#### Inherited Members

[PatientTreatmentDisplayStateABC.DisplaySupports](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md\#TriageTrainer\_Patient\_PatientTreatmentDisplayStateABC\_DisplaySupports), 
[PatientTreatmentDisplayStateABC.DisplayState](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md\#TriageTrainer\_Patient\_PatientTreatmentDisplayStateABC\_DisplayState), 
[PatientTreatmentDisplayStateABC.PatientModelGameObject](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md\#TriageTrainer\_Patient\_PatientTreatmentDisplayStateABC\_PatientModelGameObject), 
[PatientTreatmentDisplayStateABC.ChildGameObjects](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md\#TriageTrainer\_Patient\_PatientTreatmentDisplayStateABC\_ChildGameObjects)

## Properties

### <a id="TriageTrainer_Patient_PatientTypeBFemaleTreatmentDisplayState_ChildGameObjects"></a> ChildGameObjects

런타임 환경: 자식 GameObject 참조

```csharp
public override PatientTreatmentDisplayingChildGameObjects ChildGameObjects { get; set; }
```

#### Property Value

 [PatientTreatmentDisplayingChildGameObjects](TriageTrainer.Patient.PatientTreatmentDisplayingChildGameObjects.md)

### <a id="TriageTrainer_Patient_PatientTypeBFemaleTreatmentDisplayState_DisplayState"></a> DisplayState

런타임 환경: 현재 환자 모델이 어떤 내용이 표시중인지 플래그

```csharp
public override PatientTreatmentDisplayModel DisplayState { get; set; }
```

#### Property Value

 [PatientTreatmentDisplayModel](TriageTrainer.Patient.PatientTreatmentDisplayModel.md)

### <a id="TriageTrainer_Patient_PatientTypeBFemaleTreatmentDisplayState_DisplaySupports"></a> DisplaySupports

환자 모델이 표현 가능한 상태들의 내용을 정의
true인 경우 환자 모델이 표현 가능함
false인 경우 환자 모델에 관련 내용 구현되어있지 않음

```csharp
public override PatientTreatmentDisplayModel DisplaySupports { get; set; }
```

#### Property Value

 [PatientTreatmentDisplayModel](TriageTrainer.Patient.PatientTreatmentDisplayModel.md)

### <a id="TriageTrainer_Patient_PatientTypeBFemaleTreatmentDisplayState_PatientModelGameObject"></a> PatientModelGameObject

```csharp
public override GameObject PatientModelGameObject { get; set; }
```

#### Property Value

 GameObject

