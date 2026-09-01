# <a id="TriageTrainer_Entity_Patient_BloodPulse"></a> Class BloodPulse

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

맥박을 표현합니다.

```csharp
public class BloodPulse
```

#### Inheritance

object ← 
[BloodPulse](TriageTrainer.Entity.Patient.BloodPulse.md)

## Fields

### <a id="TriageTrainer_Entity_Patient_BloodPulse_forceType"></a> forceType

맥박의 세기 유형을 BloodPulseForceType에 의해 정의된 유형에 따라 표현합니다.

```csharp
public BloodPulseForceType forceType
```

#### Field Value

 [BloodPulseForceType](TriageTrainer.Entity.Patient.BloodPulseForceType.md)

### <a id="TriageTrainer_Entity_Patient_BloodPulse_rate"></a> rate

분당 맥박수

```csharp
public int rate
```

#### Field Value

 int

## Properties

### <a id="TriageTrainer_Entity_Patient_BloodPulse_SpeedType"></a> SpeedType

맥박의 속도 유형을 분당 맥박수 값으로부터 계산하여 반환합니다. 반환값은 BloodPulseSpeedType에 의해 정의된 유형에 따라 표현됩니다.

```csharp
public BloodPulseSpeedType SpeedType { get; }
```

#### Property Value

 [BloodPulseSpeedType](TriageTrainer.Entity.Patient.BloodPulseSpeedType.md)

