# <a id="TriageTrainer_Entity_Patient_Respiration"></a> Class Respiration

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

환자의 호흡 상태를 정의합니다.

```csharp
public class Respiration
```

#### Inheritance

object ← 
[Respiration](TriageTrainer.Entity.Patient.Respiration.md)

## Fields

### <a id="TriageTrainer_Entity_Patient_Respiration_awRR"></a> awRR

분당 호흡수(breaths per minute)입니다. 필드 이름은 <a href="https://techdocs.masimo.com/globalassets/techdocs/pdf/labmk_si22-4282_i_nomoline-isa-co2-module_rev.00.pdf">masimo 환자감시시스템모듈 설명서</a>에서 유래되었습니다.

```csharp
public int awRR
```

#### Field Value

 int

### <a id="TriageTrainer_Entity_Patient_Respiration_type"></a> type

호흡의 유형입니다. RespirationType에 의해 정의된 상태 값 중 하나를 가집니다.

```csharp
public RespirationType type
```

#### Field Value

 [RespirationType](TriageTrainer.Entity.Patient.RespirationType.md)

