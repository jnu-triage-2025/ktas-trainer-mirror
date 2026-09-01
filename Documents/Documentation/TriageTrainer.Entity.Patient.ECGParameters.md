# <a id="TriageTrainer_Entity_Patient_ECGParameters"></a> Struct ECGParameters

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public struct ECGParameters
```

## Fields

### <a id="TriageTrainer_Entity_Patient_ECGParameters_bpm"></a> bpm

```csharp
[Range(0, 250)]
public float bpm
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_irregularity"></a> irregularity

```csharp
[Range(0, 1)]
public float irregularity
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_noise"></a> noise

```csharp
public float noise
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_pAmp"></a> pAmp

```csharp
public float pAmp
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_pWidth"></a> pWidth

```csharp
public float pWidth
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_qAmp"></a> qAmp

```csharp
public float qAmp
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_qrsWidthScale"></a> qrsWidthScale

```csharp
public float qrsWidthScale
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_rAmp"></a> rAmp

```csharp
public float rAmp
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_sAmp"></a> sAmp

```csharp
public float sAmp
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_stElevation"></a> stElevation

```csharp
public float stElevation
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_tAmp"></a> tAmp

```csharp
public float tAmp
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_tWidth"></a> tWidth

```csharp
public float tWidth
```

#### Field Value

 float

### <a id="TriageTrainer_Entity_Patient_ECGParameters_uAmp"></a> uAmp

```csharp
public float uAmp
```

#### Field Value

 float

## Properties

### <a id="TriageTrainer_Entity_Patient_ECGParameters_Normal"></a> Normal

```csharp
public static ECGParameters Normal { get; }
```

#### Property Value

 [ECGParameters](TriageTrainer.Entity.Patient.ECGParameters.md)

## Methods

### <a id="TriageTrainer_Entity_Patient_ECGParameters_FromRhythm_TriageTrainer_Entity_Patient_ECGRhythmType_"></a> FromRhythm\(ECGRhythmType\)

```csharp
public static ECGParameters FromRhythm(ECGRhythmType rhythm)
```

#### Parameters

`rhythm` [ECGRhythmType](TriageTrainer.Entity.Patient.ECGRhythmType.md)

#### Returns

 [ECGParameters](TriageTrainer.Entity.Patient.ECGParameters.md)

