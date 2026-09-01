# <a id="TriageTrainer_Entity_Patient_BodyTemperatureType"></a> Enum BodyTemperatureType

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

신체 온도의 유형을 표현합니다.

```csharp
public enum BodyTemperatureType
```

## Fields

`Fever = 2` 

발열, (37.5, 38.0] 범위를 표현합니다.



`HighFever = 3` 

고열, (38.0, INF) 범위를 표현합니다.



`Hypothermia = 0` 

저체온증, (-INF, 36.0) 범위를 표현합니다.



`Normal = 1` 

정상, [36.0, 37.5] 범위를 표현합니다.



