# <a id="TriageTrainer_Entity_PatientController_ChangeAssessableOnAssessDone"></a> Enum PatientController.ChangeAssessableOnAssessDone

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

트리아지 평가 완료 후 인터랙션 재노출 정책.

```csharp
public enum PatientController.ChangeAssessableOnAssessDone
```

## Fields

`DisableAssessable = 0` 

한 번 평가하면 이후 항상 트리아지 인터랙션을 비활성화한다.



`DisableOnIntendedOnly = 2` 

의도된 정답(intendedTriage)을 맞췄을 때만 비활성화하고, 오답이면 계속 노출한다.



`RemainAssessable = 1` 

평가 후에도 항상 트리아지 인터랙션을 유지(재평가 허용)한다.



