# <a id="TriageTrainer_Entity_Patient_TriageLevel"></a> Enum TriageLevel

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

KTAS(Korean Triage and Acuity Scale, 한국형 응급환자 분류도구) 5단계 트리아지 등급을 표현합니다.

<p>
트리아지 분류에서 플레이어가 환자에게 부여하는 응급도 등급이며, 각 등급은 표준 색상과 명칭을 가집니다.
색상/명칭 조회는 <xref href="TriageTrainer.Entity.Patient.TriageLevelInfo" data-throw-if-not-resolved="false"></xref> 를 사용합니다.
</p>

<p>
<xref href="TriageTrainer.Entity.Patient.TriageLevel.Unassessed" data-throw-if-not-resolved="false"></xref> 는 아직 트리아지 분류가 수행되지 않은 상태를 나타내며, 실제 KTAS 등급이 아닙니다.
</p>

```csharp
public enum TriageLevel
```

## Fields

`Level1 = 1` 

KTAS 1단계 - 소생(Resuscitation). 색상: 파랑. 즉각적인 처치가 필요한 최고 응급.



`Level2 = 2` 

KTAS 2단계 - 긴급(Emergency). 색상: 빨강.



`Level3 = 3` 

KTAS 3단계 - 응급(Urgent). 색상: 노랑.



`Level4 = 4` 

KTAS 4단계 - 준응급(Less Urgent). 색상: 초록.



`Level5 = 5` 

KTAS 5단계 - 비응급(Non Urgent). 색상: 흰색.



`Unassessed = 0` 

미분류: 아직 트리아지 평가가 수행되지 않음(실제 KTAS 등급 아님).



