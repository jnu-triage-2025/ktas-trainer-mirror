# <a id="TriageTrainer_Scenario_Rubric_RubricStatus"></a> Enum RubricStatus

Namespace: [TriageTrainer.Scenario.Rubric](TriageTrainer.Scenario.Rubric.md)  
Assembly: Assembly\-CSharp.dll  

한 루브릭 항목의 수행 판정 상태.

```csharp
public enum RubricStatus
```

## Fields

`NotApplicable = 3` 

해당 없음.



`NotPerformed = 2` 

미수행(게이트 타임아웃 강제진행/실패분기 또는 관찰자 표기).



`Pending = 0` 

아직 판정되지 않음(세션 진행 중 기본값).



`Performed = 1` 

수행함.



