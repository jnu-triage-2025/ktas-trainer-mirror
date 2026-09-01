# <a id="TriageTrainer_Entity_PatientMonitor_Models_DisconnectedPatientDisplayMode"></a> Enum DisconnectedPatientDisplayMode

Namespace: [TriageTrainer.Entity.PatientMonitor.Models](TriageTrainer.Entity.PatientMonitor.Models.md)  
Assembly: Assembly\-CSharp.dll  

환자가 연결되지 않은 동안 모니터에 표시할 데이터의 원천입니다.

```csharp
public enum DisconnectedPatientDisplayMode
```

## Fields

`PlayDummyValues = 1` 

인스펙터에 설정된 모니터 기본값을 더미 데이터로 재생합니다.



`UnavailableValues = 0` 

모든 수치를 측정 불가(-1)로 표시하고, 파형은 기준선으로 유지합니다.



