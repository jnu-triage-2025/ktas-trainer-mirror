# <a id="TriageTrainer_Entity_Patient_BodyTemperature"></a> Class BodyTemperature

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

신체의 체온을 표현합니다.

```csharp
public class BodyTemperature
```

#### Inheritance

object ← 
[BodyTemperature](TriageTrainer.Entity.Patient.BodyTemperature.md)

## Fields

### <a id="TriageTrainer_Entity_Patient_BodyTemperature_celsius"></a> celsius

실제 환자의 체온 값입니다.

```csharp
public float celsius
```

#### Field Value

 float

## Properties

### <a id="TriageTrainer_Entity_Patient_BodyTemperature_type"></a> type

이 속성은 환자의 체온 값으로부터 산출된 발열 유형입니다. BodyTemperatureType에 의해 정의된 발열 유형을 반환합니다. <br />
- (-INF, 36.0) : 저체온증(BodyTemperatureType.Hypothermia) <br />
- [36.0, 37.5] : 정상 범위(BodyTemperatureType.Normal) <br />
- (37.5, 38.0] : 발열(BodyTemperatureType.Fever) <br />
- (38.0, INF)  : 고열(BodyTemperatureType.HighFever)

```csharp
public BodyTemperatureType type { get; }
```

#### Property Value

 [BodyTemperatureType](TriageTrainer.Entity.Patient.BodyTemperatureType.md)

