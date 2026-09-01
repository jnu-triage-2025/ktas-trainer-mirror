# <a id="TriageTrainer_Entity_PatientMonitor_Models_ECGWaveformCalculator"></a> Class ECGWaveformCalculator

Namespace: [TriageTrainer.Entity.PatientMonitor.Models](TriageTrainer.Entity.PatientMonitor.Models.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class ECGWaveformCalculator
```

#### Inheritance

object ← 
[ECGWaveformCalculator](TriageTrainer.Entity.PatientMonitor.Models.ECGWaveformCalculator.md)

## Methods

### <a id="TriageTrainer_Entity_PatientMonitor_Models_ECGWaveformCalculator_Calculate_TriageTrainer_Entity_Patient_ECGParameters__TriageTrainer_Entity_Patient_ECGRhythmType_System_Single_System_Single_TriageTrainer_Entity_PatientMonitor_Models_ECGRuntimeState__"></a> Calculate\(in ECGParameters, ECGRhythmType, float, float, ref ECGRuntimeState\)

```csharp
public static float Calculate(in ECGParameters parameters, ECGRhythmType rhythmType, float cycleNorm, float dt, ref ECGRuntimeState runtimeState)
```

#### Parameters

`parameters` [ECGParameters](TriageTrainer.Entity.Patient.ECGParameters.md)

`rhythmType` [ECGRhythmType](TriageTrainer.Entity.Patient.ECGRhythmType.md)

`cycleNorm` float

`dt` float

`runtimeState` [ECGRuntimeState](TriageTrainer.Entity.PatientMonitor.Models.ECGRuntimeState.md)

#### Returns

 float

### <a id="TriageTrainer_Entity_PatientMonitor_Models_ECGWaveformCalculator_ConsumePendingPrematureBeat_TriageTrainer_Entity_PatientMonitor_Models_ECGRuntimeState__"></a> ConsumePendingPrematureBeat\(ref ECGRuntimeState\)

```csharp
public static bool ConsumePendingPrematureBeat(ref ECGRuntimeState runtimeState)
```

#### Parameters

`runtimeState` [ECGRuntimeState](TriageTrainer.Entity.PatientMonitor.Models.ECGRuntimeState.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientMonitor_Models_ECGWaveformCalculator_OnBeat_TriageTrainer_Entity_Patient_ECGRhythmType_TriageTrainer_Entity_PatientMonitor_Models_ECGRuntimeState__"></a> OnBeat\(ECGRhythmType, ref ECGRuntimeState\)

```csharp
public static void OnBeat(ECGRhythmType rhythmType, ref ECGRuntimeState runtimeState)
```

#### Parameters

`rhythmType` [ECGRhythmType](TriageTrainer.Entity.Patient.ECGRhythmType.md)

`runtimeState` [ECGRuntimeState](TriageTrainer.Entity.PatientMonitor.Models.ECGRuntimeState.md)

