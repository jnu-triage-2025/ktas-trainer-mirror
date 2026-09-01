# <a id="TriageTrainer_Entity_Patient_ARTParameters"></a> Struct ARTParameters

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public struct ARTParameters
```

## Fields

### <a id="TriageTrainer_Entity_Patient_ARTParameters_bpm"></a> bpm

```csharp
[Range(0, 250)]
public float bpm
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ARTParameters_diastolic"></a> diastolic

```csharp
[Range(20, 180)]
public float diastolic
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ARTParameters_noise"></a> noise

```csharp
[Range(0, 1)]
public float noise
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ARTParameters_systolic"></a> systolic

```csharp
[Range(40, 260)]
public float systolic
```

#### Field Value

 float

## Properties

### <a id="TriageTrainer_Entity_Patient_ARTParameters_Default"></a> Default

```csharp
public static ARTParameters Default { get; }
```

#### Property Value

 [ARTParameters](TriageTrainer.Entity.Patient.ARTParameters.md)

