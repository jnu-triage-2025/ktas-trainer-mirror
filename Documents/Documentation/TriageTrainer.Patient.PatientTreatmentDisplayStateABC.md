# <a id="TriageTrainer_Patient_PatientTreatmentDisplayStateABC"></a> Class PatientTreatmentDisplayStateABC

Namespace: [TriageTrainer.Patient](TriageTrainer.Patient.md)  
Assembly: Assembly\-CSharp.dll  

환자 모델이 표현 가능한 치료 과정 및 상태에 대한 추상 클래스

환자 모델의 치료가 진행 중일 때, 치료 과정 중에 시각적으로 확인 가능한 변화들에 대해 서술합니다.
대개는 특정한 장비들이 환자에 적용되거나 부착된 상태를 표시합니다.
실제로 각 환자 모델은, DisplaySupports에 정의된 내용들이 3D 모델이나 프리팹에서 구현되어있어야 합니다.

```csharp
[Serializable]
public abstract class PatientTreatmentDisplayStateABC
```

#### Inheritance

object ← 
[PatientTreatmentDisplayStateABC](TriageTrainer.Patient.PatientTreatmentDisplayStateABC.md)

#### Derived

[PatientDummyDTreatmentDisplayState](TriageTrainer.Patient.PatientDummyDTreatmentDisplayState.md), 
[PatientTypeATreatmentDisplayState](TriageTrainer.Patient.PatientTypeATreatmentDisplayState.md), 
[PatientTypeBFemaleTreatmentDisplayState](TriageTrainer.Patient.PatientTypeBFemaleTreatmentDisplayState.md), 
[PatientTypeBMaleTreatmentDisplayState](TriageTrainer.Patient.PatientTypeBMaleTreatmentDisplayState.md)

## Properties

### <a id="TriageTrainer_Patient_PatientTreatmentDisplayStateABC_ChildGameObjects"></a> ChildGameObjects

런타임 환경: 자식 GameObject 참조

```csharp
public abstract PatientTreatmentDisplayingChildGameObjects ChildGameObjects { get; set; }
```

#### Property Value

 [PatientTreatmentDisplayingChildGameObjects](TriageTrainer.Patient.PatientTreatmentDisplayingChildGameObjects.md)

### <a id="TriageTrainer_Patient_PatientTreatmentDisplayStateABC_DisplayState"></a> DisplayState

런타임 환경: 현재 환자 모델이 어떤 내용이 표시중인지 플래그

```csharp
public abstract PatientTreatmentDisplayModel DisplayState { get; set; }
```

#### Property Value

 [PatientTreatmentDisplayModel](TriageTrainer.Patient.PatientTreatmentDisplayModel.md)

### <a id="TriageTrainer_Patient_PatientTreatmentDisplayStateABC_DisplaySupports"></a> DisplaySupports

환자 모델이 표현 가능한 상태들의 내용을 정의
true인 경우 환자 모델이 표현 가능함
false인 경우 환자 모델에 관련 내용 구현되어있지 않음

```csharp
public abstract PatientTreatmentDisplayModel DisplaySupports { get; set; }
```

#### Property Value

 [PatientTreatmentDisplayModel](TriageTrainer.Patient.PatientTreatmentDisplayModel.md)

### <a id="TriageTrainer_Patient_PatientTreatmentDisplayStateABC_PatientModelGameObject"></a> PatientModelGameObject

```csharp
public abstract GameObject PatientModelGameObject { get; set; }
```

#### Property Value

 GameObject

