# <a id="TriageTrainer_Patient_PatientTreatmentState"></a> Class PatientTreatmentState

Namespace: [TriageTrainer.Patient](TriageTrainer.Patient.md)  
Assembly: Assembly\-CSharp.dll  

시각 표현과 독립적으로 환자에게 완료된 처치를 보관하는 직렬화 데이터.

```csharp
[Serializable]
public sealed class PatientTreatmentState
```

#### Inheritance

object ← 
[PatientTreatmentState](TriageTrainer.Patient.PatientTreatmentState.md)

## Properties

### <a id="TriageTrainer_Patient_PatientTreatmentState_AppliedTreatments"></a> AppliedTreatments

```csharp
public IReadOnlyList<string> AppliedTreatments { get; }
```

#### Property Value

 IReadOnlyList<string\>

## Methods

### <a id="TriageTrainer_Patient_PatientTreatmentState_ApplySnapshot_System_Collections_Generic_IEnumerable_System_String__"></a> ApplySnapshot\(IEnumerable<string\>\)

```csharp
public void ApplySnapshot(IEnumerable<string> treatmentIdentifiers)
```

#### Parameters

`treatmentIdentifiers` IEnumerable<string\>

### <a id="TriageTrainer_Patient_PatientTreatmentState_CreateSnapshot"></a> CreateSnapshot\(\)

```csharp
public string[] CreateSnapshot()
```

#### Returns

 string\[\]

### <a id="TriageTrainer_Patient_PatientTreatmentState_IsApplied_System_String_"></a> IsApplied\(string\)

```csharp
public bool IsApplied(string treatmentIdentifier)
```

#### Parameters

`treatmentIdentifier` string

#### Returns

 bool

### <a id="TriageTrainer_Patient_PatientTreatmentState_SetApplied_System_String_System_Boolean_"></a> SetApplied\(string, bool\)

```csharp
public bool SetApplied(string treatmentIdentifier, bool applied)
```

#### Parameters

`treatmentIdentifier` string

`applied` bool

#### Returns

 bool

