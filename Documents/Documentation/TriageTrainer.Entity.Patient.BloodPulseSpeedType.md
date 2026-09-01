# <a id="TriageTrainer_Entity_Patient_BloodPulseSpeedType"></a> Enum BloodPulseSpeedType

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

맥박의 속도 유형을 정의합니다.

```csharp
public enum BloodPulseSpeedType
```

## Fields

`Bradycardia = 0` 

서맥, 맥박수가 60회/분 미만일 때를 표현합니다.



`Normal = 1` 

정상, 맥박수가 60회/분 이상, 100회/분 이하일 때를 표현합니다.



`Tachycardia = 2` 

빈맥, 맥박수가 100회/분 초과일 때를 표현합니다. 
(구현 팁: 과다출혈시의 맥박은 주로 [140, 170]회/분 범위에서 관찰됩니다.
이후 이 값으로부터 구체적인 맥박수를 결정해야 하는 경우, 특별한 이유가 아니라면
이 범위의 값으로 결정해야 합니다.)



